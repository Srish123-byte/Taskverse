#!/bin/bash
set -e

SERVICE_NAME="taskverse-api"

echo "===== VALIDATE SERVICE ====="

echo "Checking if ${SERVICE_NAME} is running..."

systemctl is-active --quiet ${SERVICE_NAME}.service

echo "${SERVICE_NAME} is running."

sleep 20

HTTP_CODE=$(curl -s -o /dev/null -w "%{http_code}" http://localhost:5000/api/system || true)

if [ "$HTTP_CODE" = "200" ]; then
    echo "API validation successful."
    exit 0
fi

echo "API validation failed."

exit 1