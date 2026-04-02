# assets/scripts/tester_run.ps1
Write-Host "Preparing Load Test (BogusGenerator)..." -ForegroundColor Yellow
Write-Host "Ensure Aspire Dashboard or Docker logs show 'Healthy' before continuing." -ForegroundColor White

$confirmation = Read-Host "Ready to start the load test? (y/n)"
if ($confirmation -eq 'y') {
    Write-Host "Running Tester..." -ForegroundColor Cyan
    dotnet run --project ./test/OT.Assessment.Tester/OT.Assessment.Tester.csproj
} else {
    Write-Host "Execution cancelled." -ForegroundColor Red
}