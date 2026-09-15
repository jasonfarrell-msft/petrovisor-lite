using Microsoft.Extensions.DependencyInjection;
using PetroVisorLite.Application.Analytics;
using PetroVisorLite.Application.Assistant;
using PetroVisorLite.Application.Interfaces;

namespace PetroVisorLite.Application.DependencyInjection;

/// <summary>
/// Registers Application-layer services. Han: call <c>builder.Services.AddApplicationServices()</c>
/// from <c>Program.cs</c> alongside the Infrastructure repository registrations — <see cref="KpiService"/>
/// depends on <see cref="IProductionRecordRepository"/>, which must also be registered.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IProductionKpiCalculator, ProductionKpiCalculator>();
        services.AddScoped<IDeclineRateCalculator, DeclineRateCalculator>();
        services.AddScoped<IProductionLossDetector, ProductionLossDetector>();
        services.AddScoped<IArtificialLiftMonitor, ArtificialLiftMonitor>();
        services.AddScoped<IFacilityComparisonService, FacilityComparisonService>();
        services.AddScoped<IKpiService, KpiService>();
        services.AddScoped<IQueryOrchestrator, QueryOrchestrator>();

        return services;
    }
}
