using PetroVisorLite.Application.Dtos;

namespace PetroVisorLite.Application.Interfaces;

/// <summary>
/// Thin composition point: a classified intent is translated into a real repository/service call and
/// shaped into a backend-sourced result DTO with no LLM-generated numbers.
/// </summary>
public interface IQueryOrchestrator
{
    Task<AssistantQueryResponse> ExecuteAsync(QueryIntentClassification classification, CancellationToken cancellationToken = default);
}
