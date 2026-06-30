#!/bin/bash
set -e

SERVICE_NAME="taskverse-users"

echo "===== VALIDATE SERVICE ====="

echo "Checking if ${SERVICE_NAME} is running..."

systemctl is-active --quiet ${SERVICE_NAME}.service

echo "${SERVICE_NAME} is running."

sleep 20

HTTP_CODE=$(curl -s -o /dev/null -w "%{http_code}" http://localhost:5003/health || true)

echo "HTTP_CODE=$HTTP_CODE"

if [ "$HTTP_CODE" = "200" ]; then
    echo "Users service validation successful."
    exit 0
fi

echo "Users service validation failed."

exit 1