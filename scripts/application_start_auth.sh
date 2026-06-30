#!/bin/bash
set -e

SERVICE_NAME="taskverse-auth"

echo "===== APPLICATION START ====="

echo "Reloading systemd..."
systemctl daemon-reload

echo "Enabling ${SERVICE_NAME} service..."
systemctl enable ${SERVICE_NAME}.service

echo "Starting ${SERVICE_NAME} service..."
systemctl restart ${SERVICE_NAME}.service

echo "Waiting for service to start..."
sleep 10

echo "Checking service status..."
systemctl status ${SERVICE_NAME}.service --no-pager

echo "ApplicationStart completed successfully."