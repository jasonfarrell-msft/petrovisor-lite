using Microsoft.AspNetCore.Mvc;
using PetroVisorLite.Application.Dtos;
using PetroVisorLite.Application.Interfaces;
using PetroVisorLite.Domain;

namespace PetroVisorLite.Api.Controllers;

/// <summary>Production record listing (with date-range filtering) and CSV bulk import.</summary>
[ApiController]
[Route("api/production")]
public class ProductionController : ControllerBase
{
    private readonly IProductionRecordRepository _productionRecordRepository;
    private readonly ICsvImportService _csvImportService;

    public ProductionController(IProductionRecordRepository productionRecordRepository, ICsvImportService csvImportService)
    {
        _productionRecordRepository = productionRecordRepository;
        _csvImportService = csvImportService;
    }

    [HttpGet("well/{wellId:guid}")]
    public async Task<ActionResult<IReadOnlyList<ProductionRecordDto>>> GetByWell(
        Guid wellId,
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        CancellationToken cancellationToken)
    {
        var records = await _productionRecordRepository.GetByWellIdAsync(wellId, from, to, cancellationToken);
        return Ok(records.Select(ToDto));
    }

    /// <summary>
    /// Bulk-imports daily production records from a CSV file. Engineer-only.
    /// Expected header columns (case-insensitive): WellId, Date, OilVolumeBbl, GasVolumeMcf,
    /// WaterVolumeBbl, ChokeSize64th, WellheadPressurePsi, ArtificialLiftType, ArtificialLiftStatus.
    /// </summary>
    [HttpPost("import")]
    [RequestSizeLimit(20_000_000)]
    public async Task<ActionResult<CsvImportResultDto>> Import(IFormFile file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new { message = "No file uploaded." });
        }

        await using var stream = file.OpenReadStream();
        var result = await _csvImportService.ImportProductionRecordsAsync(stream, cancellationToken);
        return Ok(result);
    }

    private static ProductionRecordDto ToDto(ProductionRecord record) => new(
        record.Id, record.WellId, record.Date, record.OilVolumeBbl, record.GasVolumeMcf, record.WaterVolumeBbl,
        record.ChokeSize64th, record.WellheadPressurePsi, record.ArtificialLiftType.ToString(), record.ArtificialLiftStatus.ToString());
}
