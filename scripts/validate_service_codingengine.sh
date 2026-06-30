#!/bin/bash
set -e

SERVICE_NAME="taskverse-codingengine"

echo "===== VALIDATE SERVICE ====="

echo "Checking if ${SERVICE_NAME} is running..."

systemctl is-active --quiet ${SERVICE_NAME}.service

echo "${SERVICE_NAME} is running."

sleep 5

HTTP_CODE=$(curl -s -o /dev/null -w "%{http_code}" http://localhost:5007/api/system || true)

if [ "$HTTP_CODE" = "200" ]; then
    echo "CodingEngine service validation successful."
    exit 0
fi

echo "CodingEngine service validation failed."

exit 1