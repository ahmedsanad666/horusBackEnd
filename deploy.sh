#!/bin/bash

# Deployment script for Horus API with Caddy HTTPS
# Run this on your Digital Ocean droplet

echo "🚀 Deploying Horus API with HTTPS support..."

# Stop existing containers
echo "📦 Stopping existing containers..."
docker-compose down

# Pull latest changes
echo "📥 Pulling latest changes..."
git pull origin main

# Build and start services
echo "🔨 Building and starting services..."
docker-compose up -d --build

# Wait for services to be ready
echo "⏳ Waiting for services to be ready..."
sleep 30

# Check service status
echo "📊 Checking service status..."
docker-compose ps

# Check Caddy logs for HTTPS setup
echo "🔒 Checking Caddy HTTPS setup..."
docker-compose logs caddy | tail -20

# Check API logs
echo "📝 Checking API logs..."
docker-compose logs api | tail -20

echo "✅ Deployment complete!"
echo ""
echo "🌐 Your API is now available at:"
echo "   HTTPS: https://api.206-189-218-64.nip.io"
echo "   (Replace 206-189-218-64 with your actual droplet IP)"
echo ""
echo "🔍 Test your API:"
echo "   curl -X GET 'https://api.206-189-218-64.nip.io/api/health'"
echo ""
echo "📱 Update your frontend to use:"
echo "   https://api.206-189-218-64.nip.io"
echo "   instead of http://206.189.218.64"
