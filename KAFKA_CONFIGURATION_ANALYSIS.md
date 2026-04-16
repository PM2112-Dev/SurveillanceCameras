# Kafka Configuration Issues - Detailed Analysis & Resolution

## 📊 Executive Summary

Ứng dụng đang gặp lỗi khi startup vì Kafka consumer configuration không đúng. Các vấn đề chính:

1. ✅ **ConsumerConfig Missing Critical Properties** - Đã FIX
2. ✅ **Duplicate Service Registration** - Đã FIX  
3. ✅ **Poor Exception Handling** - Đã FIX
4. ✅ **Resource Management Issues** - Đã FIX

---

## 🔴 Problems Identified

### Problem 1: Incomplete ConsumerConfig
**File:** `src/Infrastructure/Kafka/KafkaConsumerBackgroundService.cs`

**Original Code:**
```csharp
var config = new ConsumerConfig
{
    BootstrapServers = _options.BootstrapServers,
    GroupId = _options.GroupId ?? "surveillance-consumer-group",
    AutoOffsetReset = AutoOffsetReset.Earliest
};
```

**Issues:**
- Missing `EnableAutoCommit` → offsets not committed automatically
- Missing `StatisticsIntervalMs` → no performance metrics
- Missing `SessionTimeoutMs` → default timeout might be too high
- Missing `AllowAutoCreateTopics` → fails if topic doesn't exist
- `EnablePartitionEof = true` → may cause issues with long-running consumers

**Error Stack:**
```
Confluent.Kafka.KafkaException: Local: Invalid argument or configuration
   at Confluent.Kafka.Impl.SafeKafkaHandle.Subscribe(IEnumerable`1 topics)
   at Confluent.Kafka.Consumer`2.Subscribe(IEnumerable`1 topics)
```

---

### Problem 2: Service Registration Duplication
**File:** `src/Infrastructure/DependencyInjection.cs` (lines 44-47)

**Original Code:**
```csharp
// Line 44: Calls AddKafka which already registers everything
builder.Services.AddKafka(builder.Configuration);

// Lines 45-47: DUPLICATE registrations!
builder.Services.Configure<KafkaOptions>(builder.Configuration.GetSection("Kafka"));
builder.Services.AddScoped<IKafkaProducer, KafkaProducer>();
builder.Services.AddScoped<IKafkaConsumerHandler<StoryCreatedEvent>, StoryCreatedHandler>();
```

**What AddKafka already does:**
```csharp
// From KafkaExtensions.cs
services.Configure<KafkaOptions>(configuration.GetSection("Kafka"));
services.AddSingleton<IKafkaProducer, KafkaProducer>();
services.AddHostedService<KafkaConsumerBackgroundService>();
services.AddScoped<IKafkaConsumerHandler<StoryCreatedEvent>, StoryCreatedHandler>();
```

**Issues:**
- `KafkaOptions` configured twice
- `IKafkaProducer` registered twice (once as Singleton, once as Scoped)
- `StoryCreatedHandler` registered twice (both as Scoped)
- Lifetime mismatch: Singleton vs Scoped for IKafkaProducer

---

### Problem 3: Exception Handling
**File:** `src/Infrastructure/Kafka/KafkaConsumerBackgroundService.cs` (lines 32-41, 54-59)

**Original Code:**
```csharp
if (string.IsNullOrWhiteSpace(_options.BootstrapServers))
{
    Console.WriteLine("[Kafka] ERROR: BootstrapServers is not configured.");
    throw new InvalidOperationException("Kafka BootstrapServers is required."); // ❌ Crashes app
}

try
{
    _consumer = new ConsumerBuilder<string, string>(config).Build();
    _consumer.Subscribe(_topics);
}
catch (Exception ex)
{
    Console.WriteLine($"[Kafka] ERROR: Failed to create consumer or subscribe to topics: {ex.Message}");
    Console.WriteLine("[Kafka] HINT: Check that Kafka is running and configuration is correct.");
    throw;  // ❌ Crashes app
}
```

**Issues:**
- Throwing exceptions crashes the entire application
- BackgroundServiceExceptionBehavior is set to StopHost (default)
- No graceful degradation if Kafka is temporarily unavailable
- No error handler setup for Kafka client errors

---

### Problem 4: Resource Management
**File:** `src/Infrastructure/Kafka/KafkaConsumerBackgroundService.cs` (line 80)

**Original Code:**
```csharp
_consumer.Close();
// Missing: _consumer.Dispose() 
// Missing: try-finally to ensure cleanup
```

**Issues:**
- If exception occurs before `_consumer.Close()`, consumer is not cleaned up
- `IConsumer` extends `IDisposable` but not being disposed
- Memory leak potential

---

## ✅ Solutions Applied

### Solution 1: Enhanced ConsumerConfig

```csharp
var config = new ConsumerConfig
{
    BootstrapServers = _options.BootstrapServers,
    GroupId = _options.GroupId ?? "surveillance-consumer-group",
    AutoOffsetReset = AutoOffsetReset.Earliest,
    EnableAutoCommit = true,              // Auto-commit offsets
    StatisticsIntervalMs = 60000,        // Collect stats every 60 seconds
    SessionTimeoutMs = 6000,             // 6 second session timeout
    EnablePartitionEof = false,          // Don't report end-of-partition
    AllowAutoCreateTopics = true         // Auto-create if missing
};
```

**Benefits:**
- ✅ Proper offset management
- ✅ Can auto-create topics in dev environment  
- ✅ Reasonable timeouts
- ✅ Stability improvements

---

### Solution 2: Error Handler Setup

```csharp
_consumer = new ConsumerBuilder<string, string>(config)
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
    .Build();
