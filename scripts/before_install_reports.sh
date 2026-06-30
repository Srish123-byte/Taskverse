#!/bin/bash
set -e

echo "===== BEFORE INSTALL ====="

systemctl stop taskverse-reports.service || true

rm -rf /opt/taskverse/reports

mkdir -p /opt/taskverse/reports