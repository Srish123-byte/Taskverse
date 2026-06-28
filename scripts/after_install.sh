#!/bin/bash
set -e

echo "===== AFTER INSTALL ====="

echo "Copying Angular files..."

cp -R /tmp/taskverse-web/* /var/www/

echo "Fetching config.json from Secrets Manager..."

aws secretsmanager get-secret-value \
    --secret-id taskverse-web-config \
    --query SecretString \
    --output text > /var/www/assets/config.json

chmod 644 /var/www/assets/config.json

echo "config.json deployed successfully."