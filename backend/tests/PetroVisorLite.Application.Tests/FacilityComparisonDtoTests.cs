using PetroVisorLite.Application.Dtos;

namespace PetroVisorLite.Application.Tests;

/// <summary>
/// Smoke tests proving the Story #11 facility comparison contract is wired correctly and
/// that its default comparison order (total oil descending, facility name ascending
/// tie-breaker) is unambiguous for consumers to implement.
/// </summary>
public class FacilityComparisonDtoTests
{
    [Fact]
    public void FacilityComparisonDto_CanBeConstructed_WithExpectedValues()
    {
        var range = (Start: new DateOnly(2024, 1, 1), End: new DateOnly(2024, 1, 31));
        var row = new FacilityComparisonRowDto(
            FacilityId: Guid.NewGuid(),
            FacilityName: "Facility A",
            FacilityType: "Battery",
            TiedInWellCount: 5,
            TotalOilBbl: 1200.5,
            TotalGasMcf: 3400.0,
            TotalWaterBbl: 800.25,
            HasNoReportedProduction: false);

        var dto = new FacilityComparisonDto(range.Start, range.End, new[] { row });

        Assert.Equal(range.Start, dto.RangeStart);
        Assert.Equal(range.End, dto.RangeEnd);
        Assert.Single(dto.Facilities);
        Assert.Equal("Facility A", dto.Facilities[0].FacilityName);
        Assert.False(dto.Facilities[0].HasNoReportedProduction);
    }

    [Fact]
    public void FacilityComparisonRowDto_NoReportedProduction_IsFlagged()
    {
        var row = new FacilityComparisonRowDto(
            FacilityId: Guid.NewGuid(),
            FacilityName: "Facility B",
            FacilityType: "Battery",
            TiedInWellCount: 2,
            TotalOilBbl: 0,
            TotalGasMcf: 0,
            TotalWaterBbl: 0,
            HasNoReportedProduction: true);

        Assert.True(row.HasNoReportedProduction);
    }

    [Fact]
    public void DefaultComparisonOrder_IsTotalOilDescending_ThenFacilityNameAscending()
    {
        var zulu = new FacilityComparisonRowDto(Guid.NewGuid(), "Zulu", "Battery", 1, 500, 0, 0, false);
        var alpha = new FacilityComparisonRowDto(Guid.NewGuid(), "Alpha", "Battery", 1, 500, 0, 0, false);
        var bravo = new FacilityComparisonRowDto(Guid.NewGuid(), "Bravo", "Battery", 1, 900, 0, 0, false);

        var ordered = new[] { zulu, alpha, bravo }
            .OrderByDescending(f => f.TotalOilBbl)
            .ThenBy(f => f.FacilityName, StringComparer.Ordinal)
            .ToList();

        Assert.Equal(new[] { "Bravo", "Alpha", "Zulu" }, ordered.Select(f => f.FacilityName));
    }
}
