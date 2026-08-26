using Microsoft.AspNetCore.Mvc;
using PetroVisorLite.Application.Dtos;
using PetroVisorLite.Application.Interfaces;
using PetroVisorLite.Domain;

namespace PetroVisorLite.Api.Controllers;

/// <summary>Facility CRUD. Reads available to Engineer + Viewer; writes restricted to Engineer.</summary>
[ApiController]
[Route("api/facilities")]
public class FacilitiesController : ControllerBase
{
    private readonly IFacilityRepository _facilityRepository;

    public FacilitiesController(IFacilityRepository facilityRepository) => _facilityRepository = facilityRepository;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<FacilityDto>>> GetAll(CancellationToken cancellationToken)
    {
        var facilities = await _facilityRepository.GetAllAsync(cancellationToken);
        return Ok(facilities.Select(ToDto));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<FacilityDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var facility = await _facilityRepository.GetByIdAsync(id, cancellationToken);
        return facility is null ? NotFound() : Ok(ToDto(facility));
    }

    [HttpPost]
    public async Task<ActionResult<FacilityDto>> Create([FromBody] FacilityDto dto, CancellationToken cancellationToken)
    {
        var facility = new Facility { Id = Guid.NewGuid(), Name = dto.Name, Type = dto.Type };
        await _facilityRepository.AddAsync(facility, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = facility.Id }, ToDto(facility));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] FacilityDto dto, CancellationToken cancellationToken)
    {
        var facility = await _facilityRepository.GetByIdAsync(id, cancellationToken);
        if (facility is null) return NotFound();

        facility.Name = dto.Name;
        facility.Type = dto.Type;

        await _facilityRepository.UpdateAsync(facility, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _facilityRepository.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    private static FacilityDto ToDto(Facility facility) => new(facility.Id, facility.Name, facility.Type);
}
