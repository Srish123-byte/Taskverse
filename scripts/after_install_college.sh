#!/bin/bash
set -e

echo "===== AFTER INSTALL ====="

chmod -R 755 /opt/taskverse/college

chmod 644 /etc/systemd/system/taskverse-college.service

systemctl daemon-reload

systemctl enable taskverse-college.service