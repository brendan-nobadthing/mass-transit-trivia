using System.Net;
using System.Security.Cryptography.X509Certificates;
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
        Console.WriteLine($"Server={dbHost};Port={dbPort};Database=postgres;User Id={dbUsername};Password={dbPassword};");
        services.AddSingleton<AwsSecrets>();
        services.AddMassTransit(x =>
        {
            x.SetEndpointNameFormatter(new KebabCaseEndpointNameFormatter("brendan-trivia", false));
           
            x.SetMartenSagaRepositoryProvider();
            x.AddSagaRepository<GameState>().MartenRepository();
            
            var mtAssembly = (typeof(CreateGame).Assembly);
            x.AddConsumers(mtAssembly);
            x.AddSagaStateMachines(mtAssembly);
            x.AddSagas(mtAssembly);
            x.AddActivities(mtAssembly);
            
            x.UsingInMemory((context, cfg) =>
            {
                cfg.ConfigureEndpoints(context);
            });
            // x.UsingAmazonSqs((context, cfg) =>
            // {
            //     cfg.Host("ap-southeast-2", h =>
            //     {
            //         h.AccessKey(userAccessKey);
            //         h.SecretKey(userSecret);
            //         h.EnableScopedTopics();
            //         h.Scope("brendan-trivia", true);
            //     });
            //     cfg.ConfigureEndpoints(context);
            // });
        });
        return services.BuildServiceProvider(true);
    }

    public async Task SQSHandler(SQSEvent evnt, ILambdaContext context)
    {
        try
        {
            using var cts = new CancellationTokenSource(context.RemainingTime);
            var serviceProvider = await _buildServiceProviderTask;
            using var scope = serviceProvider.CreateScope();
            // var busControl = scope.ServiceProvider.GetRequiredService<IBusControl>();
            // await busControl.StartAsync(cts.Token);
        
            var dispatcher = scope.ServiceProvider.GetRequiredService<IReceiveEndpointDispatcher<GameState>>();
        
            await Console.Out.WriteLineAsync($"processing {evnt.Records.Count} records");
            foreach (var record in evnt.Records)
            {
                await Console.Out.WriteLineAsync("SQS Handler Got Message: "+record.Body);
                var sqsMessageJson = JsonNode.Parse(record.Body);
                var mtMessage = sqsMessageJson!["Message"]!.GetValue<string>();
                await Console.Out.WriteLineAsync("Got inner message: "+mtMessage);
                await dispatcher.Dispatch(Encoding.UTF8.GetBytes(mtMessage), new Dictionary<string, object>(), cts.Token);
                
            } 
        } catch (Exception e)
        {
            await Console.Out.WriteLineAsync("SQSHandler Exception: "+e.Message);
        }
    }
    
}