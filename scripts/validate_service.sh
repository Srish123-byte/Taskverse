#!/bin/bash
set -e

echo "===== VALIDATING DEPLOYMENT ====="

systemctl is-active --quiet nginx

if [ $? -ne 0 ]; then
    echo "Nginx is not running."
    exit 1
fi

if [ ! -f /var/www/assets/config.json ]; then
    echo "config.json not found."
    exit 1
fi

echo "Deployment validated successfully."