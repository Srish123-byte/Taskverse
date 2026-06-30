#!/bin/bash
set -e

SERVICE_NAME="taskverse-auth"
APP_DIR="/opt/taskverse/auth"

echo "===== BEFORE INSTALL ====="

echo "Stopping ${SERVICE_NAME} service if running..."
systemctl stop ${SERVICE_NAME}.service || true

echo "Creating application directory..."
mkdir -p ${APP_DIR}

echo "Creating log directory..."
mkdir -p /var/log/taskverse

echo "Setting permissions..."
chown -R ec2-user:ec2-user /opt/taskverse

echo "BeforeInstall completed successfully."