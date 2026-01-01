#!/bin/bash

# Local development environment setup
# Starts PostgreSQL and Redis using localstack or Docker Compose

set -e

echo "Starting Luna local environment..."

# Check if Docker is running
if ! docker info > /dev/null 2>&1; then
    echo "Error: Docker is not running"
    exit 1
fi

# Start services
docker-compose -f ../../docker/compose.local.yml up -d

echo "✓ PostgreSQL running on localhost:5432"
echo "✓ Redis running on localhost:6379"
echo ""
echo "Credentials:"
echo "  - PostgreSQL: luna / password"
echo "  - Database: luna_db"
