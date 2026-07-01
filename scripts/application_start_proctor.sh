#!/bin/bash
set -e

echo "===== APPLICATION START ====="

systemctl restart taskverse-proctor.service

sleep 10

systemctl status taskverse-proctor.service --no-pager