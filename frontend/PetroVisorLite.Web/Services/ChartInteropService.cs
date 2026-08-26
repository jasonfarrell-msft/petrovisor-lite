using Microsoft.JSInterop;

namespace PetroVisorLite.Web.Services;

/// <summary>
/// Thin C# wrapper over the Chart.js JS-interop module (<c>wwwroot/js/chartInterop.js</c>),
/// which itself wraps the Chart.js library loaded via CDN <c>&lt;script&gt;</c> tag in
/// <c>wwwroot/index.html</c> (no npm/node build step).
/// </summary>
public class ChartInteropService : IAsyncDisposable
{
    private readonly Lazy<Task<IJSObjectReference>> _moduleTask;

    public ChartInteropService(IJSRuntime jsRuntime)
    {
        _moduleTask = new Lazy<Task<IJSObjectReference>>(() =>
            jsRuntime.InvokeAsync<IJSObjectReference>("import", "./js/chartInterop.js").AsTask());
    }

    public async Task RenderProductionChartAsync(string canvasId, IReadOnlyList<string> labels, IReadOnlyList<double> oilData, IReadOnlyList<double> gasData)
    {
        var module = await _moduleTask.Value;
        await module.InvokeVoidAsync("renderProductionChart", canvasId, labels, oilData, gasData);
    }

    public async Task RenderFieldTrendChartAsync(string canvasId, IReadOnlyList<string> labels, IReadOnlyList<double> oilData, IReadOnlyList<double> gasData, IReadOnlyList<double> waterData)
    {
        var module = await _moduleTask.Value;
        await module.InvokeVoidAsync("renderFieldTrendChart", canvasId, labels, oilData, gasData, waterData);
    }

    public async Task RenderLiftBreakdownChartAsync(string canvasId, IReadOnlyList<string> labels, IReadOnlyList<double> oilData)
    {
        var module = await _moduleTask.Value;
        await module.InvokeVoidAsync("renderLiftBreakdownChart", canvasId, labels, oilData);
    }

    public async Task RenderDeclineRankingChartAsync(string canvasId, IReadOnlyList<string> labels, IReadOnlyList<double> declineData)
    {
        var module = await _moduleTask.Value;
        await module.InvokeVoidAsync("renderDeclineRankingChart", canvasId, labels, declineData);
    }

    public async ValueTask DisposeAsync()
    {
        if (_moduleTask.IsValueCreated)
        {
            var module = await _moduleTask.Value;
            await module.InvokeVoidAsync("destroyProductionChart");
            await module.InvokeVoidAsync("destroyFieldTrendChart");
            await module.InvokeVoidAsync("destroyLiftBreakdownChart");
            await module.InvokeVoidAsync("destroyDeclineRankingChart");
            await module.DisposeAsync();
        }
    }
}
