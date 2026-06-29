#!/bin/bash
set -e

echo "===== BEFORE INSTALL ====="

mkdir -p /var/www

mkdir -p /var/www/assets

rm -rf /var/www/*

echo "Old deployment cleaned."