TEST_PROJECT = Analyzer.Tests/Analyzer.Tests.csproj
REPORT_DIR = coveragereport
RESULTS_DIR = TestResults
SEED ?= $(shell od -An -N4 -tu4 /dev/urandom | tr -d ' ')

.PHONY: test test-random report clean test-offline help

help:
	@echo "Доступные команды:"
	@echo "  make test         - Запуск тестов с выводом покрытия в консоль"
	@echo "  make test-random  - Запуск тестов в случайном порядке (опционально: SEED=12345)"
	@echo "  make report       - Запуск тестов, генерация HTML-отчета и открытие в браузере"
	@echo "  make test-offline - Запуск тестов без интернета"
	@echo "  make clean        - Очистка сгенерированных отчетов, логов и файлов покрытия"

test:
	dotnet test $(TEST_PROJECT) \
		-p:CollectCoverage=true \
		-p:CoverletOutputFormat=cobertura \
		-p:CoverletOutput="../$(RESULTS_DIR)/"

test-random:
	@echo "=== Запуск со случайным порядком (Seed: $(SEED)) ==="
	dotnet test $(TEST_PROJECT) \
		-p:CollectCoverage=true \
		-p:CoverletOutputFormat=cobertura \
		-p:CoverletOutput="../$(RESULTS_DIR)/" \
		-- xUnit.Seed=$(SEED)

report: clean test
	reportgenerator \
		-reports:"$(RESULTS_DIR)/coverage.cobertura.xml" \
		-targetdir:"$(REPORT_DIR)" \
		-reporttypes:HtmlInline_AzurePipelines
	@xdg-open $(REPORT_DIR)/index.html 2>/dev/null || echo "Отчет сгенерирован в $(REPORT_DIR)/index.html"

test-offline:
	unshare -r -n sh -c "ip link set lo up && dotnet test $(TEST_PROJECT) --no-restore"

clean:
	-rm -rf $(RESULTS_DIR)
	-rm -rf $(REPORT_DIR)
	-find . -type d -name "TestResults" -exec rm -rf {} + 2>/dev/null || true