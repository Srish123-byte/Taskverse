#!/bin/bash
set -e

echo "===== BEFORE INSTALL ====="

systemctl stop taskverse-assessments.service || true

rm -rf /opt/taskverse/assessments

mkdir -p /opt/taskverse/assessments