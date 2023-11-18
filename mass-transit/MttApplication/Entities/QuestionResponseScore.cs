using MassTransit;

namespace MttApplication.Entities;

public class QuestionResponseScore
{
    public Guid CorrelationId { get; set; }
    public Guid ParticipantId { get; set; }
    public int? QuestionIndex { get; set; }
    public int? Score { get; set; }
}