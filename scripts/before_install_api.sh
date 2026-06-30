#!/bin/bash
set -e

SERVICE_NAME="taskverse-api"
APP_DIR="/opt/taskverse/api"

echo "===== BEFORE INSTALL ====="

systemctl stop ${SERVICE_NAME}.service || true

mkdir -p ${APP_DIR}

mkdir -p /var/log/taskverse

chown -R ec2-user:ec2-user /opt/taskverse

echo "BeforeInstall completed successfully."