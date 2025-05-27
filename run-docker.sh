#!/bin/bash

echo "🐳 Starting Aggregator with Docker..."

# Check if Docker is running
if ! docker info > /dev/null 2>&1; then
    echo "❌ Docker is not running. Please start Docker first."
    exit 1
fi

# Check if docker-compose is available
if ! command -v docker-compose &> /dev/null; then
    echo "❌ docker-compose not found. Please install docker-compose."
    exit 1
fi

# Clean up any existing containers
echo "🧹 Cleaning up existing containers..."
docker-compose down

echo "📦 Building and starting services..."
echo "   1️⃣  PostgreSQL will start first"
echo "   2️⃣  Migrations will run as init container"
echo "   3️⃣  Application will start after migrations complete"
echo ""

# Start all services - Docker Compose will handle the dependency order
docker-compose up --build

echo "✅ Done! Check the logs above for results." 