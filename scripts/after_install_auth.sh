#!/bin/bash
set -euo pipefail

APP_DIR="/opt/taskverse/auth"
SECRET_NAME="taskverse-auth-config"
AWS_REGION="ap-south-1"

. "$(dirname "$0")/deploy_helpers.sh"

echo "===== AFTER INSTALL ====="

ensure_aws_cli

echo "Fetching appsettings.json from Secrets Manager..."

fetch_secret_to_file "${SECRET_NAME}" "${AWS_REGION}" "${APP_DIR}/appsettings.json"

require_file_with_content "${APP_DIR}/appsettings.json" "appsettings.json"

echo "Setting file permissions..."

chown -R ec2-user:ec2-user ${APP_DIR}

find ${APP_DIR} -type d -exec chmod 755 {} \;

find ${APP_DIR} -type f -exec chmod 644 {} \;

echo "AfterInstall completed successfully."
