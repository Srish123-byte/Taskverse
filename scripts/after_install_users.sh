#!/bin/bash
set -e

echo "===== AFTER INSTALL ====="

chmod -R 755 /opt/taskverse/users

chmod 644 /etc/systemd/system/taskverse-users.service

systemctl daemon-reload

systemctl enable taskverse-users.service