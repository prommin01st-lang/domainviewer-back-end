#!/bin/bash
set -e

echo "=== DomainViewer API Installer ==="

# Check if running as root
if [ "$EUID" -ne 0 ]; then 
    echo "Please run as root (sudo ./install.sh)"
    exit 1
fi

# Create app directory
mkdir -p /opt/domainviewer
cd /opt/domainviewer

# Extract published files
if [ -f /tmp/publish-ubuntu.zip ]; then
    echo "Extracting publish-ubuntu.zip..."
    unzip -o /tmp/publish-ubuntu.zip -d /opt/domainviewer
elif [ -f /home/*/publish-ubuntu.zip ]; then
    echo "Extracting publish-ubuntu.zip from home..."
    unzip -o /home/*/publish-ubuntu.zip -d /opt/domainviewer
else
    echo "ERROR: publish-ubuntu.zip not found!"
    echo "Please copy publish-ubuntu.zip to /tmp/ first:"
    echo "  scp publish-ubuntu.zip user@server:/tmp/"
    exit 1
fi

# Set permissions
chmod +x /opt/domainviewer/DomainViewer.API

# Create systemd service
cat > /etc/systemd/system/domainviewer.service << 'EOF'
[Unit]
Description=DomainViewer API
After=network.target

[Service]
Type=simple
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
echo "View logs: sudo journalctl -u domainviewer -f"
echo "API URL:   http://$(hostname -I | awk '{print $1}'):5000"
