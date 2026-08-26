namespace PetroVisorLite.Domain;

/// <summary>
/// A surface facility (battery, gathering station, processing plant) that
/// groups one or more wells.
/// </summary>
public class Facility
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public ICollection<Well> Wells { get; set; } = new List<Well>();
}
