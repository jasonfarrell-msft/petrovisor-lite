using PetroVisorLite.Application.Dtos;
using PetroVisorLite.Application.Interfaces;
using PetroVisorLite.Domain;

namespace PetroVisorLite.Application.Analytics;

/// <summary>
/// Aggregates the facility comparison view from the facility source of truth, then enriches each
/// facility row with tied-in well counts and production totals for the requested period.
/// </summary>
public class FacilityComparisonService : IFacilityComparisonService
{
    private readonly IFacilityRepository _facilityRepository;
    private readonly IProductionRecordRepository _productionRecordRepository;

    public FacilityComparisonService(
        IFacilityRepository facilityRepository,
        IProductionRecordRepository productionRecordRepository)
    {
        _facilityRepository = facilityRepository;
        _productionRecordRepository = productionRecordRepository;
    }

    public async Task<FacilityComparisonDto> GetFacilityComparisonAsync(DateOnly rangeStart, DateOnly rangeEnd, CancellationToken cancellationToken = default)
    {
        var facilities = await _facilityRepository.GetAllAsync(cancellationToken);
        var rows = new List<FacilityComparisonRowDto>();

        foreach (var facility in facilities)
        {
            var wells = facility.Wells ?? Array.Empty<Well>();
            var tiedInWellCount = wells.Count;
            var totalOil = 0d;
            var totalGas = 0d;
            var totalWater = 0d;
            var hasReportedProduction = false;

            foreach (var well in wells)
            {
                var records = await _productionRecordRepository.GetByWellIdAsync(well.Id, rangeStart, rangeEnd, cancellationToken);
                foreach (var record in records)
                {
                    hasReportedProduction = true;
                    totalOil += record.OilVolumeBbl;
                    totalGas += record.GasVolumeMcf;
                    totalWater += record.WaterVolumeBbl;
                }
            }

            rows.Add(new FacilityComparisonRowDto(
                facility.Id,
                facility.Name,
                facility.Type,
                tiedInWellCount,
                totalOil,
                totalGas,
                totalWater,
                !hasReportedProduction));
        }

        var orderedRows = rows
            .OrderByDescending(row => row.TotalOilBbl)
            .ThenBy(row => row.FacilityName, StringComparer.Ordinal)
            .ToList();

        return new FacilityComparisonDto(rangeStart, rangeEnd, orderedRows);
    }
}
