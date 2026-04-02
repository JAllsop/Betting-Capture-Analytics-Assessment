# assets/scripts/docker_run.ps1
Write-Host "Starting Docker Infrastructure (SQL, Redis, Rabbit)..." -ForegroundColor Blue
docker compose up -d --build

Write-Host "Waiting for infrastructure..." -ForegroundColor Gray
Start-Sleep -Seconds 5

# FORCE the API to use the hardcoded ports expected by the Tester
# This maps HTTPS to 7120 and HTTP to 5021
$env:ASPNETCORE_URLS="https://localhost:7120;http://localhost:5021"
$env:ASPNETCORE_ENVIRONMENT="Development"

Write-Host "Launching Consumer..." -ForegroundColor Magenta
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd ./src/OT.Assessment.Consumer; dotnet run -c Release"

Write-Host "Launching App API..." -ForegroundColor Green
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd ./src/OT.Assessment.App; dotnet run -c Release --launch-profile https"

Write-Host "Systems launching. Access Swagger at http://localhost:5021/swagger" -ForegroundColor Green