using MassTransit;

public class MttReceiveObserver : IReceiveObserver
{
    public async Task PreReceive(ReceiveContext context)
    {
        await Console.Out.WriteLineAsync($"receiving message: {context.GetBody()}");
    }

    public Task PostReceive(ReceiveContext context)
    {
        return Task.CompletedTask;
    }

    public Task PostConsume<T>(ConsumeContext<T> context, TimeSpan duration, string consumerType) where T : class
    {
        return Task.CompletedTask;
    }

    public Task ConsumeFault<T>(ConsumeContext<T> context, TimeSpan duration, string consumerType, Exception exception) where T : class
    {
        return Task.CompletedTask;
    }

    public Task ReceiveFault(ReceiveContext context, Exception exception)
    {
        return Task.CompletedTask;
    }
}