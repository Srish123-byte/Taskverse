#!/bin/bash
set -e

echo "===== APPLICATION START ====="

systemctl restart taskverse-college.service

sleep 10

systemctl status taskverse-college.service --no-pager