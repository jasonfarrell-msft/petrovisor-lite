using PetroVisorLite.Domain.Enums;

namespace PetroVisorLite.Domain;

/// <summary>
/// Represents a physical well. Plain POCO with no persistence concerns —
/// extend later with completions, reservoir, and ESG-related properties.
/// </summary>
public class Well
{
    public Guid Id { get; set; }

    /// <summary>Human-readable well name (e.g. "Eagle 12-3H").</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Unique regulatory well identifier (API number in the US, UWI elsewhere).</summary>
    public string ApiNumber { get; set; } = string.Empty;

    public double Latitude { get; set; }
    public double Longitude { get; set; }

    /// <summary>Current artificial lift equipment installed on this well.</summary>
    public ArtificialLiftType ArtificialLiftType { get; set; } = ArtificialLiftType.None;

    /// <summary>Current operating status of the artificial lift system.</summary>
    public ArtificialLiftStatus ArtificialLiftStatus { get; set; } = ArtificialLiftStatus.Unknown;

    public Guid? FacilityId { get; set; }
    public Facility? Facility { get; set; }
    public ICollection<ProductionRecord> ProductionRecords { get; set; } = new List<ProductionRecord>();
}
