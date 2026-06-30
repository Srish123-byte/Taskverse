#!/bin/bash
set -e

echo "===== AFTER INSTALL ====="

chmod -R 755 /opt/taskverse/codingengine

chmod 644 /etc/systemd/system/taskverse-codingengine.service

systemctl daemon-reload

systemctl enable taskverse-codingengine.service