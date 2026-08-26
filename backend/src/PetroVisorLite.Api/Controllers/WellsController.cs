using Microsoft.AspNetCore.Mvc;
using PetroVisorLite.Application.Dtos;
using PetroVisorLite.Application.Interfaces;
using PetroVisorLite.Domain;

namespace PetroVisorLite.Api.Controllers;

/// <summary>Well CRUD. Reads available to Engineer + Viewer; writes restricted to Engineer.</summary>
[ApiController]
[Route("api/wells")]
public class WellsController : ControllerBase
{
    private readonly IWellRepository _wellRepository;

    public WellsController(IWellRepository wellRepository) => _wellRepository = wellRepository;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<WellDto>>> GetAll(CancellationToken cancellationToken)
    {
        var wells = await _wellRepository.GetAllAsync(cancellationToken);
        return Ok(wells.Select(ToDto));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<WellDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var well = await _wellRepository.GetByIdAsync(id, cancellationToken);
        return well is null ? NotFound() : Ok(ToDto(well));
    }

    [HttpPost]
    public async Task<ActionResult<WellDto>> Create([FromBody] WellDto dto, CancellationToken cancellationToken)
    {
        var well = new Well
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            ApiNumber = dto.ApiNumber,
            Latitude = dto.Latitude,
            Longitude = dto.Longitude,
        };
        await _wellRepository.AddAsync(well, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = well.Id }, ToDto(well));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] WellDto dto, CancellationToken cancellationToken)
    {
        var well = await _wellRepository.GetByIdAsync(id, cancellationToken);
        if (well is null) return NotFound();

        well.Name = dto.Name;
        well.ApiNumber = dto.ApiNumber;
        well.Latitude = dto.Latitude;
        well.Longitude = dto.Longitude;

        await _wellRepository.UpdateAsync(well, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _wellRepository.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    private static WellDto ToDto(Well well) => new(well.Id, well.Name, well.ApiNumber, well.Latitude, well.Longitude);
}
