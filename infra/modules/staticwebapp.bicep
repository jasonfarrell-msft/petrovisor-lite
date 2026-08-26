// Azure Static Web App hosting the Blazor WebAssembly frontend.
// API version: Microsoft.Web/staticSites@2023-12-01 — latest GA known at
// authoring time (2026-08-25). REVERIFY before real deployment:
//   az provider show --namespace Microsoft.Web \
//     --query "resourceTypes[?resourceType=='staticSites'].apiVersions" -o tsv
//
// NOTE on regions: Azure Static Web Apps only deploys to a specific subset of
// regions (independent of the "location" of the underlying static content
// distribution — Front Door/CDN edges are global regardless). Central US is
// one of the supported/GA regions for Microsoft.Web/staticSites, so this
// module can share the same `centralus` location as the rest of the resource
// group. If that ever changes, fall back to West US 2 or East US 2 (also GA)
// and note the deviation in the decision log / README.
param location string
param staticWebAppName string

@description('SKU for the Static Web App. "Standard" is required for custom auth/backends and private endpoint support; "Free" is a valid cost-saving alternative for a pure demo/lite deployment with no custom auth needs.')
@allowed([
  'Free'
  'Standard'
])
param skuName string = 'Standard'

@description('Build output location (relative to appLocation) containing the static Blazor WASM publish output, e.g. "wwwroot" when appLocation points at the publish folder.')
param appArtifactLocation string = 'wwwroot'

@description('App source location as tracked by Static Web Apps config (informational when not using the GitHub/DevOps CI integration — this scaffold assumes deployment via `swa deploy` / SWA CLI / a pipeline artifact, not a linked GitHub repo).')
param appLocation string = '/'

@description('Backend Container App FQDN (no scheme) to expose as an app setting for the Blazor WASM client to read at runtime, e.g. via a config endpoint or environment-injected value. NOTE: Static Web Apps app settings are only readable from linked Azure Functions API routes, not directly by static client-side WASM files — see README for how this is actually wired at deploy time (SWA CLI `swa deploy --app-settings` or GitHub Actions env, or a runtime config.json fetched by the app).')
param backendApiFqdn string

resource staticWebApp 'Microsoft.Web/staticSites@2023-12-01' = {
  name: staticWebAppName
  location: location
  sku: {
    name: skuName
    tier: skuName
  }
  properties: {
    // No repositoryUrl/branch: this scaffold assumes deployment via the SWA
    // CLI or a pipeline (not GitHub-integrated CI), consistent with the
    // container-image-based backend deployment model used elsewhere here.
    buildProperties: {
      appLocation: appLocation
      outputLocation: appArtifactLocation
    }
    stagingEnvironmentPolicy: 'Enabled'
    allowConfigFileUpdates: true
  }
}

// App settings on a Static Web App are exposed to any linked Functions API,
// not to the static client files directly. We still provision the setting so
// it's available for a future managed Functions API and for documentation/
// deploy-time reference; the Blazor WASM client itself should read this via
// a fetched runtime config (e.g. `wwwroot/appsettings.json` overridden at
// deploy time, or a small `/api/config` Functions route) — see README.
resource appSettings 'Microsoft.Web/staticSites/config@2023-12-01' = {
  parent: staticWebApp
  name: 'appsettings'
  properties: {
    BACKEND_API_BASE_URL: 'https://${backendApiFqdn}'
  }
}

output staticWebAppId string = staticWebApp.id
output staticWebAppName string = staticWebApp.name
output defaultHostname string = staticWebApp.properties.defaultHostname
