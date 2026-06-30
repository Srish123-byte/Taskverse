#!/bin/bash
set -e

echo "===== APPLICATION START ====="

systemctl restart taskverse-users.service

sleep 10

systemctl status taskverse-users.service --no-pager