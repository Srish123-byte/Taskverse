#!/bin/bash
set -euo pipefail

SERVICE_NAME="taskverse-api"
APP_DIR="/opt/taskverse/api"

. "$(dirname "$0")/deploy_helpers.sh"

echo "===== BEFORE INSTALL ====="

systemctl stop ${SERVICE_NAME}.service || true

reset_app_dir "${APP_DIR}"

mkdir -p /var/log/taskverse

chown -R ec2-user:ec2-user /opt/taskverse

echo "BeforeInstall completed successfully."
