using System.Security.Cryptography.X509Certificates;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using MttApplication.Consumers;
using MttApplication.Contracts;
using Shouldly;

namespace MttTests.Consumers;

public class FetchQuestionsTests
{

    [Fact]
    public async Task ShouldFetchQuestions()
    {
        await using var provider = new ServiceCollection()
            .AddHttpClient()
            .AddMassTransitTestHarness(x =>
            {
                x.AddConsumer<FetchQuestionsConsumer>();
            })
            .BuildServiceProvider(true);
        
        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        var client = harness.GetRequestClient<FetchQuestions>();
        var command = new FetchQuestions { CorrelationId = Guid.NewGuid(), };
        var response = await client.GetResponse<QuestionsFetched>(command);
        
        response.CorrelationId.ShouldBe(command.CorrelationId);
        response.Message.Questions.ShouldNotBeEmpty();
    }
    
}