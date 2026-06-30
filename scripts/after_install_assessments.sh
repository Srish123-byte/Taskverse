#!/bin/bash
set -e

echo "===== AFTER INSTALL ====="

chmod -R 755 /opt/taskverse/assessments

chmod 644 /etc/systemd/system/taskverse-assessments.service

systemctl daemon-reload

systemctl enable taskverse-assessments.service