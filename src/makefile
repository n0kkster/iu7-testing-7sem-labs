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

docker-test:
	docker compose -f docker-compose.testing.yml build
	docker compose -f docker-compose.testing.yml run --rm test-runner all

docker-test-unit:
	docker compose -f docker-compose.testing.yml run --rm test-runner unit

docker-test-integration:
	docker compose -f docker-compose.testing.yml run --rm test-runner integration

docker-test-e2e:
	docker compose -f docker-compose.testing.yml run --rm test-runner e2e

test-offline:
	unshare -r -n sh -c "ip link set lo up && dotnet test $(TEST_PROJECT) --no-restore"

capture-traffic:
	@echo "=== Запуск захвата трафика ==="
	sudo tcpdump -i lo -nn -s 0 -w traffic_dump.pcap port 1555 > /dev/null 2>&1 & TCPDUMP_PID=$$!; \
	sleep 1; \
	./simulate-e2e.sh; \
	sleep 1; \
	sudo kill $$TCPDUMP_PID; \
	echo "=== Захват завершен. Файл сохранен в traffic_dump.pcap ==="

clean:
	-rm -rf $(RESULTS_DIR)
	-rm -rf $(REPORT_DIR)
	-find . -type d -name "TestResults" -exec rm -rf {} + 2>/dev/null || true