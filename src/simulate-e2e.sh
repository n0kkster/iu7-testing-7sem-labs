#!/bin/bash
set -e

API_URL="${API_URL:-http://localhost:1555}"
SUFFIX=$(head /dev/urandom | tr -dc a-z0-9 | head -c 6)

ADMIN_USER="admin"
ADMIN_PASS="admin123"

ARCH_USER="architect_${SUFFIX}"
ARCH_EMAIL="arch_${SUFFIX}@corp.local"
ARCH_PASS="P@ssword1234!"

echo "=========================================================="
echo "Запуск симуляции E2E-сценария на $API_URL"
echo "=========================================================="

# 1. Логин администратора
echo "1. Логин администратора..."
ADMIN_TOKEN=$(curl -s -X POST "$API_URL/api/v2/users/login" \
  -H "Content-Type: application/json" \
  -d "{\"username\":\"$ADMIN_USER\",\"password\":\"$ADMIN_PASS\"}" | jq -r '.token')

if [ -z "$ADMIN_TOKEN" ] || [ "$ADMIN_TOKEN" = "null" ]; then
    echo "Ошибка входа администратора"
    exit 1
fi

# 2. Создание команды
echo "2. Создание команды..."
TEAM_ID=$(curl -s -X POST "$API_URL/api/v2/teams" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -d "{\"name\":\"Traffic_Team_$SUFFIX\",\"description\":\"Traffic Capture Test Team\"}" | jq -r '.id')

# 3. Выпуск инвайта
echo "3. Выпуск инвайта для архитектора..."
INVITE_CODE=$(curl -s -X POST "$API_URL/api/v2/teams/$TEAM_ID/invites" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -d "{\"email\":\"$ARCH_EMAIL\",\"validForDays\":7,\"role\":\"Architect\"}" | jq -r '.code')

# 4. Регистрация архитектора
echo "4. Регистрация архитектора..."
curl -s -X POST "$API_URL/api/v2/users/register" \
  -H "Content-Type: application/json" \
  -d "{\"username\":\"$ARCH_USER\",\"email\":\"$ARCH_EMAIL\",\"password\":\"$ARCH_PASS\",\"inviteCode\":\"$INVITE_CODE\"}" > /dev/null

# 5. Вход архитектора
echo "5. Логин архитектора..."
ARCH_TOKEN=$(curl -s -X POST "$API_URL/api/v2/users/login" \
  -H "Content-Type: application/json" \
  -d "{\"username\":\"$ARCH_USER\",\"password\":\"$ARCH_PASS\"}" | jq -r '.token')

# 6. Создание системы
echo "6. Создание IT-системы..."
SYSTEM_ID=$(curl -s -X POST "$API_URL/api/v2/systems" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $ARCH_TOKEN" \
  -d "{\"name\":\"Traffic_System_$SUFFIX\",\"description\":\"System for traffic dump\",\"teamId\":\"$TEAM_ID\"}" | jq -r '.id')

# 7. Создание компонентов
echo "7. Создание компонентов..."
COMP1_ID=$(curl -s -X POST "$API_URL/api/v2/components" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $ARCH_TOKEN" \
  -d "{\"systemId\":\"$SYSTEM_ID\",\"type\":\"Microservice\",\"name\":\"Billing-API\",\"description\":\"Handles payments\"}" | jq -r '.id')

COMP2_ID=$(curl -s -X POST "$API_URL/api/v2/components" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $ARCH_TOKEN" \
  -d "{\"systemId\":\"$SYSTEM_ID\",\"type\":\"Database\",\"name\":\"Billing-DB\",\"description\":\"Postgres database\"}" | jq -r '.id')

# 8. Создание зависимости (Link)
echo "8. Связывание компонентов..."
curl -s -X POST "$API_URL/api/v2/links" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $ARCH_TOKEN" \
  -d "{\"sourceId\":\"$COMP1_ID\",\"targetId\":\"$COMP2_ID\",\"severity\":\"High\",\"protocol\":\"TCP\"}" > /dev/null

# 9. Запуск анализа отказоустойчивости
echo "9. Вызов анализа каскадного сбоя..."
curl -s -X POST "$API_URL/api/v2/analysis" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $ARCH_TOKEN" \
  -d "{\"type\":\"CascadingFailure\",\"componentId\":\"$COMP2_ID\"}" > /dev/null

# 10. Экспорт системы
echo "10. Экспорт бэкапа топологии..."
curl -s -X GET "$API_URL/api/v2/systems/$SYSTEM_ID/export" \
  -H "Authorization: Bearer $ARCH_TOKEN" > /dev/null

# 11. Удаление системы
echo "11. Удаление системы..."
curl -s -X DELETE "$API_URL/api/v2/systems/$SYSTEM_ID" \
  -H "Authorization: Bearer $ARCH_TOKEN" > /dev/null

echo "=========================================================="
echo "Все запросы сценария успешно отправлены!"
echo "=========================================================="