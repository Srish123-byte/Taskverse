#!/bin/bash
set -e

echo "===== APPLICATION START ====="

systemctl restart taskverse-assessments.service

sleep 10

systemctl status taskverse-assessments.service --no-pager