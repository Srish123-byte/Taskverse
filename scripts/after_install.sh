#!/bin/bash
set -euo pipefail

. "$(dirname "$0")/deploy_helpers.sh"

echo "===== AFTER INSTALL ====="

echo "Copying Angular files..."

cp -R /tmp/taskverse-web/browser/* /var/www/

echo "Fetching config.json from Secrets Manager..."

ensure_aws_cli

fetch_secret_to_file "taskverse-web-config" "ap-south-1" "/var/www/assets/config.json"

require_file_with_content "/var/www/assets/config.json" "assets/config.json"

chmod 644 /var/www/assets/config.json

echo "config.json deployed successfully."
