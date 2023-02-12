using Telegram.Bot.Polling;

namespace LocalConsoleTest;

internal class ReceiverService : DefaultUpdateReceiver {
    private readonly MyBot _botClient;

    public ReceiverService(MyBot botClient, ReceiverOptions? receiverOptions = null) :
        base(botClient, receiverOptions) {
        _botClient = botClient;
    }

    public async Task ReceiveAsync() {
        await base.ReceiveAsync(_botClient);
    }
}
