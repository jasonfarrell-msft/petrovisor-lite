namespace PetroVisorLite.Domain.Enums;

/// <summary>Type of artificial lift equipment installed on a well, if any.</summary>
public enum ArtificialLiftType
{
    None = 0,
    RodPump = 1,
    Esp = 2,
    GasLift = 3,
    Plunger = 4,
    Pcp = 5,
}

/// <summary>Operating status of the artificial lift system at the time of a reading.</summary>
public enum ArtificialLiftStatus
{
    Unknown = 0,
    Running = 1,
    Down = 2,
    Maintenance = 3,
}
