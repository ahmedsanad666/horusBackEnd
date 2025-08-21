# 🚀 Horus API Deployment Guide with HTTPS

## Overview

This guide explains how to deploy your Horus API to Digital Ocean with automatic HTTPS using Caddy and nip.io.

## 🔧 What We've Added

### 1. **Caddy Service** (HTTPS Termination)

- Automatic Let's Encrypt SSL certificates
- Reverse proxy to your API
- Security headers and CORS handling

### 2. **Updated Docker Compose**

- Caddy service with ports 80 and 443
- API service now only exposed internally
- Proper volume management for Caddy

### 3. **Caddyfile Configuration**

- HTTPS with automatic certificate renewal
- Reverse proxy to `api:8080`
- Security headers and CORS configuration

## 📋 Prerequisites

1. **Digital Ocean Droplet** with Docker and Docker Compose
2. **Domain or IP Address** (we'll use nip.io for IP-based domains)
3. **Ports 80 and 443** open in your firewall

## 🚀 Deployment Steps

### Step 1: Update Your Droplet

```bash
# SSH into your droplet
ssh root@your-droplet-ip

# Navigate to your project directory
cd /path/to/your/project

# Pull latest changes
git pull origin main
```

### Step 2: Update Caddyfile

**IMPORTANT**: Replace `206-189-218-64` with your actual droplet IP address in the `Caddyfile`:

```caddy
# Change this line in Caddyfile:
api.206-189-218-64.nip.io {
    # ... rest of config
}
```

### Step 3: Deploy

```bash
# Make deploy script executable
chmod +x deploy.sh

# Run deployment
./deploy.sh
```

### Step 4: Verify Deployment

```bash
# Check service status
docker-compose ps

# Check Caddy logs for HTTPS setup
docker-compose logs caddy

# Test your API
curl -X GET 'https://api.YOUR-IP.nip.io/api/health'
```

## 🌐 Accessing Your API

### **New HTTPS URL:**

```
https://api.YOUR-IP.nip.io
```

### **Example:**

If your droplet IP is `206.189.218.64`, your API will be available at:

```
https://api.206-189-218-64.nip.io
```

## 🔒 Security Features

- **Automatic HTTPS** with Let's Encrypt
- **Security Headers** (XSS protection, content type options, etc.)
- **CORS Configuration** for your frontend domain
- **HTTP to HTTPS Redirect**

## 📱 Frontend Updates

Update your frontend application to use the new HTTPS URL:

```javascript
// Before (causing mixed content error)
const API_BASE = "http://206.189.218.64";

// After (secure HTTPS)
const API_BASE = "https://api.206-189-218-64.nip.io";
```

## 🐛 Troubleshooting

### **Caddy Certificate Issues:**

```bash
# Check Caddy logs
docker-compose logs caddy

# Restart Caddy service
docker-compose restart caddy
```

### **API Connection Issues:**

```bash
# Check API logs
docker-compose logs api

# Test internal connectivity
docker-compose exec caddy curl api:8080/api/health
```

### **Port Conflicts:**

```bash
# Check what's using ports 80/443
sudo netstat -tlnp | grep :80
sudo netstat -tlnp | grep :443
```

## 📊 Monitoring

```bash
# View all service logs
docker-compose logs -f

# Check resource usage
docker stats

# View Caddy configuration
docker-compose exec caddy cat /etc/caddy/Caddyfile
```

## 🔄 Updating

To update your deployment:

```bash
git pull origin main
docker-compose up -d --build
```

## ✅ Benefits

1. **No More Mixed Content Errors** - Your frontend can now call HTTPS APIs
2. **Automatic SSL** - Let's Encrypt handles certificate management
3. **Security Headers** - Added protection against common attacks
4. **Professional Setup** - Proper reverse proxy with HTTPS termination

## 🆘 Support

If you encounter issues:

1. Check the logs: `docker-compose logs [service-name]`
2. Verify your IP address in the Caddyfile
3. Ensure ports 80 and 443 are open in your firewall
4. Check that your droplet has enough resources

---

**🎉 Your API is now secure and ready for production use!**
