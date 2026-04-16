# 🚀 Quick Start Guide - After Kafka Fixes

## ✅ Những gì đã được fix

### 1. **ConsumerConfig** - Thêm các properties quan trọng
- ✅ `EnableAutoCommit = true` - Tự động commit offsets
- ✅ `StatisticsIntervalMs = 60000` - Collect metrics
- ✅ `SessionTimeoutMs = 6000` - Session timeout 6 giây
- ✅ `AllowAutoCreateTopics = true` - Tự động tạo topic nếu không tồn tại

### 2. **Exception Handling** - Không crash app nữa
- ✅ Consumer không throw exception khi initialization fail
- ✅ Application vẫn start thành công dù Kafka tạm thời unavailable
- ✅ Error handler logs non-fatal errors

### 3. **Resource Management** - Proper cleanup
- ✅ Consumer được dispose properly
- ✅ Try-finally đảm bảo cleanup
- ✅ Override Dispose() method

### 4. **Duplicate Services** - Removed
- ✅ Xóa duplicate registrations trong DependencyInjection.cs
- ✅ Một source of truth cho Kafka configuration

---

## 🎯 Để chạy ứng dụng

### Step 1: Kiểm tra Kafka đang chạy
```bash
docker ps | grep kafka
```

Nếu thấy:
```
CONTAINER ID   IMAGE               STATUS            PORTS
f1f8c997d149   apache/kafka:3.9.0  Up 25 minutes    0.0.0.0:9092->9092/tcp
```
✅ Kafka đang chạy. Tiếp tục Step 2.

Nếu không thấy:
```bash
# Khởi động Kafka
docker start kafka

# Hoặc (nếu đã có docker-compose)
docker-compose up -d kafka
```

### Step 2: Chạy ứng dụng
```bash
cd /Users/pdm-mac/PDM/Server/SurveillanceCameras
dotnet run --project src/Web/Web.csproj
```

### Step 3: Kiểm tra logs
Bạn sẽ thấy:
```
[Kafka] BootstrapServers: localhost:9092
[Kafka] GroupId: surveillance-consumer-group
[Kafka] Topics: story-created
[Kafka] Successfully subscribed to topics
```

✅ Nếu thấy "Successfully subscribed to topics" → Kafka consumer đang chạy!

---

## 🧪 Để test Kafka messaging

### Gửi một test message
```bash
# SSH vào Kafka container
docker exec -it kafka bash

# Tạo message
echo '{"storyId":"test-123","title":"Test Story"}' | \
kafka-console-producer.sh \
  --broker-list localhost:9092 \
  --topic story-created
```

### Xem application logs
Application sẽ process message từ topic `story-created` và gọi `StoryCreatedHandler`.

---

## 📊 Monitoring

### Xem Kafka logs
```bash
docker logs -f kafka | grep -E "ERROR|WARNING"
```

### Xem topics
```bash
docker exec kafka kafka-topics.sh \
  --list \
  --bootstrap-server localhost:9092
```

Output:
```
story-created
__consumer_offsets
```

### Xem consumer groups
```bash
docker exec kafka kafka-consumer-groups.sh \
  --list \
  --bootstrap-server localhost:9092
```

Output:
```
surveillance-consumer-group
```

---

## ❌ Nếu vẫn gặp lỗi

### Error: "Local: Invalid argument or configuration"
1. **Kiểm tra Kafka running:**
   ```bash
   docker ps | grep kafka
   ```

2. **Kiểm tra config:**
   ```bash
   cat src/Web/appsettings.Development.json | grep -A 5 "Kafka"
   ```
   
   Output phải là:
   ```json
   "Kafka": {
     "BootstrapServers": "localhost:9092",
     "Topics": ["story-created"],
     "GroupId": "surveillance-consumer-group"
   }
   ```

3. **Tạo topic thủ công:**
   ```bash
   docker exec kafka kafka-topics.sh \
     --create \
     --if-not-exists \
     --bootstrap-server localhost:9092 \
     --topic story-created \
     --partitions 1 \
     --replication-factor 1
   ```

### Error: "No brokers are available"
```bash
# Khởi động lại Kafka
docker restart kafka

# Chờ 10 giây
sleep 10

# Chạy lại app
dotnet run --project src/Web/Web.csproj
```

### Error: "Connection refused"
```bash
# Kiểm tra port 9092
lsof -i :9092

# Nếu không thấy gì, Kafka chưa chạy:
docker start kafka
```

---

## 📝 Configuration Files

### `src/Web/appsettings.Development.json`
```json
{
  "Kafka": {
    "BootstrapServers": "localhost:9092",
    "Topics": ["story-created"],
    "GroupId": "surveillance-consumer-group"
  }
}
```

### `src/Web/appsettings.json`
```json
{
  "Kafka": {
    "BootstrapServers": "localhost:9092",
    "Topics": ["story-created"],
    "GroupId": "surveillance-consumer-group"
  }
}
```

---

## 📚 Thêm thông tin

- Xem `KAFKA_FIXES.md` - Danh sách các fix được áp dụng
- Xem `KAFKA_CONFIGURATION_ANALYSIS.md` - Chi tiết phân tích từng vấn đề

---

## ✨ Success Indicators

✅ Application starts without errors
✅ Logs show "Successfully subscribed to topics"
✅ No exceptions in console
✅ Can send and receive messages from Kafka
✅ StoryCreatedHandler is called when messages arrive

**Nếu thấy tất cả những điều trên → Kafka configuration OK!** 🎉

