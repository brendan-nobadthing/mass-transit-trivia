using System.Data.Common;
using Marten;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using MttApplication.Contracts;
using MttApplication.Sagas;
using Npgsql;
using Shouldly;
using Testcontainers.PostgreSql;

namespace MttPersistenceIntegrationTests;


public class MartenPersistenceForGameStateTests: IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgreSqlContainer = new PostgreSqlBuilder().Build();
    public Task InitializeAsync()
    {
        return _postgreSqlContainer.StartAsync();
    }

    public Task DisposeAsync()
    {
        return _postgreSqlContainer.DisposeAsync().AsTask();
    }

    [Fact]
    public async Task ShouldPersistGameState()
    {
        var services =  new ServiceCollection()
            .AddHttpClient();
            services.AddMarten(opt => opt.Connection(_postgreSqlContainer.GetConnectionString()));
            services.AddMassTransit(x =>
            {
                x.SetMartenSagaRepositoryProvider();
                x.AddSagaRepository<GameState>()
                    .MartenRepository();
                
                
                x.SetKebabCaseEndpointNameFormatter();

                var mtAssembly = (typeof(CreateGame).Assembly);

                x.AddConsumers(mtAssembly);
                x.AddSagaStateMachines(mtAssembly);
                x.AddSagas(mtAssembly);
                x.AddActivities(mtAssembly);
                
                x.UsingInMemory((context, cfg) =>
                {
                    cfg.ConfigureEndpoints(context);
                });
            });
            await using var provider = services.BuildServiceProvider(true);
            
            var busControl = provider.GetRequiredService<IBusControl>();
            await busControl.StartAsync();
            var bus = provider.GetRequiredService<IBus>();

            await bus.Publish(new CreateGame() { CorrelationId = Guid.NewGuid() });
            await busControl.StopAsync();
            
            // assert record created
            await using DbConnection connection = new NpgsqlConnection(_postgreSqlContainer.GetConnectionString());
            await using DbCommand command = new NpgsqlCommand();
            await connection.OpenAsync();
            command.Connection = connection;
            command.CommandText = "SELECT COUNT(id) from mt_doc_gamestate";

            var count = await command.ExecuteScalarAsync();
            count.ShouldBe(1);
    }
}