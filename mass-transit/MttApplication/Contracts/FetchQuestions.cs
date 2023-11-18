using MassTransit;
using MttApplication.Entities;

namespace MttApplication.Contracts;

public class FetchQuestions: CorrelatedBy<Guid>
{
    public Guid CorrelationId { get; set; }
    
}

public class QuestionsFetched: CorrelatedBy<Guid>
{
    public Guid CorrelationId { get; set; }

    public IList<Question> Questions { get; set; } = new List<Question>();

}