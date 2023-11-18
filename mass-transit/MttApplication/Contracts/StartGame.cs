using MassTransit;

namespace MttApplication.Contracts;

public class StartGame: CorrelatedBy<Guid>
{
    public Guid CorrelationId { get; set; }
}