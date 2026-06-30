#!/bin/bash
set -e

APP_DIR="/opt/taskverse/auth"
SECRET_NAME="taskverse-auth-config"
AWS_REGION="ap-south-1"

echo "===== AFTER INSTALL ====="

if ! command -v aws >/dev/null 2>&1; then
    dnf install -y awscli
fi

echo "Fetching appsettings.json from Secrets Manager..."

aws secretsmanager get-secret-value \
    --secret-id ${SECRET_NAME} \
    --region ${AWS_REGION} \
    --query SecretString \
    --output text > ${APP_DIR}/appsettings.json

echo "Setting file permissions..."

chown -R ec2-user:ec2-user ${APP_DIR}

find ${APP_DIR} -type d -exec chmod 755 {} \;

find ${APP_DIR} -type f -exec chmod 644 {} \;

echo "AfterInstall completed successfully."