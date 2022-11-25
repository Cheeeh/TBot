using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Exceptions;
using System.Linq;
using System.Timers;
using Timer = System.Threading.Timer;
using TBot.Ogame.Infrastructure.Models;
using TBot.Ogame.Infrastructure.Enums;
using Microsoft.Extensions.Logging;
using TBot.Common.Logging;
using TBot.Model;
using Tbot.Includes;
using Tbot.Helpers;
using Serilog.Events;
using Telegram.Bot.Types.ReplyMarkups;
using Tbot.Workers;

namespace Tbot.Services {

	internal class TelegramMessenger : ITelegramMessenger {
		private class InstanceWithBridge {
			public InstanceWithBridge(TBotMain instance, ITBotOgamedBridge tBotOgamedBridge) {
				Instance = instance;
				TBotOGamedBridge = tBotOgamedBridge;
			}
			public TBotMain Instance { get; set; }
			public ITBotOgamedBridge TBotOGamedBridge { get; set; }
		}
		public string Api { get; private set; }
		public string Channel { get; private set; }
		public ITelegramBotClient Client { get; private set; }

		private readonly ILoggerService<TelegramMessenger> _logger;
		private readonly ICalculationService _helpersService;

		CancellationTokenSource cts;
		CancellationToken ct;
		Task receivingTask = null;

		private SemaphoreSlim instanceSem = new SemaphoreSlim(1, 1);
		private List<InstanceWithBridge> instances = new();
		private int currInstanceIndex = -1;

		private Timer autoPingTimer;
		private SemaphoreSlim pingSem;
		private long pingHours;
		private DateTime startTime = DateTime.UtcNow;

		public TelegramMessenger(ILoggerService<TelegramMessenger> logger,
			ICalculationService helpersService,
			string api,
			string chatId,
			bool enableLogging) {
			_logger = logger;
			_helpersService = helpersService;
			Api = api;
			Client = new TelegramBotClient(Api);
			Channel = chatId;

			// Initialize Logger
			if (enableLogging == true) {
				_logger.WriteLog(LogLevel.Information, LogSender.Telegram, "Initializing Logging over Telegram");
				logger.AddTelegramLogger(api, chatId);
			}
		}

		public void StartAutoPing(long everyHours) {

			DateTime now = DateTime.UtcNow;
			DateTime roundedNextHour = now.AddHours(everyHours).AddMinutes(-now.Minute).AddSeconds(-now.Second);
			long nextping = (long) roundedNextHour.Subtract(now).TotalMilliseconds;

			_logger.WriteLog(LogLevel.Information, LogSender.Telegram, $"Initializing AutoPing (Hours {everyHours})...");
			StopAutoPing();

			pingHours = everyHours;
			pingSem = new SemaphoreSlim(1, 1);
			autoPingTimer = new Timer(AutoPing, null, nextping, Timeout.Infinite);
		}

		public void StopAutoPing() {
			if (autoPingTimer != null) {
				autoPingTimer.Dispose();
				autoPingTimer = null;
			}

			if (pingSem != null) {
				pingSem.Dispose();
				pingSem = null;
			}
		}
		private async void AutoPing(object state) {
			await pingSem.WaitAsync();
			try {
				DateTime now = DateTime.UtcNow;
				TimeSpan upTime = now - startTime;
				DateTime roundedNextHour = now.AddHours(pingHours).AddMinutes(-now.Minute).AddSeconds(-now.Second);
				long nextping = (long) roundedNextHour.Subtract(now).TotalMilliseconds;

				DateTime newTime = now.AddMilliseconds(nextping);
				autoPingTimer.Change(nextping, Timeout.Infinite);

				string pingStr = $"TBot is running since {FormattingHelper.TimeSpanToString(upTime)}\n";
				foreach (var instanceWithBridge in instances) {
					var instance = instanceWithBridge.Instance;
					TimeSpan instanceUpTime = now - instance.startTime;
					int instanceIndex = instances.IndexOf(instanceWithBridge);
					pingStr += $"#{instanceIndex} " +
						$"<code>[{instance.userData.userInfo.PlayerName}@{instance.userData.serverData.Name}]</code> " +
						$"since {FormattingHelper.TimeSpanToString(instanceUpTime)}\n";
				}
				await SendMessage(pingStr);

				_logger.WriteLog(LogLevel.Information, LogSender.Telegram, $"AutoPing sent, Next ping at {newTime.ToString()}");
			} catch (Exception ex) {
				_logger.WriteLog(LogLevel.Error, LogSender.Telegram, $"Error in AutoPing: {ex.Message}");
			} finally {
				pingSem.Release();
			}
			return;
		}

		public async Task AddTbotInstance(TBotMain instance, ITBotOgamedBridge tbotOgamedBridge) {
			_logger.WriteLog(LogLevel.Information, LogSender.Telegram, "Adding instance.....");
			_logger.WriteLog(LogLevel.Information, LogSender.Telegram, $"[{instance.userData.userInfo.PlayerName}@{instance.userData.serverData.Name}]");

			await instanceSem.WaitAsync(ct);

			if (!instances.Any(i => i.Instance.InstanceAlias == instance.InstanceAlias)) {
				var instanceWithBridge = new InstanceWithBridge(instance, tbotOgamedBridge);
				instances.Add(instanceWithBridge);

				int instanceIndex = instances.IndexOf(instanceWithBridge);
				await SendMessage($"<code>[{instance.userData.userInfo.PlayerName}@{instance.userData.serverData.Name}]</code> Instance added! (Index:{instanceIndex})");

				// Set a default instance
				if (currInstanceIndex < 0) {
					currInstanceIndex = instanceIndex;
				}
			}
			instanceSem.Release();
		}

		public async Task RemoveTBotInstance(TBotMain instance) {
			_logger.WriteLog(LogLevel.Information, LogSender.Telegram, "Removing instance.....");
			_logger.WriteLog(LogLevel.Information, LogSender.Telegram, $"[{instance.userData.userInfo.PlayerName}@{instance.userData.serverData.Name}]");

			await instanceSem.WaitAsync(ct);
			var instanceToRemove = instances.FirstOrDefault(i => i.Instance.InstanceAlias == instance.InstanceAlias);
			if (instanceToRemove == null || !instances.Remove(instanceToRemove)) {
				_logger.WriteLog(LogLevel.Information, LogSender.Telegram, $"Error removing [{instance.userData.userInfo.PlayerName}@{instance.userData.serverData.Name}]");
			}

			instanceSem.Release();
		}

