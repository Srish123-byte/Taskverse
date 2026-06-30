#!/bin/bash
set -e

echo "===== BEFORE INSTALL ====="

systemctl stop taskverse-codingengine.service || true

rm -rf /opt/taskverse/codingengine

mkdir -p /opt/taskverse/codingengine