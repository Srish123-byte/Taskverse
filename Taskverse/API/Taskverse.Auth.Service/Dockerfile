FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj files
COPY ["Taskverse.API.Auth.Service/Taskverse.API.Auth.Service.csproj", "Taskverse.API.Auth.Service/"]

# Restore packages
RUN dotnet restore "Taskverse.API.Auth.Service/Taskverse.API.Auth.Service.csproj"

# Copy source code
COPY . .

# Build
WORKDIR "/src/Taskverse.API.Auth.Service"
RUN dotnet build "Taskverse.API.Auth.Service.csproj" -c Release -o /app/build

# Publish
FROM build AS publish
RUN dotnet publish "Taskverse.API.Auth.Service.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

EXPOSE 5001
ENV ASPNETCORE_URLS=http://+:5001
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "Taskverse.API.Auth.Service.dll"]
