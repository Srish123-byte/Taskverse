#!/bin/bash
set -e

echo "===== APPLICATION START ====="

systemctl restart taskverse-reports.service

sleep 10

systemctl status taskverse-reports.service --no-pager