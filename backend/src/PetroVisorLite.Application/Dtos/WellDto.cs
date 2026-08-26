namespace PetroVisorLite.Application.Dtos;

/// <summary>Example DTO — extend/replace as Han builds out use cases.</summary>
public record WellDto(Guid Id, string Name, string ApiNumber, double Latitude, double Longitude);
