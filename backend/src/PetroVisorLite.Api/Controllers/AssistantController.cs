using Microsoft.AspNetCore.Mvc;
using PetroVisorLite.Application.Dtos;
using PetroVisorLite.Application.Interfaces;

namespace PetroVisorLite.Api.Controllers;

/// <summary>
/// Thin HTTP surface for Ask PetroVisor intent routing. The controller does not generate numbers;
/// it delegates to Application-layer orchestration and Infrastructure-backed intent classification.
/// </summary>
[ApiController]
[Route("api/assistant")]
public class AssistantController : ControllerBase
{
    private readonly IQueryIntentClassifier _intentClassifier;
    private readonly IQueryOrchestrator _queryOrchestrator;

    public AssistantController(IQueryIntentClassifier intentClassifier, IQueryOrchestrator queryOrchestrator)
    {
        _intentClassifier = intentClassifier;
        _queryOrchestrator = queryOrchestrator;
    }

    [HttpPost("query")]
    public async Task<ActionResult<AssistantQueryResponse>> Query([FromBody] AssistantQueryRequest request, CancellationToken cancellationToken)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Question))
        {
            return Ok(new AssistantQueryResponse(QueryIntent.Unsupported, false, "I can't answer that yet."));
        }

        var classifiedIntent = await _intentClassifier.ClassifyAsync(request.Question, cancellationToken);
        var response = await _queryOrchestrator.ExecuteAsync(classifiedIntent, cancellationToken);
        return Ok(response);
    }
}
