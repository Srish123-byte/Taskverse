#!/bin/bash
set -e

echo "===== AFTER INSTALL ====="

chmod -R 755 /opt/taskverse/proctor

chmod 644 /etc/systemd/system/taskverse-proctor.service

systemctl daemon-reload

systemctl enable taskverse-proctor.service