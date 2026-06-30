#!/bin/bash
set -e

echo "===== BEFORE INSTALL ====="

systemctl stop taskverse-college.service || true

rm -rf /opt/taskverse/college

mkdir -p /opt/taskverse/college