#!/bin/bash
set -e

echo "===== APPLICATION START ====="

systemctl restart taskverse-codingengine.service

sleep 10

systemctl status taskverse-codingengine.service --no-pager