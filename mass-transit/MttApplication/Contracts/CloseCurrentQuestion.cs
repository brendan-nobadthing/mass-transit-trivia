using MassTransit;

namespace MttApplication.Contracts;

public class CloseCurrentQuestion : CorrelatedBy<Guid>
{
    public Guid CorrelationId { get; set;  }
}