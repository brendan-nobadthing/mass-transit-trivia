using MassTransit;
using MttApplication.Entities;

namespace MttApplication.Contracts;

public class GetParticipantState: CorrelatedBy<Guid>
{
    public Guid CorrelationId { get; set; }
    public Guid ParticipantId { get; set; }
}

public class ParticipantStateResponse: CorrelatedBy<Guid>
{
    public Guid CorrelationId { get; set; }

    public string? CurrentState { get; set; }

    public int? CurrentQuestionIndex { get; set; }
    
    public ParticipantQuestion? CurrentQuestion { get; set; }

    public IList<QuestionResponse> QuestionResponses { get; set; } = new List<QuestionResponse>();

    public IList<QuestionResponseScore> QuestionResponseScores { get; set; } = new List<QuestionResponseScore>();
    
}