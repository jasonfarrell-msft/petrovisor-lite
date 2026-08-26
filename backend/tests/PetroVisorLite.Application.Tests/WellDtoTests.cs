using PetroVisorLite.Application.Dtos;

namespace PetroVisorLite.Application.Tests;

/// <summary>Smoke test proving the test harness/project references are wired correctly.</summary>
public class WellDtoTests
{
    [Fact]
    public void WellDto_CanBeConstructed_WithExpectedValues()
    {
        var dto = new WellDto(Guid.NewGuid(), "Well A-1", "42-000-00001", 31.9, -102.1);

        Assert.Equal("Well A-1", dto.Name);
        Assert.Equal("42-000-00001", dto.ApiNumber);
    }
}