		public async Task SendMessage(string message, ParseMode parseMode = ParseMode.Html, CancellationToken cancellationToken = default) {
			try {
				await Client.SendTextMessageAsync(
					chatId: Channel,
					text: message,
					parseMode: parseMode,
					cancellationToken: cancellationToken);
			} catch (Exception e) {
				_logger.WriteLog(LogLevel.Error, LogSender.Tbot, $"Could not send Telegram message: an exception has occurred: {e.Message}");
			}
		}

		public async Task SendTyping(CancellationToken cancellationToken) {
			await Client.SendChatActionAsync(
				chatId: Channel,
				chatAction: ChatAction.Typing,
				cancellationToken: cancellationToken);
		}

		public async Task SendReplyMarkup(string text, IEnumerable<IEnumerable<InlineKeyboardButton>> buttons, CancellationToken ct) {
			var inlineKeyboard = new InlineKeyboardMarkup(buttons);
			await Client.SendTextMessageAsync(
				chatId: Channel,
				text: text,
				replyMarkup: inlineKeyboard,
				cancellationToken: ct
			);
		}

		public async Task SendMessage(ITelegramBotClient client, Chat chat, string message, ParseMode parseMode = ParseMode.Html) {
			try {
				await client.SendTextMessageAsync(chat, message, parseMode);
			} catch (Exception e) {
				_logger.WriteLog(LogLevel.Error, LogSender.Tbot, $"Could not send Telegram message: an exception has occurred: {e.Message}");
			}
		}

