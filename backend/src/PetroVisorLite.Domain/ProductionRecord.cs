using PetroVisorLite.Domain.Enums;

namespace PetroVisorLite.Domain;

/// <summary>
/// A single daily production reading for a well: volumes, choke size,
/// wellhead pressure, and artificial lift state at the time of the reading.
/// </summary>
public class ProductionRecord
{
    public Guid Id { get; set; }
    public Guid WellId { get; set; }
    public Well? Well { get; set; }

    public DateOnly Date { get; set; }

    public double OilVolumeBbl { get; set; }
    public double GasVolumeMcf { get; set; }
    public double WaterVolumeBbl { get; set; }

    /// <summary>Choke size in 64ths of an inch, as is conventional in the field.</summary>
    public double ChokeSize64th { get; set; }

    /// <summary>Wellhead (tubing) pressure in psi.</summary>
    public double WellheadPressurePsi { get; set; }

    /// <summary>Artificial lift equipment type in effect for this reading (may change over the well's life).</summary>
    public ArtificialLiftType ArtificialLiftType { get; set; } = ArtificialLiftType.None;

    /// <summary>Artificial lift operating status at the time of this reading.</summary>
    public ArtificialLiftStatus ArtificialLiftStatus { get; set; } = ArtificialLiftStatus.Unknown;
}
