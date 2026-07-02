#!/bin/bash
set -euo pipefail

SERVICE_NAME="taskverse-assessments"
APP_DIR="/opt/taskverse/assessments"

. "$(dirname "$0")/deploy_helpers.sh"

echo "===== VALIDATE SERVICE ====="

echo "Checking if ${SERVICE_NAME} is running..."

systemctl is-active --quiet ${SERVICE_NAME}.service

echo "${SERVICE_NAME} is running."

require_file_with_content "${APP_DIR}/appsettings.json" "appsettings.json"

sleep 20

HTTP_CODE=$(curl -s -o /dev/null -w "%{http_code}" http://localhost:5004/health || true)

echo "HTTP_CODE=$HTTP_CODE"

if [ "$HTTP_CODE" = "200" ]; then
    echo "Assessments service validation successful."
    exit 0
fi

echo "Assessments service validation failed."

exit 1
