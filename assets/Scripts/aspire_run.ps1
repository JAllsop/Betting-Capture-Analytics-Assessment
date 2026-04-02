# assets/scripts/aspire_run.ps1
Write-Host "Starting .NET Aspire Orchestration (Release Mode)..." -ForegroundColor Cyan

# Ensure we are in the root and the project exists
$appHostPath = "./src/OT.Assessment.AppHost/OT.Assessment.AppHost.csproj"

if (Test-Path $appHostPath) {
    # We force DOTNET_ENVIRONMENT to Development so that User Secrets 
    # and local infrastructure settings are loaded while code is optimized.
    $env:DOTNET_ENVIRONMENT="Development"
    $env:ASPNETCORE_ENVIRONMENT="Development"
    
    dotnet run --project $appHostPath -c Release
} else {
    Write-Error "Could not find AppHost project at $appHostPath. Please run from the repo root."
}