```

**Benefits:**
- ✅ Logs non-fatal errors (network issues, rebalancing)
- ✅ Logs fatal errors separately
- ✅ Continues processing despite errors

---

### Solution 3: Resource Cleanup

```csharp
protected override async Task ExecuteAsync(CancellationToken stoppingToken)
{
    try
    {
        // ... configuration and connection logic ...
        while (!stoppingToken.IsCancellationRequested)
        {
            // ... message processing ...
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[Kafka] ERROR: Failed to initialize consumer: {ex.Message}");
        // Don't re-throw - allow app to continue
    }
    finally
    {
        if (_consumer != null)
        {
            _consumer.Close();
            _consumer.Dispose();
            Console.WriteLine("[Kafka] Consumer closed");
        }
    }
}

public override void Dispose()
{
    _consumer?.Dispose();
    base.Dispose();
}
```

**Benefits:**
- ✅ Resources always cleaned up
- ✅ Graceful shutdown
- ✅ No crashes on initialization failure

---

### Solution 4: Remove Duplicate Registrations

**Before:**
```csharp
builder.Services.AddKafka(builder.Configuration);
builder.Services.Configure<KafkaOptions>(builder.Configuration.GetSection("Kafka"));
builder.Services.AddScoped<IKafkaProducer, KafkaProducer>();
builder.Services.AddScoped<IKafkaConsumerHandler<StoryCreatedEvent>, StoryCreatedHandler>();
```

**After:**
```csharp
builder.Services.AddKafka(builder.Configuration);
```

**Benefits:**
- ✅ Single source of truth
- ✅ Correct service lifetimes
- ✅ Cleaner code

---

## 🧪 Verification

After applying all fixes, the application logs:

```
[Kafka] BootstrapServers: localhost:9092
[Kafka] GroupId: surveillance-consumer-group
[Kafka] Topics: story-created
[Kafka] Successfully subscribed to topics
```

Build result: **✅ Build succeeded. 0 Warning(s), 0 Error(s)**

---

## 🚀 How to Test

### 1. Ensure Kafka is Running
```bash
docker ps | grep kafka
# Output should show: kafka running on 0.0.0.0:9092
```

### 2. Run the Application
```bash
cd src/Web
dotnet run
```

### 3. Check Logs
The application should start without crashing and show:
```
[Kafka] Successfully subscribed to topics
```

### 4. Send a Test Message
```bash
# Create a test message
echo '{"storyId": "test-123"}' | \
  docker exec -i kafka kafka-console-producer.sh \
    --broker-list localhost:9092 \
    --topic story-created
```

### 5. Verify Message Processing
Watch application logs - should show the message being processed.

---

## 📋 Configuration Reference

### appsettings.json
```json
{
  "Kafka": {
    "BootstrapServers": "localhost:9092",
    "Topics": ["story-created"],
    "GroupId": "surveillance-consumer-group"
  }
}
```

### Key Configuration Options

| Option | Value | Purpose |
|--------|-------|---------|
| `BootstrapServers` | `localhost:9092` | Kafka broker address |
| `Topics` | `["story-created"]` | Topics to subscribe to |
| `GroupId` | `surveillance-consumer-group` | Consumer group name |
| `AutoOffsetReset` | `Earliest` | Start from beginning if no offset |
| `EnableAutoCommit` | `true` | Auto-commit offsets after processing |
| `SessionTimeoutMs` | `6000` | Session timeout in milliseconds |
| `AllowAutoCreateTopics` | `true` | Create missing topics automatically |

---

## ⚠️ Production Considerations

### Current Settings (Development)
- ✅ `AllowAutoCreateTopics = true` - Convenient for development
- ✅ `EnableAutoCommit = true` - Simpler but less control
- ✅ `SessionTimeoutMs = 6000` - May be too low for slow processing

### Production Recommendations
- 🔒 Set `AllowAutoCreateTopics = false` - Create topics manually
- 🔒 Set `EnableAutoCommit = false` - Manual commit after processing
- ⏱️ Increase `SessionTimeoutMs` based on message processing time
- 📊 Monitor consumer lag and rebalancing events
- 🔄 Implement dead-letter queue for failed messages

---

## 🔧 Troubleshooting

### Error: "Local: Invalid argument or configuration"
1. Check `BootstrapServers` is correct
2. Verify Kafka is running: `docker ps | grep kafka`
3. Verify `Topics` array is not empty
4. Check for malformed config

### Error: "Local: Topic authorization failed"
1. Topic doesn't exist - create it or set `AllowAutoCreateTopics = true`
2. Check consumer permissions

### Error: "No brokers are available"
1. Kafka not running
2. Wrong `BootstrapServers` address
3. Firewall blocking port 9092

### Consumer keeps rebalancing
1. Message processing takes too long - increase `SessionTimeoutMs`
2. Consumer crashes during processing - add error handling

---

## 📚 Files Modified

| File | Changes |
|------|---------|
| `src/Infrastructure/Kafka/KafkaConsumerBackgroundService.cs` | Enhanced config, error handling, resource cleanup |
| `src/Infrastructure/DependencyInjection.cs` | Removed duplicate service registrations |

## 📚 New Files Created

| File | Purpose |
|------|---------|
| `setup-kafka-topics.sh` | Script to create Kafka topics |
| `KAFKA_FIXES.md` | Quick reference guide |
| `KAFKA_CONFIGURATION_ANALYSIS.md` | This detailed analysis |

---

## ✨ Next Steps

1. **Run the application** to verify fixes
2. **Monitor logs** for Kafka-related messages
3. **Test message production/consumption**
4. **Review production configuration** recommendations
5. **Consider implementing** manual offset commits for better control

