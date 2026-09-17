FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["Analyzer.Api/Analyzer.Api.csproj", "Analyzer.Api/"]
COPY ["Analyzer.Application/Analyzer.Application.csproj", "Analyzer.Application/"]
COPY ["Analyzer.Infrastructure/Analyzer.Infrastructure.csproj", "Analyzer.Infrastructure/"]
COPY ["Analyzer.Shared/Analyzer.Shared.csproj", "Analyzer.Shared/"]

RUN dotnet restore "Analyzer.Api/Analyzer.Api.csproj"

COPY . .

WORKDIR "/src/Analyzer.Api"
RUN dotnet publish "Analyzer.Api.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled-extra AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Analyzer.Api.dll"]