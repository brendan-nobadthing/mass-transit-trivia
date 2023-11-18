using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using MttApplication.Contracts;
using Shouldly;

namespace MttTests.Sagas;

/// <summary>
/// Test operation with inmemory config without test harness
/// </summary>
public class InMemoryTests
{

    [Fact]
    public async Task CreateGame()
    {
        await using var provider = new ServiceCollection()
            .AddHttpClient()
            .AddMassTransit(x =>
            {
                x.SetKebabCaseEndpointNameFormatter();

                // By default, sagas are in-memory, but should be changed to a durable
                // saga repository.
                x.SetInMemorySagaRepositoryProvider();

                var mtAssembly = (typeof(CreateGame).Assembly);

                x.AddConsumers(mtAssembly);
                x.AddSagaStateMachines(mtAssembly);
                x.AddSagas(mtAssembly);
                x.AddActivities(mtAssembly);

                x.UsingInMemory((context, cfg) =>
                {
                    cfg.ConfigureEndpoints(context);
                });
            })
            .BuildServiceProvider(true);

        var busControl = provider.GetRequiredService<IBusControl>();
        await busControl.StartAsync();
        var bus = provider.GetRequiredService<IBus>();

        await bus.Publish(new CreateGame() { CorrelationId = Guid.NewGuid() });
        await Task.Delay(2000);
        await busControl.StopAsync();
    }
    
    
    
    [Fact]
    public async Task GetQuestions()
    {
        await using var provider = new ServiceCollection()
            .AddHttpClient()
            .AddMassTransit(x =>
            {
                x.SetKebabCaseEndpointNameFormatter();

                // By default, sagas are in-memory, but should be changed to a durable
                // saga repository.
                x.SetInMemorySagaRepositoryProvider();

                var mtAssembly = (typeof(CreateGame).Assembly);

                x.AddConsumers(mtAssembly);
                x.AddSagaStateMachines(mtAssembly);
                x.AddSagas(mtAssembly);
                x.AddActivities(mtAssembly);

                x.UsingInMemory((context, cfg) =>
                {
                    cfg.ConfigureEndpoints(context);
                });
            })
            .BuildServiceProvider(true);


        var busControl = provider.GetRequiredService<IBusControl>();
        await busControl.StartAsync();
        var bus = provider.GetRequiredService<IBus>();

        var response =
            await bus.Request<FetchQuestions, QuestionsFetched>(new FetchQuestions()
                { CorrelationId = Guid.NewGuid() });
        response.Message.Questions.ShouldNotBeEmpty();

        await busControl.StopAsync();

    }
    
    

}