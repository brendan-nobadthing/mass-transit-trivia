using MassTransit;

namespace MttApplication.Contracts;

public class NextQuestion: CorrelatedBy<Guid>
{
    public Guid CorrelationId { get; set; }
}