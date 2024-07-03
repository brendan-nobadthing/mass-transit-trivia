using MassTransit;

namespace MttApi;

public class MttPublishObserver: IPublishObserver
{
    public async Task PrePublish<T>(PublishContext<T> context) where T : class
    {
        await Console.Out.WriteLineAsync($"Publishing message, Id: {context.MessageId}, destination: {context.DestinationAddress}");
    }

    public async Task PostPublish<T>(PublishContext<T> context) where T : class
    {
        await Console.Out.WriteLineAsync($"Message Published, Id: {context.MessageId}, destination: {context.DestinationAddress}");
    }

    public async Task PublishFault<T>(PublishContext<T> context, Exception exception) where T : class
    { 
        await Console.Out.WriteLineAsync($"Message failed: {exception.Message}");
    }
}