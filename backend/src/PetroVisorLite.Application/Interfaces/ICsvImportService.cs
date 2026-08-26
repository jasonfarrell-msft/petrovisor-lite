using PetroVisorLite.Application.Dtos;

namespace PetroVisorLite.Application.Interfaces;

/// <summary>
/// Contract for importing well/production data from CSV files.
/// </summary>
public interface ICsvImportService
{
    Task<CsvImportResultDto> ImportProductionRecordsAsync(Stream csvStream, CancellationToken cancellationToken = default);
}
