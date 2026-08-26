namespace PetroVisorLite.Web.Models;

// These mirror the backend's PetroVisorLite.Application.Dtos / Analytics DTOs.
// Kept as plain records in the frontend project since the frontend does not
// reference the backend assemblies directly (separate deployable app).

public record LoginRequest(string Email, string Password);

public record LoginResponse(string Token, string Email, IReadOnlyList<string> Roles, DateTime ExpiresAtUtc);

public record WellDto(Guid Id, string Name, string ApiNumber, double Latitude, double Longitude);

public record FacilityDto(Guid Id, string Name, string Type);

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

public record ProductionKpiDto(Guid WellId, DateOnly PeriodStart, DateOnly PeriodEnd, double TotalOilBbl, double TotalGasMcf, double AverageDailyOilBbl);

public record FieldDailyProductionDto(DateOnly Date, double OilVolumeBbl, double GasVolumeMcf, double WaterVolumeBbl);

public record ArtificialLiftBreakdownDto(string ArtificialLiftType, int WellCount, double TotalOilBbl30d);

public record WellDeclineRankingDto(Guid WellId, string WellName, string ApiNumber, double? DailyDeclinePercent, double? AnnualDeclinePercent);

public record DashboardSummaryDto(
    int WellCount,
    int FacilityCount,
    double TotalOilBbl30d,
    double TotalGasMcf30d,
    IReadOnlyList<FieldDailyProductionDto> FieldDailyProduction,
    IReadOnlyList<ArtificialLiftBreakdownDto> ArtificialLiftBreakdown,
    IReadOnlyList<WellDeclineRankingDto> TopWellsByDecline);

public record CsvImportRowErrorDto(int RowNumber, string Reason);

public class CsvImportResultDto
{
    public int RowsImported { get; set; }
    public int RowsFailed { get; set; }
    public List<CsvImportRowErrorDto> Errors { get; set; } = new();
}
