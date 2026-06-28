Write-Host "Starting all Taskverse microservices..."

# Start each service in background
Start-Process -FilePath "dotnet" -ArgumentList "run --project Taskverse.Auth.Service/Taskverse.API.Auth.Service.csproj" -NoNewWindow
Start-Process -FilePath "dotnet" -ArgumentList "run --project Taskverse.API.Users.Service/Taskverse.API.Users.Service.csproj" -NoNewWindow
Start-Process -FilePath "dotnet" -ArgumentList "run --project Taskverse.API.CodingEngine.Service/Taskverse.API.CodingEngine.Service.csproj" -NoNewWindow
Start-Process -FilePath "dotnet" -ArgumentList "run --project Taskverse.API.Proctor.Service/Taskverse.API.Proctor.Service.csproj" -NoNewWindow
Start-Process -FilePath "dotnet" -ArgumentList "run --project Taskverse.API.Assessments.Service/Taskverse.API.Assessments.Service.csproj" -NoNewWindow
Start-Process -FilePath "dotnet" -ArgumentList "run --project Taskverse.API.Reports.Service/Taskverse.API.Reports.Service.csproj" -NoNewWindow
Start-Process -FilePath "dotnet" -ArgumentList "run --project Taskverse.College.Service/Taskverse.API.College.Service.csproj" -NoNewWindow

# Wait 5 seconds before starting gateway
Start-Sleep -Seconds 5

Write-Host "Starting Gateway..."
Start-Process -FilePath "dotnet" -ArgumentList "run --project Taskverse.API/Taskverse.API.csproj --urls http://localhost:5200" -NoNewWindow

Write-Host "All microservices and Gateway started successfully!"

# Loop forever to keep parent powershell process and child processes alive
while ($true) {
    Start-Sleep -Seconds 10
}
