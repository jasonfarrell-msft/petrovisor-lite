using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using PetroVisorLite.Application.Dtos;
using PetroVisorLite.Application.Interfaces;
using PetroVisorLite.Domain;
using PetroVisorLite.Domain.Enums;

namespace PetroVisorLite.Infrastructure.Services;

/// <summary>
/// Parses production-record CSV uploads and persists valid rows.
/// Expected columns (header row, case-insensitive): WellId, Date, OilVolumeBbl,
/// GasVolumeMcf, WaterVolumeBbl, ChokeSize64th, WellheadPressurePsi,
/// ArtificialLiftType, ArtificialLiftStatus.
/// </summary>
public class CsvImportService : ICsvImportService
{
    private readonly IProductionRecordRepository _productionRecordRepository;
    private readonly IWellRepository _wellRepository;

    public CsvImportService(IProductionRecordRepository productionRecordRepository, IWellRepository wellRepository)
    {
        _productionRecordRepository = productionRecordRepository;
        _wellRepository = wellRepository;
    }

    private sealed class ProductionRecordCsvRow
    {
        public string WellId { get; set; } = string.Empty;
        public string Date { get; set; } = string.Empty;
        public string OilVolumeBbl { get; set; } = string.Empty;
        public string GasVolumeMcf { get; set; } = string.Empty;
        public string WaterVolumeBbl { get; set; } = string.Empty;
        public string ChokeSize64th { get; set; } = string.Empty;
        public string WellheadPressurePsi { get; set; } = string.Empty;
        public string ArtificialLiftType { get; set; } = string.Empty;
        public string ArtificialLiftStatus { get; set; } = string.Empty;
    }

    public async Task<CsvImportResultDto> ImportProductionRecordsAsync(Stream csvStream, CancellationToken cancellationToken = default)
    {
        var result = new CsvImportResultDto();
        var validRecords = new List<ProductionRecord>();
        var knownWellIds = new HashSet<Guid>((await _wellRepository.GetAllAsync(cancellationToken)).Select(w => w.Id));

        using var reader = new StreamReader(csvStream);
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HeaderValidated = null,
            MissingFieldFound = null,
            TrimOptions = TrimOptions.Trim,
        };
        using var csv = new CsvReader(reader, config);

        IEnumerable<ProductionRecordCsvRow> rows;
        try
        {
            csv.Read();
            csv.ReadHeader();
            rows = csv.GetRecords<ProductionRecordCsvRow>().ToList();
        }
        catch (Exception ex)
        {
            result.RowsFailed++;
            result.Errors.Add(new CsvImportRowErrorDto(0, $"Failed to read CSV header/body: {ex.Message}"));
            return result;
        }

        var rowNumber = 1; // row 1 = first data row after the header
        foreach (var row in rows)
        {
            var errors = new List<string>();

            if (!Guid.TryParse(row.WellId, out var wellId))
            {
                errors.Add($"Invalid WellId '{row.WellId}'.");
            }
            else if (!knownWellIds.Contains(wellId))
            {
                errors.Add($"Unknown WellId '{row.WellId}'.");
            }

            if (!DateOnly.TryParse(row.Date, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            {
                errors.Add($"Invalid Date '{row.Date}'.");
            }

            if (!TryParseDouble(row.OilVolumeBbl, out var oil))
            {
                errors.Add($"Invalid OilVolumeBbl '{row.OilVolumeBbl}'.");
            }

            if (!TryParseDouble(row.GasVolumeMcf, out var gas))
            {
                errors.Add($"Invalid GasVolumeMcf '{row.GasVolumeMcf}'.");
            }

            if (!TryParseDouble(row.WaterVolumeBbl, out var water))
            {
                errors.Add($"Invalid WaterVolumeBbl '{row.WaterVolumeBbl}'.");
            }

            if (!TryParseDouble(row.ChokeSize64th, out var choke))
            {
                errors.Add($"Invalid ChokeSize64th '{row.ChokeSize64th}'.");
            }

            if (!TryParseDouble(row.WellheadPressurePsi, out var pressure))
            {
                errors.Add($"Invalid WellheadPressurePsi '{row.WellheadPressurePsi}'.");
            }

            var liftType = ParseEnumOrDefault<ArtificialLiftType>(row.ArtificialLiftType, ArtificialLiftType.None);
            var liftStatus = ParseEnumOrDefault<ArtificialLiftStatus>(row.ArtificialLiftStatus, ArtificialLiftStatus.Unknown);

            if (errors.Count > 0)
            {
                result.RowsFailed++;
                result.Errors.Add(new CsvImportRowErrorDto(rowNumber, string.Join(" ", errors)));
            }
            else
            {
                validRecords.Add(new ProductionRecord
                {
                    Id = Guid.NewGuid(),
                    WellId = wellId,
                    Date = date,
                    OilVolumeBbl = oil,
                    GasVolumeMcf = gas,
                    WaterVolumeBbl = water,
                    ChokeSize64th = choke,
                    WellheadPressurePsi = pressure,
                    ArtificialLiftType = liftType,
                    ArtificialLiftStatus = liftStatus,
                });
                result.RowsImported++;
            }

            rowNumber++;
        }

        if (validRecords.Count > 0)
        {
            await _productionRecordRepository.AddRangeAsync(validRecords, cancellationToken);
        }

        return result;
    }

    private static bool TryParseDouble(string value, out double result) =>
        double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result);

    private static TEnum ParseEnumOrDefault<TEnum>(string value, TEnum defaultValue) where TEnum : struct, Enum =>
        Enum.TryParse<TEnum>(value, ignoreCase: true, out var parsed) ? parsed : defaultValue;
}
