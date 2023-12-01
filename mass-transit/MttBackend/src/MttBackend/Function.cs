using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using Marten;
using Marten.Schema.Identity;
using MassTransit;
using MassTransit.Transports;
using MassTransit.Transports.Fabric;
using Microsoft.Extensions.DependencyInjection;
using MttApplication.Contracts;
using MttApplication.Sagas;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace MttBackend;

public class Function
{
    private readonly Task<IServiceProvider> _buildServiceProviderTask;
    private readonly AwsSecrets _awsSecrets;
    
    public Function()
    {
        _awsSecrets = new AwsSecrets();
       _buildServiceProviderTask = BuildServiceProvider();
       
    }

    private async Task<IServiceProvider> BuildServiceProvider()
    {
        var dbHost = await _awsSecrets.GetSecret("brendan-trivia-db-secret", "host");
        var dbUsername = await _awsSecrets.GetSecret("brendan-trivia-db-secret", "username");
        var dbPassword = await _awsSecrets.GetSecret("brendan-trivia-db-secret", "password");
        var dbPort = await _awsSecrets.GetSecret("brendan-trivia-db-secret", "port");
        
        var userAccessKey = await _awsSecrets.GetSecret("application-secrets", "user-access-key");
        var userSecret = await _awsSecrets.GetSecret("application-secrets", "user-secret");
        
        var services = new ServiceCollection();
        services.AddHttpClient();
        services.AddMarten(opt => opt.Connection(
                $"Server={dbHost};Port={dbPort};Database=postgres;User Id={dbUsername};Password={dbPassword};"));
        services.AddSingleton<AwsSecrets>();
        services.AddMassTransit(x =>
        {
            x.SetKebabCaseEndpointNameFormatter();
            x.SetMartenSagaRepositoryProvider();
            
            var mtAssembly = (typeof(CreateGame).Assembly);
            x.AddConsumers(mtAssembly);
            x.AddSagaStateMachines(mtAssembly);
            x.AddSagas(mtAssembly);
            x.AddActivities(mtAssembly);
            
            x.AddSagaRepository<GameState>().MartenRepository();
            
            x.UsingInMemory((context, cfg) =>
            {
                cfg.ConfigureEndpoints(context);
                cfg.UseRawJsonSerializer();
            });
            // x.UsingAmazonSqs((context, cfg) =>
            // {
            //     cfg.Host("ap-southeast-2", h =>
            //     {
            //         h.AccessKey(userAccessKey);
            //         h.SecretKey(userSecret);
            //         h.Scope("brendan-trivia", true);
            //     });
            //     cfg.ConfigureEndpoints(context);
            // });
        });
        return services.BuildServiceProvider(true);
    }

    public async Task SQSHandler(SQSEvent evnt, ILambdaContext context)
    {
        using var cts = new CancellationTokenSource(context.RemainingTime);
        var serviceProvider = await _buildServiceProviderTask;
        using var scope = serviceProvider.CreateScope();
        
        var busControl = scope.ServiceProvider.GetRequiredService<IBusControl>();
        await busControl.StartAsync(cts.Token);
        
        // WORK IN PROGRESS - the below is not quite right yet
        
        var dispatcher = scope.ServiceProvider.GetRequiredService<IReceiveEndpointDispatcher<GameStateMachine>>();

        try
        {
            var receiver = scope.ServiceProvider.GetRequiredService<IMessageReceiver<GameStateMachine>>();
        }
        catch (Exception ex)
        {
            Console.WriteLine("Failed to get receiver");
        }
        
        
        Console.Out.WriteLine($"processing {evnt.Records.Count} records");
        foreach (var message in evnt.Records)
        {
           Console.Out.WriteLine("SQS Handler Got Message: "+message.Body);

           var bus = scope.ServiceProvider.GetRequiredService<IBus>();

           await bus.Publish(new CreateGame()
           {
               CorrelationId = Guid.NewGuid(),
               Name = "TEST-" + Guid.NewGuid().ToString(),
               CreatedAt = DateTime.UtcNow
           });

           // try
           // {
           //     var headers = new Dictionary<string, object>();
           //     foreach (var key in message!.Attributes!.Keys)
           //         headers[key] = message.Attributes[key];
           //     foreach (var key in message!.MessageAttributes!.Keys)
           //         headers[key] = message.MessageAttributes[key];
           //     var body = Encoding.UTF8.GetBytes(message.Body);
           //     
           //     await dispatcher.Dispatch(body, headers, cts.Token);
           //     dispatcher.Dispatch()
           // }
           // catch (Exception dispatchError)
           // {
           //     Console.Out.WriteLine("dispatch Error: "+dispatchError.Message);
           // }

           // try
           // {
           //     var createGame = JsonSerializer.Deserialize<CreateGame>(jObj["message"]);
           //     await receiver.Deliver(createGame, cts.Token);
           // }
           // catch (Exception deliverException)
           // {
           //     Console.Out.WriteLine("deliver Error: "+deliverException.Message);
           // }


        }
        //await busControl.StopAsync();
        
    }
    
  

}