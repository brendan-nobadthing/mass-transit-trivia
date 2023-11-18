using MassTransit;
using MttApplication.Entities;

namespace MttApplication.Contracts;

public class AddParticipant: CorrelatedBy<Guid>
{
    public Guid CorrelationId { get; set; }

    public Participant? Participant { get; set; }
}