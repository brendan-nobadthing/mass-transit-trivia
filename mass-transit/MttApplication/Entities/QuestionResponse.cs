using MassTransit;

namespace MttApplication.Entities;

public class QuestionResponse
{
    public Guid CorrelationId { get; set; }
    public Guid ParticipantId { get; set; }

    public int? QuestionIndex { get; set; }
    public string? Answer { get; set; }
    public DateTime Timestamp { get; set; }
}