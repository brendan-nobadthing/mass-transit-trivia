using MassTransit;

namespace MttApplication.Entities;

public class Participant
{
    public Guid ParticipantId { get; set; }
    
    public string? DisplayName { get; set; }

    public string? Email { get; set; }
}