#!/bin/bash
set -e

echo "===== BEFORE INSTALL ====="

systemctl stop taskverse-proctor.service || true

rm -rf /opt/taskverse/proctor

mkdir -p /opt/taskverse/proctor