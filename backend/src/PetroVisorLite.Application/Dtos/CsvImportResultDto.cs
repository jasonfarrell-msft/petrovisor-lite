namespace PetroVisorLite.Application.Dtos;

/// <summary>Result of a CSV production-data import: successes and per-row failure reasons.</summary>
public class CsvImportResultDto
{
    public int RowsImported { get; set; }
    public int RowsFailed { get; set; }
    public List<CsvImportRowErrorDto> Errors { get; set; } = new();
}

/// <summary>A single row that failed to import, with its (1-based, header-exclusive) row number and reason.</summary>
public record CsvImportRowErrorDto(int RowNumber, string Reason);
