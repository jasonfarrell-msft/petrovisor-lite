using PetroVisorLite.Application.Dtos;

namespace PetroVisorLite.Application.Interfaces;

/// <summary>
/// Maps a user question to one of the approved query intents. The concrete implementation in
/// Infrastructure calls Azure AI Foundry with a constrained JSON schema, but the abstraction keeps
/// the orchestration layer pure and unit-testable.
/// </summary>
public interface IQueryIntentClassifier
{
    Task<QueryIntentClassification> ClassifyAsync(string userQuestion, CancellationToken cancellationToken = default);
}
