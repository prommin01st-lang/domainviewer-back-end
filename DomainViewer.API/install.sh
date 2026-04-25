#!/bin/bash
set -e

echo "=== DomainViewer API Installer ==="

# Check if running as root
if [ "$EUID" -ne 0 ]; then
    echo "Please run as root (sudo ./install.sh)"
    exit 1
fi

# Create app user if not exists
if ! id -u domainviewer &>/dev/null; then
    useradd -r -s /bin/false -d /opt/domainviewer domainviewer
    echo "Created user: domainviewer"
fi

APP_DIR="/opt/domainviewer"
SRC_DIR="/opt/domainviewer-src"
mkdir -p "$APP_DIR"

# ============================================
# MODE 1: Build from source on server (Recommended)
# ============================================
if [ -d "$SRC_DIR/.git" ]; then
    echo "=== Mode: Build from source ==="
    cd "$SRC_DIR"

    # Pull latest
    git pull origin main || true

    # Ensure config exists
    if [ ! -f "DomainViewer.API/appsettings.json" ]; then
        echo "ERROR: appsettings.json not found!"
        echo "Please create it from appsettings.Example.json:"
        echo "  cp DomainViewer.API/appsettings.Example.json DomainViewer.API/appsettings.json"
        echo "  nano DomainViewer.API/appsettings.json"
        exit 1
    fi

    # Publish
    dotnet publish DomainViewer.API/DomainViewer.API.csproj \
        -c Release \
        -o "$APP_DIR" \
        --no-self-contained \
        -p:PublishSingleFile=false

# ============================================
# MODE 2: Extract pre-built zip
# ============================================
elif [ -f /tmp/publish-ubuntu.zip ]; then
    echo "=== Mode: Extract pre-built zip ==="
    rm -rf "$APP_DIR"/*
    unzip -o /tmp/publish-ubuntu.zip -d "$APP_DIR"
    rm -f /tmp/publish-ubuntu.zip

elif [ -f /home/*/publish-ubuntu.zip ]; then
    echo "=== Mode: Extract pre-built zip from home ==="
    rm -rf "$APP_DIR"/*
    unzip -o /home/*/publish-ubuntu.zip -d "$APP_DIR"

else
    echo "ERROR: No source or zip found!"
    echo ""
    echo "Option A - Build on server:"
    echo "  git clone <your-repo> $SRC_DIR"
    echo "  cd $SRC_DIR"
    echo "  cp DomainViewer.API/appsettings.Example.json DomainViewer.API/appsettings.json"
    echo "  nano DomainViewer.API/appsettings.json    # Edit config"
    echo "  sudo ./install.sh"
    echo ""
    echo "Option B - Pre-built zip:"
    echo "  scp publish-ubuntu.zip user@server:/tmp/"
    echo "  sudo ./install.sh"
    exit 1
fi

# Set permissions
chown -R domainviewer:domainviewer "$APP_DIR"
chmod +x "$APP_DIR"/DomainViewer.API

# Install systemd service
cat > /etc/systemd/system/domainviewer.service << 'EOF'
[Unit]
Description=DomainViewer API
After=network.target

[Service]
Type=simple
User=domainviewer
Group=domainviewer
WorkingDirectory=/opt/domainviewer
ExecStart=/opt/domainviewer/DomainViewer.API
Restart=always
RestartSec=10
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://0.0.0.0:5000

[Install]
WantedBy=multi-user.target
EOF

# Reload and start
systemctl daemon-reload
systemctl enable domainviewer
systemctl restart domainviewer

# Show status
sleep 2
echo ""
echo "=== Installation Complete ==="
echo ""
systemctl status domainviewer --no-pager
echo ""
echo "View logs:   sudo journalctl -u domainviewer -f"
echo "API URL:     http://$(hostname -I | awk '{print $1}'):5000"
echo ""
echo "Next steps:"
echo "  1. Run migration:  cd $SRC_DIR && dotnet ef database update --project DomainViewer.Infrastructure --startup-project DomainViewer.API"
echo "  2. Setup ngrok:    ngrok http 5000"
echo "  3. Update Vercel env: API_PROXY_URL=https://your-ngrok-url"
