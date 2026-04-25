#!/bin/bash
set -e

echo "=== DomainViewer API Updater ==="

SRC_DIR="/opt/domainviewer-src"
APP_DIR="/opt/domainviewer"

if [ "$EUID" -ne 0 ]; then
    echo "Please run as root (sudo ./deploy.sh)"
    exit 1
fi

cd "$SRC_DIR"

echo "Pulling latest code..."
git pull origin main

echo "Building..."
dotnet publish DomainViewer.API/DomainViewer.API.csproj \
    -c Release \
    -o "$APP_DIR" \
    --no-self-contained \
    -p:PublishSingleFile=false

chown -R domainviewer:domainviewer "$APP_DIR"

echo "Restarting service..."
systemctl restart domainviewer

sleep 2
echo ""
echo "=== Update Complete ==="
systemctl status domainviewer --no-pager --lines=5
