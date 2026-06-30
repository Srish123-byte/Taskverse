#!/bin/bash
set -e

echo "===== BEFORE INSTALL ====="

systemctl stop taskverse-users.service || true

rm -rf /opt/taskverse/users

mkdir -p /opt/taskverse/users