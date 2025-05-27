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

echo "📦 Building and starting PostgreSQL..."
docker-compose up --build -d postgres

echo "⏳ Waiting for PostgreSQL to be ready..."
sleep 20

echo "🚀 Starting the aggregator application (with automatic database creation)..."
docker-compose up --build aggregator

echo "✅ Done! Check the logs above for parsing results." 