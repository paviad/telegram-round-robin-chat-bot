using Telegram.Bot.Polling;

namespace RrBot;

internal class ReceiverService(MyBot botClient, ReceiverOptions? receiverOptions = null)
    : DefaultUpdateReceiver(botClient, receiverOptions) {
    public async Task ReceiveAsync() {
        await base.ReceiveAsync(botClient);
    }
}
