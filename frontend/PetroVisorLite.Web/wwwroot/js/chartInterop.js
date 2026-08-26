// JS interop wrapper around Chart.js (loaded via CDN <script> tag in wwwroot/index.html —
// no npm/node build step, consistent with the frontend pivot decision). Blazor components
// call these functions via IJSRuntime.InvokeVoidAsync/InvokeAsync.

let productionChart = null;

export function renderProductionChart(canvasId, labels, oilData, gasData) {
    const ctx = document.getElementById(canvasId);
    if (!ctx) {
        return;
    }

    if (productionChart) {
        productionChart.destroy();
    }

    productionChart = new Chart(ctx, {
        type: 'line',
        data: {
            labels: labels,
            datasets: [
                { label: 'Oil (bbl)', data: oilData, borderColor: '#0d6efd', backgroundColor: 'rgba(13,110,253,0.1)', tension: 0.2, yAxisID: 'y' },
                { label: 'Gas (mcf)', data: gasData, borderColor: '#198754', backgroundColor: 'rgba(25,135,84,0.1)', tension: 0.2, yAxisID: 'y1' }
            ]
        },
        options: {
            responsive: true,
            interaction: { mode: 'index', intersect: false },
            scales: {
                y: { type: 'linear', position: 'left', title: { display: true, text: 'Oil (bbl)' } },
                y1: { type: 'linear', position: 'right', title: { display: true, text: 'Gas (mcf)' }, grid: { drawOnChartArea: false } }
            }
        }
    });
}

export function destroyProductionChart() {
    if (productionChart) {
        productionChart.destroy();
        productionChart = null;
    }
}

let fieldTrendChart = null;

export function renderFieldTrendChart(canvasId, labels, oilData, gasData, waterData) {
    const ctx = document.getElementById(canvasId);
    if (!ctx) {
        return;
    }

    if (fieldTrendChart) {
        fieldTrendChart.destroy();
    }

    fieldTrendChart = new Chart(ctx, {
        type: 'line',
        data: {
            labels: labels,
            datasets: [
                { label: 'Oil (bbl)', data: oilData, borderColor: '#0d6efd', backgroundColor: 'rgba(13,110,253,0.1)', tension: 0.2, fill: true },
                { label: 'Gas (mcf)', data: gasData, borderColor: '#198754', backgroundColor: 'rgba(25,135,84,0.1)', tension: 0.2, fill: true },
                { label: 'Water (bbl)', data: waterData, borderColor: '#6c757d', backgroundColor: 'rgba(108,117,125,0.1)', tension: 0.2, fill: true }
            ]
        },
        options: {
            responsive: true,
            interaction: { mode: 'index', intersect: false },
            scales: {
                x: { stacked: true },
                y: { stacked: true, title: { display: true, text: 'Volume' } }
            }
        }
    });
}

export function destroyFieldTrendChart() {
    if (fieldTrendChart) {
        fieldTrendChart.destroy();
        fieldTrendChart = null;
    }
}

let liftBreakdownChart = null;

export function renderLiftBreakdownChart(canvasId, labels, oilData) {
    const ctx = document.getElementById(canvasId);
    if (!ctx) {
        return;
    }

    if (liftBreakdownChart) {
        liftBreakdownChart.destroy();
    }

    liftBreakdownChart = new Chart(ctx, {
        type: 'pie',
        data: {
            labels: labels,
            datasets: [
                {
                    label: 'Total Oil (bbl)',
                    data: oilData,
                    backgroundColor: ['#0d6efd', '#198754', '#ffc107', '#dc3545', '#6f42c1', '#20c997']
                }
            ]
        },
        options: {
            responsive: true,
            plugins: { legend: { position: 'right' } }
        }
    });
}

export function destroyLiftBreakdownChart() {
    if (liftBreakdownChart) {
        liftBreakdownChart.destroy();
        liftBreakdownChart = null;
    }
}

let declineRankingChart = null;

export function renderDeclineRankingChart(canvasId, labels, declineData) {
    const ctx = document.getElementById(canvasId);
    if (!ctx) {
        return;
    }

    if (declineRankingChart) {
        declineRankingChart.destroy();
    }

    declineRankingChart = new Chart(ctx, {
        type: 'bar',
        data: {
            labels: labels,
            datasets: [
                { label: 'Daily Decline (%)', data: declineData, backgroundColor: '#dc3545' }
            ]
        },
        options: {
            responsive: true,
            indexAxis: 'y',
            scales: {
                x: { title: { display: true, text: 'Daily decline rate (%)' } }
            }
        }
    });
}

export function destroyDeclineRankingChart() {
    if (declineRankingChart) {
        declineRankingChart.destroy();
        declineRankingChart = null;
    }
}
