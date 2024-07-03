using MassTransit;

namespace MttApi;

public class MttSendObserver: ISendObserver
{
    public async Task PreSend<T>(SendContext<T> context) where T : class
    {
        await Console.Out.WriteLineAsync($"Sending message, Id: {context.MessageId}, destination: {context.DestinationAddress}");
    }

    public async Task PostSend<T>(SendContext<T> context) where T : class
    {
        await Console.Out.WriteLineAsync($"Message Sent, Id: {context.MessageId}, destination: {context.DestinationAddress}");
    }

    public async Task SendFault<T>(SendContext<T> context, Exception exception) where T : class
    {
        await Console.Out.WriteLineAsync($"Message failed: {exception.Message}");
    }
}