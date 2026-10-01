#!/bin/bash
set -e

STAGE=${1:-"all"}
RESULTS_DIR="/app/TestResults"
mkdir -p "$RESULTS_DIR"

run_unit() {
    echo "=========================================="
    echo "1/3 Запуск юнит тестов"
    echo "=========================================="
    dotnet test Analyzer.Tests/Analyzer.Tests.csproj \
        --configuration Release \
        --no-build \
        --logger "trx;LogFileName=unit-results.trx" \
        -p:CollectCoverage=true \
        -p:CoverletOutputFormat=cobertura \
        -p:CoverletOutput="$RESULTS_DIR/unit-coverage.cobertura.xml"
}

run_integration() {
    echo "=========================================="
    echo "2/3 Запуск интеграционных тестов"
    echo "=========================================="
    dotnet test Analyzer.IntegrationTests/Analyzer.IntegrationTests.csproj \
        --configuration Release \
        --no-build \
        --filter "FullyQualifiedName!~E2E" \
        --logger "trx;LogFileName=integration-results.trx"
}

run_e2e() {
    echo "=========================================="
    echo "3/3 Запуск E2E тестов"
    echo "=========================================="
    dotnet test Analyzer.IntegrationTests/Analyzer.IntegrationTests.csproj \
        --configuration Release \
        --no-build \
        --filter "FullyQualifiedName~E2E" \
        --logger "trx;LogFileName=e2e-results.trx"
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
    all)
        # Требование 5 и 8: строгий порядок и остановка при падении
        run_unit
        run_integration
        run_e2e
        echo "=========================================="
        echo "Все стадии тестов успешно пройдены!"
        echo "=========================================="
        ;;
    *)
        echo "Неизвестная стадия: $STAGE. Доступно: unit, integration, e2e, all"
        exit 1
        ;;
esac
