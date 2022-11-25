using System.Threading;
using System.Threading.Tasks;
using Tbot.Services;
using Tbot.Workers;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Tbot.Includes {
	public interface ITelegramMessenger {
		string Api { get; }
		string Channel { get; }
		ITelegramBotClient Client { get; }

		Task AddTbotInstance(TBotMain instance, ITBotOgamedBridge tbotOgameBridge);
		Task RemoveTBotInstance(TBotMain instance);
		Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken);
		Task SendMessage(ITelegramBotClient client, Chat chat, string message, ParseMode parseMode = ParseMode.Html);
		Task SendMessage(string message, ParseMode parseMode = ParseMode.Html, CancellationToken cancellationToken = default);
		Task SendTyping(CancellationToken cancellationToken);
		void StartAutoPing(long everyHours);
		void StopAutoPing();
		Task TelegramBot();
		Task TelegramBotDisable();
	}
}
