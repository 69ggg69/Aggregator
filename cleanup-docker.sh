#!/bin/bash

echo "🧹 Cleaning up Docker containers and volumes..."

# Stop and remove containers
docker-compose down

# Remove volumes (optional - uncomment if you want to reset the database)
docker-compose down -v

# Remove built images (optional - uncomment if you want to rebuild from scratch)
# docker rmi aggregator_aggregator 2>/dev/null || true

echo "✅ Cleanup completed!" 