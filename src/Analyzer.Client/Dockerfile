FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["Analyzer.Client/Analyzer.Client.csproj", "Analyzer.Client/"]
COPY ["Analyzer.Shared/Analyzer.Shared.csproj", "Analyzer.Shared/"]

RUN dotnet restore "Analyzer.Client/Analyzer.Client.csproj"

COPY . .

WORKDIR "/src/Analyzer.Client"
RUN dotnet publish "Analyzer.Client.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Analyzer.Client.dll"]