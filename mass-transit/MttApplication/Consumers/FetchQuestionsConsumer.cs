using MassTransit;
using MttApplication.Contracts;
using MttApplication.Entities;
using System.Text.Json;
// ReSharper disable InconsistentNaming

namespace MttApplication.Consumers;

public class FetchQuestionsConsumer: IConsumer<FetchQuestions>
{
    private readonly HttpClient _httpClient;

    public FetchQuestionsConsumer(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient();
    }

    public async Task Consume(ConsumeContext<FetchQuestions> context)
    {
        var response = await _httpClient.GetAsync("https://the-trivia-api.com/v2/questions?limit=10");
        var apiQuestions =  JsonSerializer.Deserialize<IList<ApiQuestion>>(await response.Content.ReadAsStringAsync());
        var index = 0;
        await context.RespondAsync(new QuestionsFetched()
        {
            CorrelationId = context.Message.CorrelationId,
            Questions = apiQuestions.Select(q => new Question()
            {
                QuestionIndex = index++,
                CorrectAnswer = q.correctAnswer,
                IncorrectAnswers = q.incorrectAnswers,
                QuestionText = q.question.text
            }).ToList()
        });
    }
}


// these classes generasted from api spec by json2csharp
public class ApiQuestionText
{
    public string? text { get; set; }
}

public class ApiQuestion
{
    public string? category { get; set; }
    public string? id { get; set; }
    public string? correctAnswer { get; set; }
    public List<string> incorrectAnswers { get; set; } = new List<string>();
    public ApiQuestionText? question { get; set; }
    public List<string> tags { get; set; } = new List<string>();
    public string? type { get; set; }
    public string? difficulty { get; set; }
    public List<object> regions { get; set; } = new List<object>();
    public bool? isNiche { get; set; }
}

