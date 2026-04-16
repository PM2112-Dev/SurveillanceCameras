#!/bin/bash

# Script để tạo Kafka topics
# Kafka phải đang chạy trước

KAFKA_CONTAINER_NAME="${1:-kafka}"
BOOTSTRAP_SERVER="${2:-localhost:9092}"

echo "Setting up Kafka topics..."
echo "Container: $KAFKA_CONTAINER_NAME"
echo "Bootstrap Server: $BOOTSTRAP_SERVER"

# List các topics cần tạo
TOPICS=("story-created")

for topic in "${TOPICS[@]}"; do
    echo "Creating topic: $topic"
    
    # Try using kafka-topics.sh if available in container
    docker exec "$KAFKA_CONTAINER_NAME" bash -c "
        # Find kafka-topics script
        if [ -f /opt/kafka/bin/kafka-topics.sh ]; then
            /opt/kafka/bin/kafka-topics.sh --bootstrap-server $BOOTSTRAP_SERVER --create --topic $topic --if-not-exists --partitions 1 --replication-factor 1
        elif [ -f /kafka/bin/kafka-topics.sh ]; then
            /kafka/bin/kafka-topics.sh --bootstrap-server $BOOTSTRAP_SERVER --create --topic $topic --if-not-exists --partitions 1 --replication-factor 1
        else
            echo 'kafka-topics.sh not found'
            exit 1
        fi
    " || echo "Could not create topic $topic"
done

echo "Topics setup complete!"
echo ""
echo "To list topics, run:"
echo "  docker exec $KAFKA_CONTAINER_NAME kafka-topics --list --bootstrap-server $BOOTSTRAP_SERVER"

