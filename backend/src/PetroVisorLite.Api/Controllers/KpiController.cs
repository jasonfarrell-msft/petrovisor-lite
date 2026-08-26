using Microsoft.AspNetCore.Mvc;
using PetroVisorLite.Application.Dtos;
using PetroVisorLite.Application.Interfaces;

namespace PetroVisorLite.Api.Controllers;

/// <summary>
/// KPI endpoints. Thin wrapper around Obi-Wan's <see cref="IKpiService"/> — all calculation
/// logic (decline rate, production loss, artificial lift flags, daily/monthly totals) lives
/// in the Application layer; this controller only validates input and shapes the HTTP response.
/// </summary>
[ApiController]
[Route("api/kpi")]
public class KpiController : ControllerBase
{
    private readonly IKpiService _kpiService;

    public KpiController(IKpiService kpiService) => _kpiService = kpiService;

    /// <summary>Production KPIs (totals, average daily oil) for a well over a period.</summary>
    [HttpGet("wells/{wellId:guid}/production")]
    public async Task<ActionResult<ProductionKpiDto>> GetProductionKpi(
        Guid wellId,
        [FromQuery] DateOnly periodStart,
        [FromQuery] DateOnly periodEnd,
        CancellationToken cancellationToken)
    {
        if (periodStart > periodEnd)
        {
            return BadRequest(new { message = "periodStart must be on or before periodEnd." });
        }

        var kpi = await _kpiService.GetProductionKpiAsync(wellId, periodStart, periodEnd, cancellationToken);
        return Ok(kpi);
    }

    /// <summary>
    /// Single aggregate payload for the Dashboard page: summary counts/totals plus the data for
    /// the field-wide production trend, artificial-lift breakdown, and top-decline-wells charts.
    /// Replaces what used to be a per-well KPI-fetch loop on the frontend.
    /// </summary>
    [HttpGet("dashboard")]
    public async Task<ActionResult<Application.Dtos.DashboardSummaryDto>> GetDashboardSummary(
        [FromQuery] DateOnly periodStart,
        [FromQuery] DateOnly periodEnd,
        CancellationToken cancellationToken)
    {
        if (periodStart > periodEnd)
        {
            return BadRequest(new { message = "periodStart must be on or before periodEnd." });
        }

        var summary = await _kpiService.GetDashboardSummaryAsync(periodStart, periodEnd, cancellationToken);
        return Ok(summary);
    }
}
