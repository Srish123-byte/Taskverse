FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj files
COPY ["Taskverse.Auth.Service/Taskverse.Auth.Service.csproj", "Taskverse.Auth.Service/"]

# Restore packages
RUN dotnet restore "Taskverse.Auth.Service/Taskverse.Auth.Service.csproj"

# Copy source code
COPY . .

# Build
WORKDIR "/src/Taskverse.Auth.Service"
RUN dotnet build "Taskverse.Auth.Service.csproj" -c Release -o /app/build

# Publish
FROM build AS publish
RUN dotnet publish "Taskverse.Auth.Service.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

EXPOSE 5001
ENV ASPNETCORE_URLS=http://+:5001
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "Taskverse.Auth.Service.dll"]
