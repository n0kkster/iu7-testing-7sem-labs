#!/bin/bash
set -e

STAGE=${1:-"all"}
RESULTS_DIR="/app/TestResults"
mkdir -p "$RESULTS_DIR"

run_unit() {
    echo "=========================================="
    echo "1/4 Запуск юнит тестов"
    echo "=========================================="
    dotnet test Analyzer.Tests/Analyzer.Tests.csproj \
        --configuration Release \
        --no-build \
        --logger "trx;LogFileName=/app/TestResults/unit-results.trx" \
        -p:CollectCoverage=true \
        -p:CoverletOutputFormat=cobertura \
        -p:CoverletOutput="$RESULTS_DIR/unit-coverage.cobertura.xml"
}

run_integration() {
    echo "=========================================="
    echo "2/4 Запуск интеграционных тестов"
    echo "=========================================="
    dotnet test Analyzer.IntegrationTests/Analyzer.IntegrationTests.csproj \
        --configuration Release \
        --no-build \
        --filter "FullyQualifiedName!~E2E" \
        --logger "trx;LogFileName=/app/TestResults/integration-results.trx"
}

run_e2e() {
    echo "=========================================="
    echo "3/4 Запуск E2E тестов"
    echo "=========================================="
    dotnet test Analyzer.IntegrationTests/Analyzer.IntegrationTests.csproj \
        --configuration Release \
        --no-build \
        --filter "FullyQualifiedName~E2E" \
        --logger "trx;LogFileName=/app/TestResults/e2e-results.trx"
}

run_bdd() {
    echo "=========================================="
    echo "4/4 Запуск BDD-тестов"
    echo "=========================================="
    dotnet test Analyzer.BddTests/Analyzer.BddTests.csproj \
        --configuration Release \
        --no-build \
        --logger "trx;LogFileName=/app/TestResults/bdd-results.trx"
}

case "$STAGE" in
    unit)
        run_unit
        ;;
    integration)
        run_integration
        ;;
    e2e)
        run_e2e
        ;;
    bdd)
        run_bdd
        ;;
    all)
        run_unit
        run_integration
        run_e2e
        run_bdd
        echo "=========================================="
        echo "✅ Все стадии тестов успешно пройдены!"
        echo "=========================================="
        ;;
    *)
        echo "Неизвестная стадия: $STAGE. Доступно: unit, integration, e2e, bdd, all"
        exit 1
        ;;
esac
