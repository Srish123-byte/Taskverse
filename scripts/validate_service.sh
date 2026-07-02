#!/bin/bash
set -euo pipefail

. "$(dirname "$0")/deploy_helpers.sh"

echo "===== VALIDATING DEPLOYMENT ====="

systemctl is-active --quiet nginx

if [ $? -ne 0 ]; then
    echo "Nginx is not running."
    exit 1
fi

require_file_with_content "/var/www/assets/config.json" "assets/config.json"

echo "Deployment validated successfully."
