#!/bin/bash
set -euo pipefail

. "$(dirname "$0")/deploy_helpers.sh"

echo "===== BEFORE INSTALL ====="

systemctl stop taskverse-users.service || true

reset_app_dir "/opt/taskverse/users"

mkdir -p /var/log/taskverse

chown -R ec2-user:ec2-user /opt/taskverse
