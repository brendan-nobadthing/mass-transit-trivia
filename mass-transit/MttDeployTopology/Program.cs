﻿using Microsoft.Extensions.Hosting;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MttApplication.Contracts;

namespace MttDeployTopology
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = CreateHostBuilder(args);
            var host = builder.Build();
            //await host.RunAsync();

            var busControl = host.Services.GetRequiredService<IBusControl>();
            try
            {
                using var source = new CancellationTokenSource(TimeSpan.FromMinutes(2));
    
                Console.WriteLine("Deploy Topology...");
                await busControl.DeployAsync(source.Token);

                Console.WriteLine("Topology Deployed");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to deploy topology: {0}", ex);
            }
            
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddUserSecrets<Program>()
                .Build();
            
            return Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddMassTransit(x =>
                    {
                        x.SetKebabCaseEndpointNameFormatter();
                        //x.SetMartenSagaRepositoryProvider();

                        var mtAssembly = (typeof(CreateGame).Assembly);

                        x.AddConsumers(mtAssembly);
                        x.AddSagaStateMachines(mtAssembly);
                        x.AddActivities(mtAssembly);

                        //x.AddSagaRepository<GameState>()
                        //.MartenRepository();
                        //x.UsingInMemory((context, cfg) => { cfg.ConfigureEndpoints(context); });
                        x.SetInMemorySagaRepositoryProvider();
                        x.UsingAmazonSqs((context, cfg) =>
                        {
                            
                            cfg.Host("ap-southeast-2", h =>
                            {
                                h.AccessKey(config["user-access-key"]);
                                h.SecretKey(config["user-secret"]);
                                h.Scope("brendan-trivia", true);
                                h.EnableScopedTopics();
                            });
                            cfg.DeployTopologyOnly = true;
                            cfg.ConfigureEndpoints(context);
                        });
                    });
                });
        }
            
    }
}