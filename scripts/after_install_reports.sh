#!/bin/bash
set -e

echo "===== AFTER INSTALL ====="

chmod -R 755 /opt/taskverse/reports

chmod 644 /etc/systemd/system/taskverse-reports.service

systemctl daemon-reload

systemctl enable taskverse-reports.service