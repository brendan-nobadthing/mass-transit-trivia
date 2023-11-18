namespace MttApplication.Entities;

public class ParticipantQuestion
{
    public ParticipantQuestion()
    {
    }

    public ParticipantQuestion(Question question)
    {
        QuestionIndex = question.QuestionIndex;
        QuestionText = question.QuestionText;
        Answers = question.IncorrectAnswers.Append(question.CorrectAnswer!).Shuffle();
    }

    public int? QuestionIndex { get; set; }
    public string? QuestionText { get; set; }

    public IList<string> Answers { get; set; } = new List<string>();
    
}

public static class ShuffleListExtension
{
    private static Random rng = new Random();

    public static IList<T> Shuffle<T>(this IEnumerable<T> inputList)
    {
        var list = new List<T>().Union(inputList).ToList();
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
        return list;
    }
}