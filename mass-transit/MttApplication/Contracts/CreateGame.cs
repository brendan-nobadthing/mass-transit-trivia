using MassTransit;

namespace MttApplication.Contracts;

public class CreateGame: CorrelatedBy<Guid>
{
    public Guid CorrelationId { get; set; }
    public string? Name { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
