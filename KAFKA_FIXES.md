# Kafka Configuration Issues - Fix Summary

## 🔴 Vấn đề được tìm thấy

### 1. **ConsumerConfig thiếu cấu hình quan trọng**
- Thiếu `EnableAutoCommit` - không auto-commit offsets
- Thiếu `StatisticsIntervalMs` - không collect statistics
- Thiếu `SessionTimeoutMs` - timeout quá cao
- Thiếu `AllowAutoCreateTopics` - không tạo topic tự động nếu không tồn tại

**Lỗi khi gặp vấn đề này:**
```
Confluent.Kafka.KafkaException: Local: Invalid argument or configuration
```

### 2. **Duplicate Service Registration**
- `KafkaExtensions.AddKafka()` đã đăng ký `IKafkaProducer`, `KafkaConsumerBackgroundService`, và `StoryCreatedHandler`
- Nhưng `DependencyInjection.cs` lại đăng ký thêm một lần nữa (dòng 45-47)
- Điều này gây confusion và có thể dẫn đến incorrect instance

### 3. **Kafka Consumer không handle exception tốt**
- Nếu config sai, service sẽ throw exception và stop host
- Application sẽ không thể start nếu Kafka không available

### 4. **Resource Leak**
- `IConsumer` không được dispose properly nếu exception xảy ra

### 5. **Topics không tồn tại**
- Nếu topic `story-created` không tồn tại trong Kafka, consumer sẽ fail khi subscribe
- Cần tạo topics trước khi chạy application

---

## ✅ Các Fix được áp dụng

### 1. **KafkaConsumerBackgroundService.cs**
```csharp
// Thêm cấu hình quan trọng
var config = new ConsumerConfig
{
    BootstrapServers = _options.BootstrapServers,
    GroupId = _options.GroupId ?? "surveillance-consumer-group",
    AutoOffsetReset = AutoOffsetReset.Earliest,
    EnableAutoCommit = true,                  // ✅ THÊM
    StatisticsIntervalMs = 60000,            // ✅ THÊM
    SessionTimeoutMs = 6000,                 // ✅ THÊM
    EnablePartitionEof = false,              // ✅ THAY ĐỔI
    AllowAutoCreateTopics = true             // ✅ THÊM
};

// Error handler
.SetErrorHandler((_, error) =>
{
    if (!error.IsFatal)
    {
        Console.WriteLine($"[Kafka] WARNING: {error.Code}: {error.Reason}");
    }
    else
    {
        Console.WriteLine($"[Kafka] FATAL ERROR: {error.Code}: {error.Reason}");
    }
})

// Proper resource cleanup
finally
{
    if (_consumer != null)
    {
        _consumer.Close();
        _consumer.Dispose();
        Console.WriteLine("[Kafka] Consumer closed");
    }
}
```

### 2. **Loại bỏ Duplicate Service Registration**
- Xóa các dòng 45-47 trong `DependencyInjection.cs`
- Giữ lại chỉ `builder.Services.AddKafka(builder.Configuration);`

---

## 🚀 Cách chạy ứng dụng

### Bước 1: Đảm bảo Kafka đang chạy
```bash
# Kiểm tra Kafka container
docker ps | grep kafka

# Nếu chưa chạy, khởi động nó
docker-compose up -d kafka  # Nếu có docker-compose
# hoặc start lại container
docker start kafka
```

### Bước 2: Tạo Topics (optional - sẽ tự động tạo nếu AllowAutoCreateTopics=true)
```bash
# Cách 1: Dùng script
chmod +x setup-kafka-topics.sh
./setup-kafka-topics.sh kafka localhost:9092

# Cách 2: Manual
docker exec kafka sh -c "
kafka-topics.sh --bootstrap-server localhost:9092 \\
  --create --if-not-exists \\
  --topic story-created \\
  --partitions 1 --replication-factor 1
"
```

### Bước 3: Chạy Application
```bash
cd src/Web
dotnet run
```

---

## 🔍 Verification

### Kiểm tra Kafka logs
```bash
docker logs -f kafka | grep -E "ERROR|WARNING|story-created"
```

### Kiểm tra Application logs
```
[Kafka] BootstrapServers: localhost:9092
[Kafka] GroupId: surveillance-consumer-group
[Kafka] Topics: story-created
[Kafka] Successfully subscribed to topics
```

### Test Topic
```bash
# Produce message
docker exec kafka sh -c "
echo '{\"id\": \"123\"}' | kafka-console-producer.sh \\
  --broker-list localhost:9092 \\
  --topic story-created
"

# Check logs
docker logs -f kafka | grep story-created
```

---

## 📋 Kafka Connection Troubleshooting

| Vấn đề | Nguyên nhân | Giải pháp |
|--------|-----------|----------|
| `Local: Invalid argument or configuration` | Config thiếu hoặc sai | Kiểm tra `ConsumerConfig` setup |
| `Local: Broker transport failure` | Kafka không chạy | `docker ps \| grep kafka` |
| `Local: Topic authorization failed` | Topic không tồn tại | Tạo topic hoặc set `AllowAutoCreateTopics=true` |
| `No brokers are available` | Kafka URL sai | Check `BootstrapServers` config |
| `GroupId error` | GroupId config sai | Set hợp lệ `GroupId` |

---

## 📝 Cấu hình trong appsettings.json

```json
{
  "Kafka": {
    "BootstrapServers": "localhost:9092",
    "Topics": ["story-created"],
    "GroupId": "surveillance-consumer-group"
  }
}
```

**Chú ý:** Không để duplicate values trong `Topics` array!

---

## ⚠️ Lưu ý quan trọng

1. **AllowAutoCreateTopics=true** - Cho phép Kafka tự động tạo topic nếu không tồn tại
   - Trong production, nên disable và tạo topics manually
   
2. **EnableAutoCommit=true** - Tự động commit offsets
   - Nếu bạn muốn manual commit, set false và gọi `_consumer.Commit(result)` sau khi xử lý
   
3. **SessionTimeoutMs=6000** - 6 second timeout
   - Nếu consumer không heartbeat trong 6s, sẽ bị rebalance
   - Điều chỉnh tuỳ vào throughput của bạn

4. **Consumer không throw exception nữa** - App sẽ tiếp tục chạy dù Kafka fail
   - Điều này cho phép app start thành công ngay cả khi Kafka temporary unavailable
   - Nhưng hãy monitor logs để phát hiện issues

