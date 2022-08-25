using Tbot.Model;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Exceptions;
using System.Linq;


namespace Tbot.Includes {

	class TelegramMessenger {
		public string Api { get; private set; }
		public string Channel { get; private set; }
		static ITelegramBotClient Client { get; set; }
		public TelegramMessenger(string api, string channel) {
			Api = api;
			Client = new TelegramBotClient(Api);
			Channel = channel;
		}
		
		
		public async void SendMessage(string message) {
			Helpers.WriteLog(LogType.Info, LogSender.Tbot, "Sending Telegram message...");
			try {
				await Client.SendTextMessageAsync(Channel, message);
			} catch (Exception e) {
				Helpers.WriteLog(LogType.Error, LogSender.Tbot, $"Could not send Telegram message: an exception has occurred: {e.Message}");
			}
		}

		public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken) {

			List<string> commands = new List<string>()
			{
				"/ghostsleep",
				"/ghostsleepexpe",
				"/ghost",
				"/ghostto",
				"/switch",
				"/sleep",
				"/wakeup",
				"/collect",
				"/stopautopong",
				"/startautopong",
				"stopexpe",
				"startexpe",
				"/stopautomine",
				"/startautomine",
				"/stopdefender",
				"/startdefender",
				"/msg",
				"/ping",
				"/getinfo",
				"/celestial",
				"/cancel",
				"/editsettings",
				"/spycrash",
				"/attacked",
				"/getcelestials",
				"/recall",
				"/help"
			};

			if (update.Type == Telegram.Bot.Types.Enums.UpdateType.Message) {
				var message = update.Message;
				var arg = "";
				var test = "";
				decimal speed;
				long duration;
				Celestial celestial;
				Coordinate coord = new();

				if (commands.Any(x => message.Text.ToLower().Contains(x))) {
					//Handle /commands@botname in string if exist
					if (message.Text.Contains("@") && message.Text.Split(" ").Length == 1) 
						message.Text = message.Text.ToLower().Split(' ')[0].Split('@')[0];

					try {
						Tbot.Program.WaitFeature();

						switch (message.Text.ToLower().Split(' ')[0]) {

							case ("/ghost"):
								if (message.Text.Split(' ').Length != 2) {
									await botClient.SendTextMessageAsync(message.Chat, "Need 1 value -> ghost duration (eg: /ghost 4)");
									return;
								}
								arg = message.Text.Split(' ')[1];
								duration = Int32.Parse(arg) * 60 * 60; //second

								celestial = Tbot.Program.TelegramGetCurrentCelestial();
								Tbot.Program.AutoFleetSave(celestial, false, duration, false, false, Missions.None, true);

								return;


							case ("/ghostto"):
								if (message.Text.Split(' ').Length != 3) {
									await botClient.SendTextMessageAsync(message.Chat, "Need ghost duration and destination type (eg: /ghostto 4 harvest)");
									return;
								}
								arg = message.Text.Split(' ')[1];
								test = message.Text.Split(' ')[2];
								test = char.ToUpper(test[0]) + test.Substring(1);
								Missions mission;

								if (!Missions.TryParse(test, out mission)) {
									await botClient.SendTextMessageAsync(message.Chat, $"{test} error: Value must be 'Harvest','Deploy','Transport','Spy','Colonize'");
									return;
								}
								duration = Int32.Parse(arg) * 60 * 60; //second

								celestial = Tbot.Program.TelegramGetCurrentCelestial();
								Tbot.Program.AutoFleetSave(celestial, false, duration, false, false, mission, true);

								return;


							case ("/ghostsleep"):
								if (message.Text.Split(' ').Length != 3) {
									await botClient.SendTextMessageAsync(message.Chat, "Need time and destination type (eg /ghostsleep 5 harvest)");
									return;
								}
								arg = message.Text.Split(' ')[1];
								duration = Int32.Parse(arg) * 60 * 60; //second

								celestial = Tbot.Program.TelegramGetCurrentCelestial();
								Tbot.Program.AutoFleetSave(celestial, false, duration, false, true, Missions.None, true);
								return;


							case ("/ghostsleepexpe"):
								if (message.Text.Split(' ').Length != 3) {
									await botClient.SendTextMessageAsync(message.Chat, "Need time and destination type (eg /ghostsleepexpe 5 harvest)");
									return;
								}
								arg = message.Text.Split(' ')[1];
								duration = Int32.Parse(arg) * 60 * 60; //second

								celestial = Tbot.Program.TelegramGetCurrentCelestial();
								Tbot.Program.AutoFleetSave(celestial, false, duration, false, true, Missions.None, true, true);
								return;


							case ("/switch"):
								if (message.Text.Split(' ').Length != 2) {
									await botClient.SendTextMessageAsync(message.Chat, "Need speed value (eg: 5 for 50%)");
									return;
								}
								test = message.Text.Split(' ')[1];
								speed = decimal.Parse(test);

								if (1 <= speed && speed <= 10) {
									Tbot.Program.TelegramSwitch(speed);
									return;
								}
								await botClient.SendTextMessageAsync(message.Chat, $"{test} error: Value must be 1 or 2 or 3 for 10%,20%,30% etc.");
								return;


							case ("/cancel"):
								if (message.Text.Split(' ').Length != 2) {
									await botClient.SendTextMessageAsync(message.Chat, "Need mission value!");
									return;
								}
								arg = message.Text.Split(' ')[1];
								int fleetId = Int32.Parse(arg);

								Tbot.Program.TelegramRetireFleet(fleetId);
								return;


							case ("/recall"):
								if (message.Text.Split(' ').Length < 2) {
									await botClient.SendTextMessageAsync(message.Chat, "Need true/false value! (enable/disable auto fleets recall)");
									return;
								}

								if (message.Text.Split(' ')[1] != "true" && message.Text.Split(' ')[1] != "false") {
									await botClient.SendTextMessageAsync(message.Chat, "Need true/false value! (enable/disable auto fleets recall)");
									return;
								}
								string recall = message.Text.Split(' ')[1];
							 
								if (Tbot.Program.EditSettings(null, recall))
									await botClient.SendTextMessageAsync(message.Chat, $"Recall value updated to {recall}.");
								return;


							case ("/sleep"):
								if (message.Text.Split(' ').Length != 2) {
									await botClient.SendTextMessageAsync(message.Chat, "Need mission value!");
									return;
								}
								arg = message.Text.Split(' ')[1];
								int sleepingtime = Int32.Parse(arg);

								DateTime timeNow = Tbot.Program.GetDateTime();
								DateTime WakeUpTime = timeNow.AddHours(sleepingtime);
	
								Tbot.Program.SleepNow(WakeUpTime);
								return;

							
							case ("/wakeup"):
								if (message.Text.Split(' ').Length != 1) {
									await botClient.SendTextMessageAsync(message.Chat, "No value needed!");
									return;
								}
								Tbot.Program.WakeUpNow(null);
								return;

							
							case ("/msg"):
								if (message.Text.Split(' ').Length < 2) {
									await botClient.SendTextMessageAsync(message.Chat, "Need message value!");
									return;
								}
								arg = message.Text.Split(new[] { ' ' }, 2).Last();
								Tbot.Program.TelegramMesgAttacker(arg);
								return;

							
							case ("/stopexpe"):
								if (message.Text.Split(' ').Length != 1) {
									await botClient.SendTextMessageAsync(message.Chat, "No need value!");
									return;
								}

								Tbot.Program.StopExpeditions();
								await botClient.SendTextMessageAsync(message.Chat, "Expedition stopped!");
								return;

							
							case ("/startexpe"):
								if (message.Text.Split(' ').Length != 1) {
									await botClient.SendTextMessageAsync(message.Chat, "No need value!");
									return;
								}

								Tbot.Program.InitializeExpeditions();
								await botClient.SendTextMessageAsync(message.Chat, "Expedition initialized!");
								return;


							case ("/collect"):
								if (message.Text.Split(' ').Length != 1) {
									await botClient.SendTextMessageAsync(message.Chat, "No need value!");
									return;
								}

								Tbot.Program.TelegramCollect();
								return;


							case ("/stopautomine"):
								if (message.Text.Split(' ').Length != 1) {
									await botClient.SendTextMessageAsync(message.Chat, "No need value!");
									return;
								}

								Tbot.Program.StopBrainAutoMine();
								await botClient.SendTextMessageAsync(message.Chat, "AutoMine stopped!");
								return;


							case ("/startautomine"):
								if (message.Text.Split(' ').Length != 1) {
									await botClient.SendTextMessageAsync(message.Chat, "No need value!");
									return;
								}

								Tbot.Program.InitializeBrainAutoMine();
								await botClient.SendTextMessageAsync(message.Chat, "AutoMine started!");
								return;

							case ("/stopdefender"):
								if (message.Text.Split(' ').Length != 1) {
									await botClient.SendTextMessageAsync(message.Chat, "No need value!");
									return;
								}

								Tbot.Program.StopDefender();
								await botClient.SendTextMessageAsync(message.Chat, "Defender stopped!");
								return;


							case ("/startdefender"):
								if (message.Text.Split(' ').Length != 1) {
									await botClient.SendTextMessageAsync(message.Chat, "No need value!");
									return;
								}

								Tbot.Program.InitializeDefender();
								await botClient.SendTextMessageAsync(message.Chat, "Defender started!");
								return;


							case ("/getinfo"):
								if (message.Text.Split(' ').Length != 1) {
									await botClient.SendTextMessageAsync(message.Chat, "No need value!");
									return;
								}

								celestial = Tbot.Program.TelegramGetCurrentCelestial();
								Tbot.Program.TelegramGetInfo(celestial);
								return;


							case ("/celestial"):
								if (message.Text.Split(' ').Length != 3) {
									await botClient.SendTextMessageAsync(message.Chat, "Need coordinate and type! (/celestial 2:56:8 moon/planet)");
									return;
								}

								arg = message.Text.ToLower().Split(' ')[2];
								if ( (!arg.Equals("moon")) && (!arg.Equals("planet")) ) {
									await botClient.SendTextMessageAsync(message.Chat, $"Need value moon or planet (got {arg})");
									return;
								}

								try {
									coord.Galaxy = Int32.Parse(message.Text.Split(' ')[1].Split(':')[0]);
									coord.System = Int32.Parse(message.Text.Split(' ')[1].Split(':')[1]);
									coord.Position = Int32.Parse(message.Text.Split(' ')[1].Split(':')[2]);
								} catch {
									await botClient.SendTextMessageAsync(message.Chat, "Error while parsing coordinate! (must be like 3:125:9)");
									return;
								}

								arg = char.ToUpper(arg[0]) + arg.Substring(1);
								Tbot.Program.TelegramSetCurrentCelestial(coord, arg);
								return;

							
							case ("/editsettings"):
								if (message.Text.Split(' ').Length != 3) {
									await botClient.SendTextMessageAsync(message.Chat, "Need coordinate and type! (/editsettings 2:56:8 moon/planet)");
									return;
								}

								arg = message.Text.ToLower().Split(' ')[2];
								if ((!arg.Equals("moon")) && (!arg.Equals("planet"))) {
									await botClient.SendTextMessageAsync(message.Chat, $"Need value moon or planet (got {arg})");
									return;
								}

								try {
									coord.Galaxy = Int32.Parse(message.Text.Split(' ')[1].Split(':')[0]);
									coord.System = Int32.Parse(message.Text.Split(' ')[1].Split(':')[1]);
									coord.Position = Int32.Parse(message.Text.Split(' ')[1].Split(':')[2]);
								} catch {
									await botClient.SendTextMessageAsync(message.Chat, "Error while parsing coordinate! (must be like 3:125:9)");
									return;
								}

								arg = char.ToUpper(arg[0]) + arg.Substring(1);
								Tbot.Program.TelegramSetCurrentCelestial(coord, arg, true);
								return;


							case ("/spycrash"):
								if (message.Text.Split(' ').Length != 2) {
									await botClient.SendTextMessageAsync(message.Chat, "Need 'auto' or coordinate (/spycrash auto/2:56:8)");
									return;
								}

								Coordinate target;
								if (message.Text.Split(' ')[1].ToLower().Equals("auto")) {
									target = null;
								} else {
									try {
										coord.Galaxy = Int32.Parse(message.Text.Split(' ')[1].Split(':')[0]);
										coord.System = Int32.Parse(message.Text.Split(' ')[1].Split(':')[1]);
										coord.Position = Int32.Parse(message.Text.Split(' ')[1].Split(':')[2]);
										target = new Coordinate() { Galaxy = coord.Galaxy, System = coord.System, Position = coord.Position, Type = Celestials.Planet };
									} catch {
										await botClient.SendTextMessageAsync(message.Chat, "Error while parsing value! (coord be like 3:125:9, or 'auto')");
										return;
									}
								}
								Celestial origin = Tbot.Program.TelegramGetCurrentCelestial();
								
								Tbot.Program.SpyCrash(origin, target);
								return;


							case ("/attacked"):
								if (message.Text.Split(' ').Length != 1) {
									await botClient.SendTextMessageAsync(message.Chat, "No need value!");
									return;
								}
								bool isUnderAttack = Tbot.Program.TelegramIsUnderAttack();
									
								if (isUnderAttack) {
									await botClient.SendTextMessageAsync(message.Chat, "Yes! you're still under attack!");
								} else {
									await botClient.SendTextMessageAsync(message.Chat, "Nope! you're safe dude.");
								}
								return;


							case ("/getcelestials"):
								if (message.Text.Split(' ').Length != 1) {
									await botClient.SendTextMessageAsync(message.Chat, "No need value!");
									return;
								}
								List<Celestial> myCelestials = Tbot.Program.celestials.ToList();
								string listCoords = "";
								foreach (Coordinate coordinate in myCelestials.Select(p => p.Coordinate)){
									listCoords += coordinate.ToString() + "\n";
								}
									await botClient.SendTextMessageAsync(message.Chat, $"{listCoords}");

								return;


							case ("/ping"):
								if (message.Text.Split(' ').Length != 1) {
									await botClient.SendTextMessageAsync(message.Chat, "No need value!");
									return;
								}
								await botClient.SendTextMessageAsync(message.Chat, "pong");
								return;


							case ("/stopautopong"):
								if (message.Text.Split(' ').Length != 1) {
									await botClient.SendTextMessageAsync(message.Chat, "No need value!");
									return;
								}
								Tbot.Program.StopTelegramAutoPong();
								await botClient.SendTextMessageAsync(message.Chat, "TelegramAutoPong stopped!");
								return;


							case ("/startautopong"):
								if (message.Text.Split(' ').Length != 1) {
									await botClient.SendTextMessageAsync(message.Chat, "No need value!");
									return;
								}
								Tbot.Program.InitializeTelegramAutoPong();
								await botClient.SendTextMessageAsync(message.Chat, "TelegramAutoPong started!");
								return;


							case ("/help"):
								if (message.Text.Split(' ').Length != 1) {
									await botClient.SendTextMessageAsync(message.Chat, "No need value!");
									return;
								}
								await botClient.SendTextMessageAsync(message.Chat,
									"/ghostsleep - '/ghostsleep 5 Harvest' -> Wait fleets return, ghost harvest and sleep for 5hours\n" +
									"/ghostsleepexpe - '/ghostsleepexpe 5 Harvest' -> Wait fleets return, ghost harvest, sleep for 5hours, but keep sending expedition\n" +
									"/ghost - '/ghost 4' -> Ghost fleet for 4 hours\n, let bot chose mission type" +
									"/ghostto - '/ghostto 4 Harvest'\n" +
									"/switch - '/switch 5' -> switch current celestial resources and fleets to its planet or moon at 50% speed\n" +
									"/spycrash - Create a debris field by crashing a probe on target planet\n" +
									"/recall - '/recall true/false' -> enable/disable fleet auto recall\n" +
									"/stopexpe - Stop sending expedition\n" +
									"/startexpe - Start sending expedition\n" +
									"/startdefender - start defender\n" +
									"/stopdefender - stop defender\n" +
									"/stopautomine - stop brain automine\n" +
									"/startautomine - start brain automine\n" +
									"/stopautopong - stop telegram autopong\n" +
									"/startautopong - start telegram autopong (send telegram message every rounded hours)\n" +
									"/collect - Collect planets resources to JSON setting celestial\n" +
									"/msg - '/msg hello dude' -> Send 'hello dude' to current attacker\n" +
									"/sleep - '/sleep 1' -> Stop bot, inactive for 1 hours\n" +
									"/wakeup - Wakeup bot\n" +
									"/cancel - '/cancel 65656' -> cancel ongoing fleet id 65656\n" +
									"/getcelestials - return the coordinate list and type of all your celestials\n" +
									"/attacked - check if you're (still) under attack\n" +
									"/celestial - '/celestial 2:45:8 Moon' (Moon/Planet) Update program current celestial target\n" +
									"/getinfo - Get current celestial resources and ships\n" +
									"/editsettings - '/editsettings 2:425:9 moon -> Edit JSON file to change: Expedition, Transport, Repatriate and AutoReseach (Origin/Target) celestial\n" + 
									"/ping - Ping bot\n" +
									"/help - Display this help"
								);
								return;
							default:
								return;
						}

					} catch (ApiRequestException) {
						await botClient.SendTextMessageAsync(message.Chat, $"ApiRequestException Error!\nTry /ping to check if bot still alive!");
						return;

					} catch (FormatException) {
						await botClient.SendTextMessageAsync(message.Chat, $"FormatException Error!\nYou entered an unexpected value (string instead of integer?)\nTry /ping to check if bot still alive!");
						return;

					} catch (NullReferenceException) {
						await botClient.SendTextMessageAsync(message.Chat, $"NullReferenceException Error!\n Something unknown went wrong!\nTry /ping to check if bot still alive!");
						return;

					} catch (Exception) {
						await botClient.SendTextMessageAsync(message.Chat, $"Unknown Exception Error!\nTry /ping to check if bot still alive!");
						return;

					} finally {
						Tbot.Program.releaseFeature();

					}	
				}
			}
		}

		async Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken) {
			if (exception is ApiRequestException apiRequestException) {
				await botClient.SendTextMessageAsync(Channel, apiRequestException.ToString());
			}
		}

		public async void TelegramBot() {
			
			var cts = new CancellationTokenSource();
			var cancellationToken = cts.Token;

			var receiverOptions = new ReceiverOptions {
				AllowedUpdates = Array.Empty<UpdateType>(),
				ThrowPendingUpdates = true
			};
			
			await Client.ReceiveAsync(HandleUpdateAsync, HandleErrorAsync, receiverOptions, cts.Token);
		}
	}
}