		public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken) {
			// Commands targeting TBot process
			List<string> core_cmds = new List<string>()
			{
				"/setmain",
				"/getmain",
				"/getmainstats",
				"/listinstances",
				"/loglevel",
				"/setloglevel",
				"/ping",
				"/stopautoping",
				"/startautoping",
				"/help"
			};
			// Commands targeting specific TBotMain instance
			List<string> commands = new List<string>()
			{
				"/ghostsleep",
				"/ghostsleepall",
				"/ghost",
				"/ghostmoons",
				"/switch",
				"/sleep",
				"/wakeup",
				"/build",
				"/collect",
				"/collectdeut",
				"/minexpecargo",
				"/stopexpe",
				"/startexpe",
				"/stopautoresearch",
				"/startautoresearch",
				"/stopautomine",
				"/startautomine",
				"/stoplifeformautomine",
				"/startlifeformautomine",
				"/stoplifeformautoresearch",
				"/startlifeformautoresearch",
				"/stopdefender",
				"/startdefender",
				"/msg",
				"/getinfo",
				"/celestial",
				"/cancel",
				"/cancelghostsleep",
				"/editsettings",
				"/spycrash",
				"/attacked",
				"/getcelestials",
				"/recall",
				"/jumpgate",
				"/phalanx",
				"/deploy",
				"/getfleets",
				"/getcurrentauction",
				"/bidauction",
				"/subscribeauction",
				"/stopautofarm",
				"/startautofarm"
			};

			if (update.Type != UpdateType.Message) {

			} else {
				var message = update.Message;
				var arg = "";
				var test = "";
				decimal speed;
				long duration;
				Celestial celestial;
				int celestialID = 0;
				List<Celestial> myCelestials;
				Resources resources;
				Coordinate coord = new();
				Coordinate target = new();
				string[] args;

				if (core_cmds.Any(x => message.Text.ToLower().Contains(x))) {
					args = message.Text.ToLower().Split(' ');
					arg = args.ElementAt(0);

					switch (arg) {
						case "/setmain":
							if (args.Length != 2) {
								await SendMessage(botClient, message.Chat, "Invalid number of arguments. Expected 1");
								return;
							}

							if (int.TryParse(args.ElementAt(1), out int UserSelectedInstance) == true) {
								if (UserSelectedInstance >= instances.Count()) {
									await SendMessage(botClient, message.Chat, $"Selected index \"{args.ElementAt(1)}\" exceeds managed {instances.Count()}");
								} else {
									currInstanceIndex = UserSelectedInstance;
									var cInstance = instances.ElementAt(currInstanceIndex).Instance;
									await SendMessage(botClient, message.Chat, $"Selected index \"{args.ElementAt(1)}\"" +
										$"{cInstance.userData.userInfo.PlayerName}@{cInstance.userData.serverData.Name}");
								}
							} else {
								await SendMessage(botClient, message.Chat, $"Error parsing instance index from \"{args.ElementAt(1)}\"");
							}
							return;
						case "/getmain":
							if (currInstanceIndex < 0 || currInstanceIndex >= instances.Count()) {
								await SendMessage(botClient, message.Chat, "Currently managing no instance");
							} else {
								var instance = instances[currInstanceIndex].Instance;
								await SendMessage(botClient, message.Chat, $"Managing #{currInstanceIndex} {instance.userData.userInfo.PlayerName}@{instance.userData.serverData.Name}");
							}
							return;
						case "/getmainstats":
							if (currInstanceIndex < 0 || currInstanceIndex >= instances.Count()) {
								await SendMessage(botClient, message.Chat, "Currently managing no instance");
							} else {
								var instance = instances[currInstanceIndex].Instance;
								foreach (var feat in Features.AllFeatures) {
									await SendMessage(botClient, message.Chat, $"Instance #{currInstanceIndex} <code>{instance.ToString()}</code> <b>{feat.ToString()}</b> Running: {instance.IsFeatureRunning(feat)}");
								}
							}
							return;
						case "/listinstances":
							await SendMessage(botClient, message.Chat, $"Listing #{instances.Count}");
							foreach (var instanceWithBridge in instances) {
								var instance = instanceWithBridge.Instance;
								await SendMessage(botClient, message.Chat, $"{instances.IndexOf(instanceWithBridge)} {instance.userData.userInfo.PlayerName}@{instance.userData.serverData.Name}");
							}
							return;
						case "/loglevel":
							if (_logger.IsTelegramLoggerEnabled() == true) {
								await SendMessage(botClient, message.Chat, $"Telegram Logger Enabled. LogLevel is {_logger.GetTelegramLoggerLevel()}");
							}
							else {
								await SendMessage(botClient, message.Chat, "Telegram Logger is disabled.");
							}
							
							return;
						case "/setloglevel":
							if (args.Length != 2) {
								await SendMessage(botClient, message.Chat, "Usage is <code>/setloglevel Debug|Information|Warning|Error</code>");
								return;
							}
							
							if (Enum.TryParse<LogEventLevel>(args[1], true, out LogEventLevel newLevel) == true) {
								await SendMessage(botClient, message.Chat, $"Enabling Telegram logger with level {newLevel.ToString()}");
								_logger.AddTelegramLogger(Api, Channel);
								_logger.SetTelegramLoggerLogLevel(newLevel);
								await SendMessage(botClient, message.Chat, $"Telegram logger enabled! Level: {newLevel.ToString()}");
							}
							return;
						case "/ping":
							if (args.Length != 1) {
								await SendMessage(botClient, message.Chat, "No argument accepted with this command!");
								return;
							}
							await SendMessage(botClient, message.Chat, "Pong");
							return;
						case "/stopautoping":
							if (args.Length != 1) {
								await SendMessage(botClient, message.Chat, "No argument accepted with this command!");
								return;
							}
							StopAutoPing();
							await SendMessage(botClient, message.Chat, "TelegramAutoPing stopped!");
							return;


						case "/startautoping":
							if (args.Length != 1) {
								await SendMessage(botClient, message.Chat, "No argument accepted with this command!");
								return;
							}
							StartAutoPing(pingHours);
							await SendMessage(botClient, message.Chat, "TelegramAutoPing started!");
							return;
						case "/help":
							if (args.Length != 1) {
								await SendMessage(botClient, message.Chat, "No argument accepted with this command!");
								return;
							}
							await SendMessage(botClient, message.Chat,
								"\t Core Commands\n" +
								"/setmain - Set the TBot main instance to pilot. Format <code>/setmain 0</code>\n" +
								"/getmain - Get the current TBot instance that Telegram is managing\n" +
								"/getmainstats - Get current TBot instance statistics\n" +
								"/listinstances - List TBot main instances\n" +
								"/loglevel - Get current log level on telegram logging\n" +
								"/setloglevel - Set log level on telegram logging and enables it. Format <code>/setloglevel Debug|Information|Warning|Error </code>\n" +
								"/ping - Ping bot\n" +
								"/stopautoping - stop telegram autoping\n" +
								"/startautoping - start telegram autoping [Receive message every X hours]\n" +
								"/help - Display this help\n" +
								"\n\t TBot Main instance commands\n" +
								"/getfleets - Get OnGoing fleets ids (which are not already coming back)\n" +
								"/getcurrentauction - Get current Auction\n" +
								"/bidauction - Bid to current auction if there is one in progress. Format <code>/bidauction 213131 M:1000 C:1000 D:1000</code>\n" +
								"/subscribeauction - Get a notification when next auction will start\n" +
								"/ghostsleep - Wait fleets return, ghost harvest for current celestial only, and sleep for 5hours <code>/ghostsleep 4h3m or 3m50s Harvest</code>\n" +
								"/ghostsleepall - Wait fleets return, ghost harvest for all celestial and sleep for 5hours <code>/ghostsleepall 4h3m or 3m50s Harvest</code>\n" +
								"/ghost - Ghost for the specified amount of hours on the specified mission. Format: <code>/ghost 4h3m or 3m50s Harvest</code>\n" +
								"/ghostmoons - Ghost moons fleet for the specified amount of hours on the specified mission. Format: <code>/ghostto 4h30m Harvest</code>\n" +
								"/switch - Switch current celestial resources and fleets to its planet or moon at the specified speed. Format: <code>/switch 5</code>\n" +
								"/deploy - Deploy to celestial with full ships and resources. Format: <code>/deploy 3:41:9 moon/planet 10</code>\n" +
								"/jumpgate - jumpgate to moon with full ships [full], or keeps needed cargo amount for resources [auto]. Format: <code>/jumpgate 2:41:9 auto/full</code>\n" +
								"/phalanx - use phalanx from moon to destination. Format <code>/phalanx 2:241:9 4:100:1</code>\n" +
								"/cancelghostsleep - Cancel planned /ghostsleep(expe) if not already sent\n" +
								"/spycrash - Create a debris field by crashing a probe on target or automatically selected planet. Format: <code>/spycrash 2:41:9/auto</code>\n" +
								"/recall - Enable/disable fleet auto recall. Format: <code>/recall true/false</code>\n" +
								"/collect - Collect planets resources to JSON setting celestial\n" +
								"/build - Try to build buildable on each planet. Build max possible if no number value sent <code>/build LightFighter [100]</code>\n" +
								"/collectdeut - Collect planets only deut resources -> to JSON repatriate setting celestial\n" +
								"/msg - Send a message to current attacker. Format: <code>/msg hello dude</code>\n" +
								"/sleep - Stop bot for the specified amount of hours. Format: <code>/sleep 4h3m or 3m50s</code>\n" +
								"/wakeup - Wakeup bot\n" +
								"/cancel - Cancel fleet with specified ID. Format: <code>/cancel 65656</code>\n" +
								"/getcelestials - Return the list of your celestials\n" +
								"/attacked - check if you're (still) under attack\n" +
								"/celestial - Update program current celestial target. Format: <code>/celestial 2:45:8 Moon/Planet</code>\n" +
								"/getinfo - Get current celestial resources and ships. Additional arg format has to be <code>/getinfo 2:45:8 Moon/Planet</code>\n" +
								"/editsettings - Edit JSON file to change Expeditions, Colonize, Autominer's and Autoresearch Transport Origin, Repatriate and AutoReseach Target celestial. Format: <code>/editsettings 2:425:9 Moon</code>\n" +
								"/minexpecargo - Modify MinPrimaryToSend value inside JSON settings\n" +
								"/stopexpe - Stop sending expedition\n" +
								"/startexpe - Start sending expedition\n" +
								"/startdefender - start defender\n" +
								"/stopdefender - stop defender\n" +
								"/stopautoresearch - stop brain autoresearch\n" +
								"/startautoresearch - start brain autoresearch\n" +
								"/stopautomine - stop brain automine\n" +
								"/startautomine - start brain automine\n" +
								"/stoplifeformautomine - stop brain Lifeform automine\n" +
								"/startlifeformautomine - start brain Lifeform automine\n" +
								"/stoplifeformautoresearch - stop brain Lifeform autoresearch\n" +
								"/startlifeformautoresearch - start brain Lifeform autoresearch\n" +
								"/stopautofarm - stop autofarm\n" +
								"/startautofarm - start autofarm"
							, ParseMode.Html);
							return;
						default:

							return;
					}
				}
				// Check if instance is correct
				else if (currInstanceIndex < 0 || currInstanceIndex >= instances.Count()) {
					await SendMessage(botClient, message.Chat, "Select an instance with /setmain !");
					return;
				} else if (commands.Any(x => message.Text.ToLower().Contains(x))) {
					//Handle /commands@botname in string if exist
					if (message.Text.Contains("@") && message.Text.Split(" ").Length == 1)
						message.Text = message.Text.ToLower().Split(' ')[0].Split('@')[0];

					TBotMain currInstance = instances.ElementAt(currInstanceIndex).Instance;
					args = message.Text.ToLower().Split(' ');
					arg = args.ElementAt(0);

					try {
						await currInstance.WaitFeature();

						switch (arg) {

							case "/getfleets":
								if (args.Length != 1) {
									await SendMessage(botClient, message.Chat, "No argument accepted with this command!");
									return;
								}
								await currInstance.TelegramGetFleets();

								return;

							case "/getcurrentauction":
								if (args.Length != 1) {
									await SendMessage(botClient, message.Chat, "No argument accepted with this command!");
									return;
								}

								await currInstance.TelegramGetCurrentAuction();

								return;

							case "/subscribeauction":
								// If there is no auction in progress, then we will trigger a timer when next auction will be in place
								await currInstance.TelegramSubscribeToNextAuction();

								return;

							case "/bidauction":
								if (args.Length == 1) {
									// Bid minimum amount
									await currInstance.TelegramBidAuctionMinimum();
								} else if (args.Length < 3) {
									await SendMessage(botClient, message.Chat,
										"To bid auction must format: <code>/bidauction 33651579 M:1000 C:1000 D:1000 </code> \n" +
										"Or <code>/bidauction</code> to bid minimum amount to take auction", ParseMode.Html);
									return;
								} else {
									// First string has to be a valid celestialID
									try {
										myCelestials = currInstance.userData.celestials.ToList();
										celestial = myCelestials.Single(celestial => celestial.ID == int.Parse(args[1]));
										// If above has not thrown InvalidOperationException, then remaining can be any resource
										resources = Resources.FromString(string.Join(' ', args.Skip(2)));
										if (resources.TotalResources > 0)
											await currInstance.TelegramBidAuction(celestial, resources);
										else
											await SendMessage(botClient, message.Chat, "Cannot bid to auction with 0 resources set!");
									} catch (Exception e) {
										await SendMessage(botClient, message.Chat, $"Error parsing bid auction command \"{e.Message}\"");
									}
								}

								return;


							case "/ghost":
								if (args.Length != 2) {
									await SendMessage(botClient, message.Chat, "Duration (in hours) argument required! Format: <code>/ghost 4h3m or 3m50s or 1h</code>", ParseMode.Html);
									return;
								}
								arg = message.Text.Split(' ')[1];
								duration = FormattingHelper.ParseDurationFromString(arg);

								celestial = await currInstance.TelegramGetCurrentCelestial();
								await currInstance.FleetScheduler.AutoFleetSave(celestial, false, duration, false, false, Missions.None, true);

								return;


							case "/ghostto":
								if (args.Length != 3) {
									await SendMessage(botClient, message.Chat, "Duration (in hours) and mission arguments required! Format: <code>/ghostto 4h3m or 3m50s or 1h Harvest</code>", ParseMode.Html);
									return;
								}
								arg = message.Text.Split(' ')[1];

								Missions mission;
								if (!Enum.TryParse(args[2], true, out mission)) {
									await SendMessage(botClient, message.Chat, $"{test} error: Mission argument must be 'Harvest', 'Deploy', 'Transport', 'Spy' or 'Colonize'");
									return;
								}
								duration = FormattingHelper.ParseDurationFromString(args[1]);

								celestial = await currInstance.TelegramGetCurrentCelestial();
								await currInstance.FleetScheduler.AutoFleetSave(celestial, false, duration, false, false, mission, true);

								return;

							case "/ghostmoons":
								if (args.Length != 3) {
									await SendMessage(botClient, message.Chat, "Duration (in hours) argument required! Format: <code>/ghostmoons 4h3m or 3m50s or 1h <mission></code>!");
									return;
								}

								Missions mission_to_do;
								if (!Enum.TryParse(args[2], true, out mission_to_do)) {
									await SendMessage(botClient, message.Chat, $"{test} error: Mission argument must be 'Harvest', 'Deploy', 'Transport', 'Spy' or 'Colonize'. Got \"{test}\"");
									return;
								}
								duration = FormattingHelper.ParseDurationFromString(args[1]);

								List<Celestial> myMoons = currInstance.userData.celestials.Where(p => p.Coordinate.Type == Celestials.Moon).ToList();
								if (myMoons.Count > 0) {
									int fleetSaved = 0;
									foreach (Celestial moon in myMoons) {
										await SendMessage(botClient, message.Chat, $"Enqueueign FleetSave for {moon.ToString()}...");
										await currInstance.FleetScheduler.AutoFleetSave(moon, false, duration, false, false, mission_to_do, true);
										// Let's sleep a bit :)
										fleetSaved++;
										if (fleetSaved != myMoons.Count)
											await Task.Delay(RandomizeHelper.CalcRandomInterval(IntervalType.AFewSeconds), cancellationToken);
									}
									await SendMessage(botClient, message.Chat, "Moons FleetSave done!");
								} else {
									await SendMessage(botClient, message.Chat, "No moons found");
								}

								return;


							case "/ghostsleep":
								if (args.Length != 3) {
									await SendMessage(botClient, message.Chat, "Duration (in hours) argument required! Format: <code>/ghostsleep 4h3m or 3m50s or 1h Harvest</code>", ParseMode.Html);
									return;
								}
								duration = FormattingHelper.ParseDurationFromString(args[1]);

								if (!Enum.TryParse(args[2], true,out mission)) {
									await SendMessage(botClient, message.Chat, $"{test} error: Mission argument must be 'Harvest', 'Deploy', 'Transport', 'Spy' or 'Colonize'");
									return;
								}

								celestial = await currInstance.TelegramGetCurrentCelestial();
								currInstance.telegramUserData.CurrentCelestialToSave = celestial;
								currInstance.telegramUserData.Mission = mission;
								await currInstance.FleetScheduler.AutoFleetSave(celestial, false, duration, false, true, currInstance.telegramUserData.Mission, true);
								return;


							case "/ghostsleepall":
								if (args.Length != 3) {
									await SendMessage(botClient, message.Chat, "Duration (in hours) argument required! Format: <code>/ghostsleep 4h3m or 3m50s or 1h Harvest</code>", ParseMode.Html);
									return;
								}
								arg = message.Text.Split(' ')[1];
								duration = FormattingHelper.ParseDurationFromString(args[1]);

								if (!Enum.TryParse(args[2], true, out mission)) {
									await SendMessage(botClient, message.Chat, $"{args[2]} error: Mission argument must be 'Harvest', 'Deploy', 'Transport', 'Spy' or 'Colonize'");
									return;
								}

								celestial = await currInstance.TelegramGetCurrentCelestial();
								await currInstance.FleetScheduler.AutoFleetSave(celestial, false, duration, false, true, mission, true, true);
								return;


							case "/switch":
								if (message.Text.Split(' ').Length != 2) {
									await SendMessage(botClient, message.Chat, "Speed argument required! Format: <code>5 for 50%</code>", ParseMode.Html);
									return;
								}
								test = message.Text.Split(' ')[1];
								speed = decimal.Parse(test);

								if (1 <= speed && speed <= 10) {
									await currInstance.TelegramSwitch(speed);
									return;
								}
								await SendMessage(botClient, message.Chat, $"{test} error: Speed argument must be 1 or 2 or 3 for 10%, 20%, 30% etc.");
								return;


							case "/deploy":
								if (message.Text.Split(' ').Length != 4) {
									await SendMessage(botClient, message.Chat, "Coordinates, celestial type and speed arguments are needed! Format: <code>/deploy 2:56:8 moon/planet 1/3/5/7/10</code>", ParseMode.Html);

									return;
								}

								try {
									coord.Galaxy = int.Parse(message.Text.Split(' ')[1].Split(':')[0]);
									coord.System = int.Parse(message.Text.Split(' ')[1].Split(':')[1]);
									coord.Position = int.Parse(message.Text.Split(' ')[1].Split(':')[2]);
								} catch {
									await SendMessage(botClient, message.Chat, "Error while parsing coordinates! Format: <code>3:125:9</code>", ParseMode.Html);
									return;
								}

								Celestials type;
								arg = message.Text.ToLower().Split(' ')[2];
								if (!arg.Equals("moon") && !arg.Equals("planet")) {
									await SendMessage(botClient, message.Chat, $"Celestial type argument is needed! Format: <code>/celestial 2:41:9 moon/planet</code>", ParseMode.Html);
									return;
								}
								arg = char.ToUpper(arg[0]) + arg.Substring(1);
								if (Enum.TryParse(arg, out type)) {
									coord.Type = type;
								}

								test = message.Text.Split(' ')[3];
								speed = decimal.Parse(test);

								if (1 <= speed && speed <= 10) {
									celestial = await currInstance.TelegramGetCurrentCelestial();
									await currInstance.TelegramDeploy(celestial, coord, speed);
									return;
								}
								await SendMessage(botClient, message.Chat, $"{test} error: Speed argument must be 1 or 2 or 3 for 10%, 20%, 30% etc.");

								return;


							case "/jumpgate":
								if (message.Text.Split(' ').Length != 3) {
									await SendMessage(botClient, message.Chat, "Destination coordinates and full/auto arguments are needed (auto: keeps required cargo for resources) Format: <code>/jumpgate 2:20:8 auto</code>", ParseMode.Html);
									return;
								}

								try {
									coord.Galaxy = int.Parse(message.Text.Split(' ')[1].Split(':')[0]);
									coord.System = int.Parse(message.Text.Split(' ')[1].Split(':')[1]);
									coord.Position = int.Parse(message.Text.Split(' ')[1].Split(':')[2]);
								} catch {
									await SendMessage(botClient, message.Chat, "Error while parsing coordinates! Format: <code>3:125:9</code>", ParseMode.Html);
									return;
								}

								string mode = message.Text.ToLower().Split(' ')[2];
								if (!mode.Equals("full") && !mode.Equals("auto")) {
									await SendMessage(botClient, message.Chat, "Eerror! Format: <code>/jumpgate 2:20:8 auto/full</code>", ParseMode.Html);
									return;
								}

								celestial = await currInstance.TelegramGetCurrentCelestial();
								await currInstance.TelegramJumpGate(celestial, coord, mode);
								return;

							case "/phalanx": {
								if (args.Length != 3) {
									await SendMessage(botClient, message.Chat, "Error! Format: <code>2:241:9 4:100:1</code>", ParseMode.Html);
									return;
								}

								// Parse Origin
								try {
									coord.Galaxy = int.Parse(args[1].Split(':')[0]);
									coord.System = int.Parse(args[1].Split(':')[1]);
									coord.Position = int.Parse(args[1].Split(':')[2]);
									coord.Type = Celestials.Moon;
								} catch {
									await SendMessage(botClient, message.Chat, "Error while parsing origin coordinates! Format: <code>3:125:9</code>", ParseMode.Html);
									return;
								}

								// Parse destination
								try {
									target = new();
									target.Galaxy = int.Parse(args[2].Split(':')[0]);
									target.System = int.Parse(args[2].Split(':')[1]);
									target.Position = int.Parse(args[2].Split(':')[2]);
									target.Type = Celestials.Planet;
								} catch {
									await SendMessage(botClient, message.Chat, "Error while parsing destination coordinates! Format: <code>3:125:9</code>", ParseMode.Html);
									return;
								}

								// Check if Origin is a valid moon
								myCelestials = currInstance.userData.celestials.Where(p => p.Coordinate.IsSame(coord)).ToList();
								if (myCelestials.Count > 0) {
									await SendMessage(botClient, message.Chat, $"Phalanx from \"{myCelestials[0].Coordinate.ToString()}\" to \"{target.ToString()}\"");
									await currInstance.TelegramPhalanx(myCelestials[0], target);

								} else {
									await SendMessage(botClient, message.Chat, $"Origin coordinate \"{coord.ToString()}\" is not a Moon or does not belong to us!");
								}
								return;
							}



							case "/build":
								string listbuildables = "RocketLauncher\nLightLaser\nHeavyLaser\nGaussCannon\nPlasmaTurret\nSmallCargo\nLargeCargo\nLightFighter\nCruiser\nBattleship\nRecycler\nDestroyer\nBattlecruiser\nDeathstar\nCrawler\nPathfinder";
								decimal number = 0;
								Buildables buildable = Buildables.Null;

								if (message.Text.Split(' ').Length < 2) {
									await SendMessage(botClient, message.Chat, $"English buildable name required such as:\n{listbuildables}");
									return;
								}
								if (message.Text.Split(' ').Length == 3) {
									try {
										number = int.Parse(message.Text.Split(' ')[2]);
									} catch {
										await SendMessage(botClient, message.Chat, "Error while parsing number value!");
										return;
									}
								}
								if (Enum.TryParse(message.Text.Split(' ')[1], out buildable)) {
									await currInstance.TelegramBuild(buildable, number);
								} else {
									await SendMessage(botClient, message.Chat, "Error while parsing buildable value!");
									return;
								}
								return;


							case "/cancel":
								if (message.Text.Split(' ').Length != 2) {
									await SendMessage(botClient, message.Chat, "Mission argument required!");
									return;
								}
								arg = message.Text.Split(' ')[1];
								int fleetId = int.Parse(arg);

								await currInstance.TelegramRetireFleet(fleetId);
								return;


							case "/cancelghostsleep":
								if (message.Text.Split(' ').Length != 1) {
									await SendMessage(botClient, message.Chat, "No argument accepted with this command!");
									return;
								}

								await currInstance.TelegramCancelGhostSleep();
								return;


							case "/recall":
								if (message.Text.Split(' ').Length < 2) {
									await SendMessage(botClient, message.Chat, "Enable/disable auto fleetsave recall argument required! Format: <code>/recall true/false</code>", ParseMode.Html);
									return;
								}

								if (message.Text.Split(' ')[1] != "true" && message.Text.Split(' ')[1] != "false") {
									await SendMessage(botClient, message.Chat, "Argument must be <code>true</code> or <code>false</code>.");
									return;
								}
								string recall = message.Text.Split(' ')[1];

								if (await currInstance.EditSettings(null, Feature.Null, recall))
									await SendMessage(botClient, message.Chat, $"Recall value updated to {recall}.");
								return;


							case "/sleep":
								if (message.Text.Split(' ').Length != 2) {
									await SendMessage(botClient, message.Chat, "Time argument required!");
									return;
								}
								arg = message.Text.Split(' ')[1];
								duration = FormattingHelper.ParseDurationFromString(arg);
								var instanceWithBridge = instances.First(i => i.Instance.InstanceAlias == currInstance.InstanceAlias);
								DateTime timeNow = await instanceWithBridge.TBotOGamedBridge.GetDateTime();
								DateTime WakeUpTime = timeNow.AddSeconds(duration);

								await currInstance.SleepNow(WakeUpTime);
								return;


							case "/wakeup":
								if (message.Text.Split(' ').Length != 1) {
									await SendMessage(botClient, message.Chat, "No argument accepted with this command!");
									return;
								}
								currInstance.WakeUpNow(null);
								return;


							case "/msg":
								if (message.Text.Split(' ').Length < 2) {
									await SendMessage(botClient, message.Chat, "Need message argument!");
									return;
								}
								arg = message.Text.Split(new[] { ' ' }, 2).Last();
								await currInstance.TelegramMesgAttacker(arg);
								return;


							case "/minexpecargo":
								if (message.Text.Split(' ').Length < 2) {
									await SendMessage(botClient, message.Chat, "Need minimum cargo number argument!");
									return;
								}
								if (!int.TryParse(message.Text.Split(' ')[1], out int value)) {
									await SendMessage(botClient, message.Chat, "argument must be an integer!");
									return;
								}

								arg = message.Text.Split(' ')[1];
								int cargo = int.Parse(arg);
								if (await currInstance.EditSettings(null, Feature.Null, string.Empty, cargo))
									await SendMessage(botClient, message.Chat, $"MinPrimaryToSend value updated to {cargo}.");
								return;


							case "/stopexpe":
								if (message.Text.Split(' ').Length != 1) {
									await SendMessage(botClient, message.Chat, "No argument accepted with this command!");
									return;
								}

								await currInstance.StopFeature(Feature.Expeditions);
								await SendMessage(botClient, message.Chat, "Expeditions stopped!");
								return;


							case "/startexpe":
								if (message.Text.Split(' ').Length != 1) {
									await SendMessage(botClient, message.Chat, "No argument accepted with this command!");
									return;
								}

								await currInstance.InitializeFeature(Feature.Expeditions);
								await SendMessage(botClient, message.Chat, "Expeditions initialized!");
								return;


							case "/collect":
								if (message.Text.Split(' ').Length != 1) {
									await SendMessage(botClient, message.Chat, "No argument accepted with this command!");
									return;
								}

								currInstance.TelegramCollect();
								return;


							case "/collectdeut":
								if (message.Text.Split(' ').Length != 2) {
									await SendMessage(botClient, message.Chat, "Need minimum deut amount argument <code>/collectdeut 500000</code>");
									return;
								}
								if (!int.TryParse(message.Text.Split(' ')[1], out int val)) {
									await SendMessage(botClient, message.Chat, "argument must be an integer!");
									return;
								}

								long MinAmount = int.Parse(message.Text.Split(' ')[1]);
								await currInstance.TelegramCollectDeut(MinAmount);
								return;


							case "/stopautoresearch":
								if (message.Text.Split(' ').Length != 1) {
									await SendMessage(botClient, message.Chat, "No argument accepted with this command!");
									return;
								}

								await currInstance.StopFeature(Feature.BrainAutoResearch);
								await SendMessage(botClient, message.Chat, "AutoResearch stopped!");
								return;


							case "/startautoresearch":
								if (message.Text.Split(' ').Length != 1) {
									await SendMessage(botClient, message.Chat, "No argument accepted with this command!");
									return;
								}

								await currInstance.InitializeFeature(Feature.BrainAutoResearch);
								await SendMessage(botClient, message.Chat, "AutoResearch started!");
								return;


							case "/stopautomine":
								if (message.Text.Split(' ').Length != 1) {
									await SendMessage(botClient, message.Chat, "No argument accepted with this command!");
									return;
								}

								await currInstance.StopFeature(Feature.BrainAutoMine);
								await SendMessage(botClient, message.Chat, "AutoMine stopped!");
								return;


							case "/startautomine":
								if (message.Text.Split(' ').Length != 1) {
									await SendMessage(botClient, message.Chat, "No argument accepted with this command!");
									return;
								}

								await currInstance.InitializeFeature(Feature.BrainAutoMine);
								await SendMessage(botClient, message.Chat, "AutoMine started!");
								return;


							case "/stoplifeformautomine":
								if (message.Text.Split(' ').Length != 1) {
									await SendMessage(botClient, message.Chat, "No argument accepted with this command!");
									return;
								}

								await currInstance.StopFeature(Feature.BrainLifeformAutoMine);
								await SendMessage(botClient, message.Chat, "Lifeform AutoMine stopped!");
								return;


							case "/startlifeformautomine":
								if (message.Text.Split(' ').Length != 1) {
									await SendMessage(botClient, message.Chat, "No argument accepted with this command!");
									return;
								}

								await currInstance.InitializeFeature(Feature.BrainLifeformAutoMine);
								await SendMessage(botClient, message.Chat, "Lifeform AutoMine started!");
								return;


							case "/stoplifeformautoresearch":
								if (message.Text.Split(' ').Length != 1) {
									await SendMessage(botClient, message.Chat, "No argument accepted with this command!");
									return;
								}

								await currInstance.StopFeature(Feature.BrainLifeformAutoResearch);
								await SendMessage(botClient, message.Chat, "Lifeform AutoResearch stopped!");
								return;


							case "/startlifeformautoresearch":
								if (message.Text.Split(' ').Length != 1) {
									await SendMessage(botClient, message.Chat, "No argument accepted with this command!");
									return;
								}

								await currInstance.InitializeFeature(Feature.BrainLifeformAutoResearch);
								await SendMessage(botClient, message.Chat, "Lifeform AutoResearch started!");
								return;


							case "/stopdefender":
								if (message.Text.Split(' ').Length != 1) {
									await SendMessage(botClient, message.Chat, "No argument accepted with this command!");
									return;
								}

								await currInstance.StopFeature(Feature.Defender);
								await SendMessage(botClient, message.Chat, "Defender stopped!");
								return;


							case "/startdefender":
								if (message.Text.Split(' ').Length != 1) {
									await SendMessage(botClient, message.Chat, "No argument accepted with this command!");
									return;
								}

								await currInstance.InitializeFeature(Feature.Defender);
								await SendMessage(botClient, message.Chat, "Defender started!");
								return;


							case "/stopautofarm":
								if (message.Text.Split(' ').Length != 1) {
									await SendMessage(botClient, message.Chat, "No argument accepted with this command!");
									return;
								}

								await currInstance.StopFeature(Feature.AutoFarm);
								await SendMessage(botClient, message.Chat, "Autofarm stopped!");
								return;


							case "/startautofarm":
								if (message.Text.Split(' ').Length != 1) {
									await SendMessage(botClient, message.Chat, "No argument accepted with this command!");
									return;
								}

								await currInstance.InitializeFeature(Feature.AutoFarm);
								await SendMessage(botClient, message.Chat, "Autofarm started!");
								return;


							case "/getinfo":
								args = message.Text.Split(' ');
								if (args.Length == 1) {
									celestial = await currInstance.TelegramGetCurrentCelestial();
									await currInstance.TelegramGetInfo(celestial);

									return;
								} else if (args.Length == 2) {
									myCelestials = currInstance.userData.celestials.ToList();
									// Try celestial ID first
									try {
										celestialID = int.Parse(args[1]);
										celestial = myCelestials.Single(c => c.ID == celestialID);
										await currInstance.TelegramGetInfo(celestial);

									} catch (Exception e) {
										await SendMessage(botClient, message.Chat,
											$"Invalid arguments specified for Format <code>/getinfo 321312132</code>\n" +
											$"Error:{e.Message}");
									}
									return;
								} else if (args.Length == 3) {
									myCelestials = currInstance.userData.celestials.ToList();
									// Try format Galaxy:System:Position (Moon|Planet)
									try {
										coord = Coordinate.FromString(string.Join(' ', args.Skip(1)));
										celestial = myCelestials.Single(c => c.Coordinate.IsSame(coord));
										await currInstance.TelegramGetInfo(celestial);

									} catch (Exception e) {
										await SendMessage(botClient, message.Chat,
											$"Invalid arguments specified for Format <code>/getinfo 2:48:5 Moon/Planet</code>\n" +
											$"Error:{e.Message}");
									}
								} else {
									await SendMessage(botClient, message.Chat, "Invalid number of argument specified for current command");
								}


								return;


							case "/celestial":
								if (message.Text.Split(' ').Length != 3) {
									await SendMessage(botClient, message.Chat, "Coordinate and celestial type arguments required! Format: <code>/celestial 2:56:8 moon/planet</code>", ParseMode.Html);

									return;
								}

								arg = message.Text.ToLower().Split(' ')[2];
								if (!arg.Equals("moon") && !arg.Equals("planet")) {
									await SendMessage(botClient, message.Chat, $"Celestial type argument required! Format: <code>/celestial 2:41:9 moon/planet</code>", ParseMode.Html);
									return;
								}

								try {
									coord.Galaxy = int.Parse(message.Text.Split(' ')[1].Split(':')[0]);
									coord.System = int.Parse(message.Text.Split(' ')[1].Split(':')[1]);
									coord.Position = int.Parse(message.Text.Split(' ')[1].Split(':')[2]);
								} catch {
									await SendMessage(botClient, message.Chat, "Error while parsing coordinates! Format: <code>3:125:9</code>", ParseMode.Html);
									return;
								}

								arg = char.ToUpper(arg[0]) + arg.Substring(1);
								currInstance.TelegramSetCurrentCelestial(coord, arg);
								return;


							case "/editsettings":
								if (message.Text.Split(' ').Length < 3) {
									await SendMessage(botClient, message.Chat, "Coordinate and celestial type arguments required! Format: <code>/editsettings 2:56:8 moon/planet (AutoMine/AutoResearch/AutoRepatriate/Expeditions/Colonize)</code>", ParseMode.Html);
									return;
								}

								arg = message.Text.ToLower().Split(' ')[2];
								if (!arg.Equals("moon") && !arg.Equals("planet")) {
									await SendMessage(botClient, message.Chat, $"Celestial type argument needed! Format: <code>/editsettings 2:100:3 moon/planet (AutoMine/AutoResearch/AutoRepatriate/Expeditions/Colonize)</code>", ParseMode.Html);
									return;
								}

								try {
									coord.Galaxy = int.Parse(message.Text.Split(' ')[1].Split(':')[0]);
									coord.System = int.Parse(message.Text.Split(' ')[1].Split(':')[1]);
									coord.Position = int.Parse(message.Text.Split(' ')[1].Split(':')[2]);
								} catch {
									await SendMessage(botClient, message.Chat, "Error while parsing coordinates! Format: <code>3:125:9 moon/planet (AutoMine/AutoResearch/AutoRepatriate/Expeditions/Colonize)</code>", ParseMode.Html);
									return;
								}
								var celestialType = char.ToUpper(arg[0]) + arg.Substring(1);

								Feature updateType = Feature.Null;
								if (message.Text.ToLower().Split(' ').Length > 3) {
									arg = message.Text.ToLower().Split(' ')[3];
									if (!arg.Equals("AutoMine") && !arg.Equals("AutoResearch") && !arg.Equals("AutoRepatriate") && !arg.Equals("Expeditions") && !arg.Equals("Colonize")) {
										await SendMessage(botClient, message.Chat, $"Update type argument not valid! Format: <code>/editsettings 2:100:3 moon/planet (AutoMine/AutoResearch/AutoRepatriate/Expeditions/Colonize)</code>", ParseMode.Html);
										return;
									} else {
										switch (arg) {
											case "AutoMine":
												updateType = Feature.BrainAutoMine;
												break;
											case "AutoResearch":
												updateType = Feature.BrainAutoResearch;
												break;
											case "AutoRepatriate":
												updateType = Feature.BrainAutoRepatriate;
												break;
											case "Expeditions":
												updateType = Feature.Expeditions;
												break;
											case "Colonize":
												updateType = Feature.Colonize;
												break;
											default:
												break;
										}
									}
								}

								currInstance.TelegramSetCurrentCelestial(coord, celestialType, updateType, true);
								return;


							case "/spycrash":
								if (message.Text.Split(' ').Length != 2) {
									await SendMessage(botClient, message.Chat, "<code>auto</code> or coordinate argument needed! Format: <code>/spycrash auto/2:56:8</code>", ParseMode.Html);
									return;
								}

								if (message.Text.Split(' ')[1].ToLower().Equals("auto")) {
									target = null;
								} else {
									try {
										coord.Galaxy = int.Parse(message.Text.Split(' ')[1].Split(':')[0]);
										coord.System = int.Parse(message.Text.Split(' ')[1].Split(':')[1]);
										coord.Position = int.Parse(message.Text.Split(' ')[1].Split(':')[2]);
										target = new Coordinate() { Galaxy = coord.Galaxy, System = coord.System, Position = coord.Position, Type = Celestials.Planet };
									} catch {
										await SendMessage(botClient, message.Chat, "Error while parsing coordinates! Format: <code>3:125:9</code>, or <code>auto</code>", ParseMode.Html);
										return;
									}
								}
								Celestial origin = await currInstance.TelegramGetCurrentCelestial();

								await currInstance.FleetScheduler.SpyCrash(origin, target);
								return;


							case "/attacked":
								if (message.Text.Split(' ').Length != 1) {
									await SendMessage(botClient, message.Chat, "No argument accepted with this command!");
									return;
								}
								bool isUnderAttack = await currInstance.TelegramIsUnderAttack();

								if (isUnderAttack) {
									await SendMessage(botClient, message.Chat, "Yes! You're still under attack!");
								} else {
									await SendMessage(botClient, message.Chat, "Nope! Your empire is safe.");
								}
								return;


							case "/getcelestials":
								if (message.Text.Split(' ').Length != 1) {
									await SendMessage(botClient, message.Chat, "No argument accepted with this command!");
									return;
								}
								myCelestials = currInstance.userData.celestials.ToList();
								string celestialStr = "";
								foreach (Celestial c in myCelestials) {
									celestialStr += $"{c.Name.PadRight(16, ' ')} {c.Coordinate.ToString().PadRight(16)} {c.ID}\n";
								}
								await SendMessage(botClient, message.Chat, celestialStr);

								return;
							default:
								return;
						}

					} catch (ApiRequestException) {
						await SendMessage(botClient, message.Chat, $"ApiRequestException Error!\nTry /ping to check if bot still alive!");
						return;

					} catch (FormatException) {
						await SendMessage(botClient, message.Chat, $"FormatException Error!\nYou entered an unexpected value (string instead of integer?)\nTry /ping to check if bot still alive!");
						return;

					} catch (NullReferenceException) {
						await SendMessage(botClient, message.Chat, $"NullReferenceException Error!\n Something unknown went wrong!\nTry /ping to check if bot still alive!");
						return;

					} catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested == true) {
						await SendMessage(botClient, message.Chat, $"OperationCanceledException Maybe TelegramMessenger has been disabled.");
						return;

					} catch (Exception) {
						await SendMessage(botClient, message.Chat, $"Unknown Exception Error!\nTry /ping to check if bot still alive!");
						return;

					} finally {
						currInstance.releaseFeature();
					}
				}
			}
		}

		async Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken) {
			try {
				if (exception is ApiRequestException apiRequestException) {
					await botClient.SendTextMessageAsync(Channel, apiRequestException.ToString());
				}
			} catch { }
		}

		public async Task TelegramBot() {
			try {
				await instanceSem.WaitAsync();
				cts = new CancellationTokenSource();
				ct = cts.Token;

				var receiverOptions = new ReceiverOptions {
					AllowedUpdates = Array.Empty<UpdateType>(),
					ThrowPendingUpdates = true
				};

				receivingTask = Client.ReceiveAsync(HandleUpdateAsync, HandleErrorAsync, receiverOptions, ct);
			} finally {
				instanceSem.Release();
			}
		}

		public async Task TelegramBotDisable() {
			try {

				await instanceSem.WaitAsync(ct);
				foreach (var instance in instances) {
					instance.Instance.RemoveTelegramMessenger();
				}
			} finally {
				instanceSem.Release();
			}

			// First of all, remove from any instance
			if (Client != null) {
				cts.Cancel();
				try {
					await receivingTask;
				} catch (OperationCanceledException) {

				} finally {
					cts.Dispose();
					receivingTask = null;
				}

			}
			// We should wait for ReceiveAsync to be dismissed
			_logger.RemoveTelegramLogger();
		}
	}
}
