namespace MttApplication.Entities;

public class Question
{

    public int? QuestionIndex { get; set; }
    
    public string? QuestionText { get; set; }

    public string? CorrectAnswer { get; set; }

    public IList<string> IncorrectAnswers { get; set; } = new List<string>();

    public DateTime? QuestionOpened { get; set; }

    public DateTime? QuestionClosed { get; set; }

}

