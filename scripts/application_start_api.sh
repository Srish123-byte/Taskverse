#!/bin/bash
set -e

SERVICE_NAME="taskverse-api"

echo "===== APPLICATION START ====="

systemctl daemon-reload

systemctl enable ${SERVICE_NAME}.service

systemctl restart ${SERVICE_NAME}.service

sleep 10

systemctl status ${SERVICE_NAME}.service --no-pager

echo "ApplicationStart completed successfully."