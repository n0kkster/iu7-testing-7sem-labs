FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["Analyzer.Client/Analyzer.Client.csproj", "Analyzer.Client/"]
COPY ["Analyzer.Shared/Analyzer.Shared.csproj", "Analyzer.Shared/"]
COPY ["Analyzer.Domain/Analyzer.Domain.csproj", "Analyzer.Domain/"]
COPY ["Analyzer.Api/Analyzer.Api.csproj", "Analyzer.Api/"]
COPY ["Analyzer.Application/Analyzer.Application.csproj", "Analyzer.Application/"]
COPY ["Analyzer.Infrastructure/Analyzer.Infrastructure.csproj", "Analyzer.Infrastructure/"]

RUN dotnet restore "Analyzer.Client/Analyzer.Client.csproj"

COPY . .

WORKDIR "/src/Analyzer.Client"
RUN dotnet publish "Analyzer.Client.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Analyzer.Client.dll"]