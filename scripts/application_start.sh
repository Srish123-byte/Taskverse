#!/bin/bash
set -e

echo "===== STARTING NGINX ====="

systemctl enable nginx

systemctl restart nginx

echo "Nginx restarted successfully."