namespace PetroVisorLite.Application.Dtos;

public record ProductionRecordDto(
    Guid Id,
    Guid WellId,
    DateOnly Date,
    double OilVolumeBbl,
    double GasVolumeMcf,
    double WaterVolumeBbl,
    double ChokeSize64th,
    double WellheadPressurePsi,
    string ArtificialLiftType,
    string ArtificialLiftStatus);
