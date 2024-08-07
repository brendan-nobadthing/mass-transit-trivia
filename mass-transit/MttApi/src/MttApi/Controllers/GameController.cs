using Amazon.Runtime.Endpoints;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using MttApplication.Contracts;

namespace MttApi.Controllers;

[ApiController]
[Route("[controller]")]
public class GameController
{
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<GameController> _log;
    public GameController(IPublishEndpoint publishEndpoint, ILogger<GameController> log)
    {
        _publishEndpoint = publishEndpoint;
        _log = log;
    }

    [HttpGet("create")]
    public async Task<ActionResult<Guid>> CreateGame(
        [FromQuery] string name)
    {
        _log.LogInformation("Publish CreateGame");
        var id = Guid.NewGuid();
        await _publishEndpoint.Publish(new CreateGame()
        {
            Name = name,
            CorrelationId = id,
            CreatedAt = DateTime.UtcNow
        });
        return id;
    }
    
    
    [HttpGet("hello")]
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    public async Task<ActionResult<string>> Hello()
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    {
        return new OkObjectResult("Hello");
    }
    
    
}