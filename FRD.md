# FRD - Hệ thống Quản lý Truyện Tự động
**Functional Requirements Document**

**Version:** 1.0  
**Date:** 2026-08-23  
**Status:** Draft

---

## 1. MỤC ĐÍCH

### 1.1 Tổng quan hệ thống
Hệ thống Quản lý Truyện Tự động là nền tảng giúp tự động hóa quy trình: phát hiện truyện mới → crawl nội dung → dịch AI → đăng lên nền tảng xuất bản (TYT) → sản xuất nội dung video (YouTube).

### 1.2 Mục tiêu
- Giảm thiểu thao tác thủ công của operator
- Đảm bảo chất lượng dịch thuật nhất quán
- Quản lý tập trung nhiều tài khoản TYT và YouTube
- Theo dõi trạng thái và tiến độ xử lý theo thời gian thực
- Hỗ trợ mở rộng thêm nền tảng đích trong tương lai

---

## 2. PHẠM VI (SCOPE)

### 2.1 Thuộc hệ thống (v1.0)

| Module | Mô tả |
|--------|-------|
| **Story Management** | Tạo, xem, sửa, xóa truyện |
| **Wikidich Crawler** | Tự động crawl thông tin truyện từ Wikidich |
| **Content Translation** | Dịch nội dung word-by-word sang tiếng Việt tự nhiên bằng Gemini |
| **TYT Publisher** | Đăng truyện và chapter lên nền tảng TYT |
| **Account Management** | Quản lý tài khoản TYT (cookie) và YouTube (credentials) |
| **Dashboard & Tracking** | Theo dõi trạng thái story, chapters, tiến độ xử lý |
| **User Authentication** | JWT-based authentication, phân quyền operator |

### 2.2 Không thuộc hệ thống (v1.0)

| Module | Lý do | Roadmap |
|--------|-------|---------|
| **YouTube Video Production** | Phức tạp, cần TTS engine + video editing | v2.0 |
| **Multi-language Translation** | Chỉ hỗ trợ Trung→Việt | v2.0 |
| **Auto-scheduling/Cron Jobs** | Cần thêm infrastructure | v1.1 |
| **Payment/Revenue Tracking** | Cần integration với TYT API | v2.0 |
| **Team Collaboration** | Multi-user với roles chi tiết | v2.0 |

---

## 3. NGƯỜI DÙNG & VAI TRÒ

### 3.1 Actors

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                              ACTORS                                          │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│   ┌─────────────────┐                                                       │
│   │    Operator     │  ← Người vận hành chính                               │
│   │                 │    • Tạo story mới                                    │
│   │  [HUMAN]        │    • Monitor tiến độ                                  │
│   │                 │    • Xử lý lỗi khi có                                 │
│   └────────┬────────┘    • Review/edit content thủ công                     │
│            │                                                               │
│   ┌────────▼────────┐                                                       │
│   │   Admin         │  ← Quản trị hệ thống                                 │
│   │                 │    • Quản lý tài khoản TYT/YouTube                   │
│   │  [HUMAN]        │    • Cấu hình Gemini model                           │
│   │                 │    • Xem reports & analytics                          │
│   └────────┬────────┘    • System settings                                  │
│            │                                                               │
│   ┌────────▼────────┐                                                       │
│   │   System       │  ← Backend services (background)                      │
│   │                 │    • CrawlWikidich job                                │
│   │  [AUTOMATED]   │    • TranslateGemini job                             │
│   │                 │    • UploadTYT job                                   │
│   └─────────────────┘                                                       │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 3.2 User Personas

| Persona | Vai trò | Nhu cầu chính |
|---------|---------|---------------|
| **Minh - Operator** | Vận hành content | Tạo story nhanh, theo dõi tiến độ, xử lý lỗi đơn giản |
| **Hùng - Admin** | Quản trị hệ thống | Quản lý accounts, config settings, xem analytics |
| **Hệ thống** | Background jobs | Tự động hóa, logging, retry khi lỗi |

---

## 4. FUNCTIONAL REQUIREMENTS

### FR-1: Tạo Story từ Wikidich URL

**ID:** FR-1  
**Priority:** P0 (Critical)  
**Module:** Story Management

**Mô tả:**
User nhập URL truyện từ Wikidich, chọn tài khoản TYT và YouTube, hệ thống tự động crawl thông tin, dịch nội dung và tạo story mới trong database.

**Input:**
| Field | Type | Required | Validation |
|-------|------|----------|------------|
| Wikidich URL | string | Yes | Valid URL format, must be from wikidich domain |
| TYT Account ID | int | Yes | Must exist, must be active |
| YouTube Account ID | int | No | Must exist if provided |
| Auto-translate | bool | No | Default: true |
| Auto-upload | bool | No | Default: true |

**Output:**
| Field | Type | Mô tả |
|-------|------|-------|
| StoryId | int | ID của story vừa tạo |
| Status | enum | Created, Processing, Completed, Failed |
| Message | string | Thông báo thành công hoặc lỗi |
| ChaptersCount | int | Số chapter đã tạo |

**Business Rules:**
1. URL phải thuộc domain wikidich.com hoặc wikidich.net
2. TYT Account phải có cookie hợp lệ (chưa expired)
3. Nếu crawl fail → retry 3 lần với exponential backoff (2s, 4s, 8s)
4. Translation dùng `gemini-2.5-flash` làm default
5. Story được tạo với trạng thái "Processing"
6. User được redirect đến trang chi tiết story sau khi tạo

**User Flow:**
```
[User paste URL] → [Select TYT Account] → [Select YouTube (optional)] 
    → [Click Save] → [System: Crawl] → [System: Translate] → [Done]
```

**Acceptance Criteria:**
- [ ] User nhập URL hợp lệ → hiển thị loading spinner
- [ ] URL không hợp lệ → hiển thị validation error
- [ ] Crawl thành công → hiển thị thông tin đã parse (title, author, chapters)
- [ ] Translation thành công → story được tạo, chapters có ContentEdit
- [ ] Crawl fail sau 3 retry → hiển thị error message cụ thể
- [ ] TYT Account không hợp lệ → hiển thị error trước khi crawl

---

### FR-2: Crawl Nội dung từ Wikidich

**ID:** FR-2  
**Priority:** P0 (Critical)  
**Module:** Wikidich Crawler

**Mô tả:**
Hệ thống tự động truy cập trang Wikidich, parse HTML để lấy thông tin truyện và nội dung các chapters.

**Crawl Data Points:**

| Data Point | Source | Field Mapping |
|------------|--------|--------------|
| Title | h1 tag | Story.TitleRaw |
| Author | author meta/link | Story.Author |
| Total Chapters | chapter count section | Story.TotalChapters |
| Cover Image | img tag | Story.ImageUrl |
| Description | description section | Story.DescriptionRaw |
| Genres | genre tags | Story.Genres |
| Chapter 1 Link | "Chương 1" button | Story.LinkChapterOne |
| Chapter Content | chapter pages | Chapter.ContentRaw |
| Chapter Name | chapter title | Chapter.NameRaw |
| Chapter Number | parsed from URL/title | Chapter.ChapterNumber |

**Business Rules:**
1. Respect robots.txt của Wikidich
2. Rate limit: tối thiểu 2 giây giữa mỗi request
3. Retry failed requests 3 lần
4. Parse tất cả chapters từ 1 đến TotalChapters
5. Lưu trữ raw HTML content để debug nếu cần
6. Image URL được convert thành absolute URL

**Error Handling:**
| Error Type | Action |
|------------|--------|
| 404 Not Found | Mark story as Failed, message: "Truyện không tồn tại" |
| 403 Forbidden | Mark story as Failed, message: "Wikidich chặn truy cập" |
| 429 Rate Limited | Wait 60s, retry |
| Timeout (>30s) | Retry 3 lần |
| Parse Error | Save raw data, skip field, log warning |

**Acceptance Criteria:**
- [ ] Valid URL → tất cả data points được crawl thành công
- [ ] Invalid URL → clear error message
- [ ] 100 chapters → crawl đầy đủ, không skip
- [ ] Network error → retry và eventual success hoặc clear error

---

### FR-3: Dịch Nội dung bằng AI (Gemini)

**ID:** FR-3  
**Priority:** P0 (Critical)  
**Module:** Translation

**Mô tả:**
Hệ thống tự động dịch nội dung từ dạng word-by-word (Wikidich) sang tiếng Việt tự nhiên, dễ đọc bằng Google Gemini.

**Translation Configurations:**

| Config | Type | Default | Mô tả |
|--------|------|---------|-------|
| Model | enum | gemini-2.5-flash | Model sử dụng |
| BatchSize | int | 5 | Số chapters translate cùng lúc |
| MaxRetries | int | 3 | Số lần retry khi fail |
| Temperature | float | 0.7 | Creativity level (0-1) |
| Language | string | Vietnamese | Ngôn ngữ đích |

**Gemini Models:**

| Model | Use Case | Cost | Speed |
|-------|----------|------|-------|
| gemini-2.5-pro | Chapter quan trọng, cần chất lượng cao | High | Slow |
| gemini-2.5-flash | Chapter thường, bulk translation | Low | Fast |

**Translation Modes:**

| Mode | Description | Trigger |
|------|-------------|---------|
| **Auto** | Translate không cần review | Default, khi auto-translate = true |
| **Manual** | Translate + human review | Khi user uncheck auto-translate |

**Business Rules:**
1. Prompt template được config trong system settings
2. Context: gửi 3-5 chapters trước đó để maintain consistency
3. Character limit per request: 30,000 characters
4. Chapters > 30k chars → split thành multiple requests
5. Track translation cost (tokens) per story
6. Multi-API-key failover: nếu key A fail/quota hết → tự động dùng key B

**Prompt Template:**
```
# Role
Bạn là một dịch giả chuyên nghiệp chuyên dịch truyện tiếng Trung sang tiếng Việt.

# Rules
1. Dịch tự nhiên, giữ nguyên ý nghĩa gốc
2. Không dịch theo nghĩa đen từng từ
3. Sửa câu cho tự nhiên trong tiếng Việt
4. Giữ nguyên tên nhân vật, địa danh
5. Giữ format: xuống dòng, đoạn văn

# Input
Chương {chapter_number}: {chapter_name}

{nội dung cần dịch}

# Output
Chương {chapter_number}: {chapter_name}

{nội dung đã dịch}
```

**Acceptance Criteria:**
- [ ] Word-by-word text → dịch tự nhiên, có ý nghĩa
- [ ] Tên nhân vật → giữ nguyên (hoặc phiên âm nếu cần)
- [ ] 100 chapters → translate đầy đủ, không skip
- [ ] API error → retry với exponential backoff
- [ ] Quota exceeded → failover sang API key khác

---

### FR-4: Quản lý Tài khoản TYT

**ID:** FR-4  
**Priority:** P0 (Critical)  
**Module:** Account Management

**Mô tả:**
Quản lý các tài khoản TYT dùng để đăng truyện, bao gồm cookie authentication và trạng thái hoạt động.

**TYT Account Entity:**

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| Id | int | Auto | Primary key |
| Name | string | Yes | Tên hiển thị (VD: "Account MinMin") |
| Cookie | string | Yes | Cookie đăng nhập TYT |
| CookieExpiredAt | DateTime | Yes | Thời điểm cookie hết hạn |
| IsActive | bool | Yes | Trạng thái hoạt động |
| CreatedAt | DateTime | Auto | Thời điểm tạo |
| CreatedBy | string | Auto | User tạo |

**Features:**

| Feature | Description |
|---------|-------------|
| **Add Account** | Thêm tài khoản mới với cookie |
| **Edit Account** | Cập nhật cookie hoặc tên |
| **Delete Account** | Xóa tài khoản (soft delete) |
| **Validate Cookie** | Kiểm tra cookie còn hạn bằng cách test login |
| **Auto-expiry Warning** | Alert 3 ngày trước khi cookie hết hạn |
| **Cookie Refresh** | Update cookie mới khi cũ hết hạn |

**Business Rules:**
1. Cookie phải được lưu encrypted trong database
2. CookieExpiredAt = CreatedAt + 30 ngày (default) hoặc user specified
3. System tự động validate cookie mỗi khi sử dụng
4. Nếu cookie invalid → mark IsActive = false, alert user
5. Không cho phép delete account đang được sử dụng (in-use check)

**Acceptance Criteria:**
- [ ] Thêm account mới → validate cookie success → account active
- [ ] Cookie hết hạn → tự động mark inactive + notification
- [ ] Edit cookie → validate mới → update thành công
- [ ] Delete account đang in-use → reject với message

---

### FR-5: Quản lý Tài khoản YouTube

**ID:** FR-5  
**Priority:** P1 (Important)  
**Module:** Account Management

**Mô tả:**
Quản lý các tài khoản YouTube để phục vụ việc upload video (v1.0: lưu thông tin, v2.0: auto-upload).

**YouTube Account Entity:**

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| Id | int | Auto | Primary key |
| Name | string | Yes | Tên kênh |
| ChannelId | string | Yes | YouTube Channel ID |
| ClientId | string | Yes | OAuth Client ID |
| ClientSecret | string | Yes | OAuth Client Secret |
| RefreshToken | string | Yes | OAuth Refresh Token |
| IsActive | bool | Yes | Trạng thái hoạt động |
| CreatedAt | DateTime | Auto | Thời điểm tạo |

**Note:** Trong v1.0, YouTube account chỉ được lưu để track. Auto-upload video sẽ implement trong v2.0.

**Acceptance Criteria:**
- [ ] Thêm account mới → OAuth flow → save tokens
- [ ] Token expired → auto-refresh → update
- [ ] View accounts → list all accounts with status

---

### FR-6: Upload Story lên TYT

**ID:** FR-6  
**Priority:** P0 (Critical)  
**Module:** TYT Publisher

**Mô tả:**
Hệ thống tự động đăng story và các chapters lên nền tảng TYT sử dụng cookie authentication.

**Upload Steps:**

```
1. Login TYT (validate cookie)
2. Create Story trên TYT (upload info)
3. Upload Cover Image
4. Create Chapter 1
5. Create Chapter 2... (loop)
6. Mark story as Published
```

**Business Rules:**
1. Upload tuần tự, không parallel để tránh rate limit
2. Delay 5 giây giữa mỗi chapter upload
3. Nếu story upload fail → rollback (delete story if created)
4. Lưu TYT Story ID và TYT Chapter ID vào database
5. Set Publish status = Private mặc định (user tự đổi sang Public sau)

**TYT API Integration:**

| Action | Method | Endpoint Pattern |
|--------|--------|-----------------|
| Validate Cookie | POST | /api/auth/login |
| Create Story | POST | /api/story/create |
| Upload Image | POST | /api/story/upload-image |
| Create Chapter | POST | /api/chapter/create |
| Update Chapter | PUT | /api/chapter/{id} |

**Error Handling:**
| Error | Action |
|-------|--------|
| Cookie invalid | Alert user, stop upload |
| Story creation failed | Retry 3x, then fail整个 story |
| Chapter creation failed | Retry 3x, skip to next chapter |
| Rate limited (429) | Wait 60s, retry |

**Acceptance Criteria:**
- [ ] Single chapter story → upload thành công
- [ ] 100 chapter story → upload đầy đủ, progress bar
- [ ] Cookie expired mid-upload → stop, alert user, allow resume
- [ ] Network error → auto retry với backoff

---

### FR-7: Dashboard & Theo dõi Tiến độ

**ID:** FR-7  
**Priority:** P1 (Important)  
**Module:** Dashboard

**Mô tả:**
Giao diện dashboard hiển thị tổng quan hệ thống, danh sách stories với trạng thái, và chi tiết tiến độ xử lý.

**Dashboard Metrics:**

| Metric | Description |
|--------|-------------|
| Total Stories | Tổng số story trong hệ thống |
| Processing | Số story đang được xử lý |
| Completed | Số story đã hoàn thành |
| Failed | Số story bị lỗi |
| Total Chapters | Tổng số chapters |
| Chapters Pending | Chapters chưa translate |
| Chapters Done | Chapters đã translate |

**Story List View:**

| Column | Description |
|--------|-------------|
| Title | Tên truyện |
| Author | Tác giả |
| Chapters | Progress (VD: "45/100") |
| Status | Processing/Completed/Failed |
| TYT Status | Draft/Published/Private |
| Created | Ngày tạo |
| Actions | View/Edit/Delete |

**Story Detail View:**
- Thông tin cơ bản (title, author, description)
- Danh sách chapters với status (Raw/Translated/Uploaded)
- Progress bar (translated/total, uploaded/total)
- Log tiến độ (timestamps)
- Actions: Translate All, Upload All, Edit

**Acceptance Criteria:**
- [ ] Dashboard → hiển thị metrics real-time
- [ ] Story list → filter by status, sort by date
- [ ] Story detail → xem chi tiết từng chapter
- [ ] Progress → update tự động khi có thay đổi

---

### FR-8: Authentication & Authorization

**ID:** FR-8  
**Priority:** P0 (Critical)  
**Module:** Security

**Mô tả:**
Hệ thống authentication sử dụng JWT, phân quyền operator và admin.

**Roles:**

| Role | Permissions |
|------|------------|
| **Admin** | Full access: manage accounts, system config, all CRUD |
| **Operator** | Create/Edit/Delete stories, view dashboard |
| **Viewer** | View only (read-only access) |

**Authentication Flow:**
```
[Login] → [Validate credentials] → [Generate JWT] → [Return token]
[API Request] → [Validate JWT] → [Check Role] → [Allow/Deny]
```

**Business Rules:**
1. JWT token expires sau 1 giờ (configurable)
2. Refresh token expires sau 7 ngày
3. Password phải >= 8 chars, có uppercase, lowercase, number
4. Sau 5 lần login fail → lock account 15 phút
5. Log all authentication attempts

**Acceptance Criteria:**
- [ ] Login success → nhận JWT token
- [ ] Invalid credentials → clear error message
- [ ] Expired token → auto redirect to login
- [ ] Unauthorized access → 403 Forbidden

---

## 5. NON-FUNCTIONAL REQUIREMENTS

### 5.1 Performance

| Requirement | Target | Measurement |
|-------------|--------|-------------|
| Page load time | < 2s | Dashboard, Story list |
| API response time | < 500ms | CRUD operations |
| Crawl speed | 2-3 chapters/min | With rate limiting |
| Translation throughput | 10-20 chapters/min | Gemini 2.5-flash |
| Concurrent users | 10 users | Simultaneous operators |

### 5.2 Security

| Requirement | Implementation |
|-------------|----------------|
| Data at rest | Cookie, tokens encrypted (AES-256) |
| Data in transit | HTTPS only |
| Authentication | JWT with RS256 |
| Authorization | Role-based (RBAC) |
| Audit logging | All CRUD operations logged |
| Password policy | Min 8 chars, complexity requirements |

### 5.3 Reliability

| Requirement | Target |
|-------------|--------|
| Uptime | 99.5% |
| Error rate | < 1% for critical flows |
| Data backup | Daily automatic backup |
| Recovery time | < 4 hours |

### 5.4 Scalability

| Requirement | Design |
|-------------|--------|
| Horizontal scaling | Stateless API, can scale out |
| Database | PostgreSQL, supports partitioning |
| Message queue | Kafka for async jobs |

---

## 6. DATA REQUIREMENTS

### 6.1 Entity Relationship Diagram

```
┌─────────────────┐       ┌─────────────────┐       ┌─────────────────┐
│     Account     │       │      Story      │       │     Chapter     │
├─────────────────┤       ├─────────────────┤       ├─────────────────┤
│ Id (PK)         │       │ Id (PK)         │       │ Id (PK)         │
│ AccountType     │◀──────│ TytAccountId   │       │ StoryId (FK)    │
│ Name            │       │ YoutubeAcctId   │       │ ChapterTytId    │
│ Cookie          │       │ TitleRaw        │◀──────│ NameRaw         │
│ IsActive        │       │ TitleEdit       │       │ NameEdit        │
│ ExpiredAt       │       │ Author          │       │ ChapterNumber   │
│ CreatedAt       │       │ TotalChapters   │       │ ContentRaw      │
│ CreatedBy       │       │ ImageUrl        │       │ ContentEdit     │
└─────────────────┘       │ DescriptionRaw  │       │ IsUploaded      │
                          │ DescriptionEdit │       │ CreatedAt       │
                          │ LinkChapterOne  │       │ UpdatedAt       │
                          │ Genres (JSON)   │       └─────────────────┘
                          │ IsScraped       │
                          │ IsEdited        │
                          │ Publish (enum)  │
                          │ TytStoryId      │
                          │ CreatedAt       │
                          │ UpdatedAt       │
                          └─────────────────┘
```

### 6.2 New Tables

```sql
-- TYT & YouTube Accounts
CREATE TABLE Accounts (
    Id SERIAL PRIMARY KEY,
    AccountType INT NOT NULL, -- 1=TYT, 2=YouTube
    Name VARCHAR(100) NOT NULL,
    Cookie TEXT, -- TYT: login cookie
    ChannelId VARCHAR(100), -- YouTube: channel ID
    ClientId VARCHAR(500), -- YouTube OAuth
    ClientSecret VARCHAR(500), -- YouTube OAuth
    RefreshToken TEXT, -- YouTube OAuth
    IsActive BOOLEAN DEFAULT true,
    ExpiredAt TIMESTAMP,
    CreatedAt TIMESTAMP DEFAULT NOW(),
    CreatedBy VARCHAR(100),
    UpdatedAt TIMESTAMP DEFAULT NOW()
);

-- Stories enhancement
ALTER TABLE Stories ADD COLUMN TytAccountId INT REFERENCES Accounts(Id);
ALTER TABLE Stories ADD COLUMN YoutubeAccountId INT REFERENCES Accounts(Id);
ALTER TABLE Stories ADD COLUMN TytStoryId VARCHAR(100);
ALTER TABLE Stories ADD COLUMN ProcessingStatus INT DEFAULT 0; -- 0=Pending, 1=Processing, 2=Completed, 3=Failed

-- Chapters enhancement
ALTER TABLE Chapters ADD COLUMN ChapterTytId VARCHAR(100);
ALTER TABLE Chapters ADD COLUMN TranslateStatus INT DEFAULT 0; -- 0=Pending, 1=Done
ALTER TABLE Chapters ADD COLUMN TranslateError TEXT;
ALTER TABLE Chapters ADD COLUMN TranslatedAt TIMESTAMP;
ALTER TABLE Chapters ADD COLUMN UploadedAt TIMESTAMP;
```

### 6.3 Enum Definitions

```csharp
// AccountType
public enum AccountType
{
    Tyt = 1,
    YouTube = 2
}

// ProcessingStatus
public enum ProcessingStatus
{
    Pending = 0,
    Processing = 1,
    Completed = 2,
    Failed = 3
}

// Publish
public enum Publish
{
    Private = 0,
    Public = 1
}
```

---

## 7. API REQUIREMENTS

### 7.1 Stories API

#### POST /api/stories
**Create Story from Wikidich URL**

Request:
```json
{
    "wikidichUrl": "https://wikidich.com/truyen/dao-hanh-vo-thuong-hai",
    "tytAccountId": 1,
    "youtubeAccountId": 2,
    "autoTranslate": true,
    "autoUpload": true
}
```

Response (201 Created):
```json
{
    "id": 123,
    "status": "Processing",
    "title": "Đạo Hành Vô Thượng Hải",
    "totalChapters": 250,
    "message": "Story đang được xử lý"
}
```

#### GET /api/stories
**Get all stories**

Query params: `?status=Processing&page=1&pageSize=20`

Response:
```json
{
    "items": [...],
    "total": 150,
    "page": 1,
    "pageSize": 20
}
```

#### GET /api/stories/{id}
**Get story detail with chapters**

Response:
```json
{
    "id": 123,
    "title": "Đạo Hành Vô Thượng Hải",
    "status": "Processing",
    "progress": {
        "translated": 45,
        "total": 250,
        "percentage": 18
    },
    "chapters": [
        {
            "id": 1,
            "number": 1,
            "name": "Chương 1",
            "status": "Uploaded",
            "tytId": "ch-12345"
        }
    ]
}
```

### 7.2 Chapters API

#### POST /api/stories/{storyId}/chapters/{chapterId}/translate
**Translate single chapter**

Response:
```json
{
    "id": 1,
    "status": "Translated",
    "translatedAt": "2026-08-23T10:30:00Z"
}
```

#### POST /api/stories/{storyId}/translate-all
**Translate all pending chapters**

Response:
```json
{
    "queued": 200,
    "message": "Đã thêm vào queue"
}
```

### 7.3 Accounts API

#### GET /api/accounts
**Get all accounts**

Response:
```json
{
    "tytAccounts": [
        {
            "id": 1,
            "name": "Account MinMin",
            "isActive": true,
            "expiredAt": "2026-09-23"
        }
    ],
    "youtubeAccounts": [...]
}
```

#### POST /api/accounts
**Create new account**

Request (TYT):
```json
{
    "accountType": 1,
    "name": "Account MinMin",
    "cookie": "session=abc123...",
    "expireDays": 30
}
```

#### PUT /api/accounts/{id}/validate
**Validate account cookie**

Response:
```json
{
    "valid": true,
    "expiresAt": "2026-09-23"
}
```

---

## 8. ACCEPTANCE CRITERIA

### 8.1 FR-1: Create Story

| # | Criteria | Test Scenario |
|---|----------|---------------|
| AC-1.1 | User có thể nhập Wikidich URL | Input URL → no validation error |
| AC-1.2 | URL validation hoạt động | Input non-wikidich URL → error message |
| AC-1.3 | Account selection required | Submit without account → validation error |
| AC-1.4 | Story created successfully | Valid input → story in DB |
| AC-1.5 | Error handling | Invalid URL → clear error, no crash |

### 8.2 FR-2: Crawl

| # | Criteria | Test Scenario |
|---|----------|---------------|
| AC-2.1 | All data points extracted | Crawl → all fields populated |
| AC-2.2 | All chapters discovered | TotalChapters matches actual |
| AC-2.3 | Retry on failure | Simulate 404 → retry, eventual success |
| AC-2.4 | Rate limiting respected | 100 chapters → no 429 errors |

### 8.3 FR-3: Translation

| # | Criteria | Test Scenario |
|---|----------|---------------|
| AC-3.1 | Content translated naturally | Compare raw vs translated |
| AC-3.2 | No character limit issues | 50000 char chapter → fully translated |
| AC-3.3 | API failover works | Simulate quota exceeded → uses backup key |
| AC-3.4 | Cost tracking | Every translation → logged in DB |

### 8.4 FR-4: TYT Account

| # | Criteria | Test Scenario |
|---|----------|---------------|
| AC-4.1 | Cookie stored encrypted | DB query → cookie not readable |
| AC-4.2 | Expiry warning | 3 days before → alert shown |
| AC-4.3 | Invalid cookie detected | Expired cookie → marked inactive |

### 8.5 FR-6: Upload to TYT

| # | Criteria | Test Scenario |
|---|----------|---------------|
| AC-6.1 | Story created on TYT | Upload → TytStoryId populated |
| AC-6.2 | All chapters uploaded | 100 chapters → 100 chapters on TYT |
| AC-6.3 | Resume after failure | Stop mid-upload → resume from last |
| AC-6.4 | Progress tracked | Upload in progress → progress bar updates |

### 8.6 Definition of Done (DOD)

Một User Story được coi là **Done** khi:

- [ ] Code implemented đúng FR requirements
- [ ] Unit tests viết và pass (coverage > 80%)
- [ ] Integration tests pass (nếu có)
- [ ] Code reviewed bởi teammate
- [ ] Acceptance criteria met
- [ ] Documentation updated
- [ ] Deployed to staging environment
- [ ] User acceptance signed off

---

## Appendix A: Glossary

| Term | Definition |
|------|------------|
| Wikidich | Nguồn truyện Trung→Việt word-by-word |
| TYT | Nền tảng đăng truyện đích (tytnovel.info) |
| Word-by-word | Dịch máy theo thứ tự từ gốc, không tự nhiên |
| Processing | Story đang trong quá trình crawl/translate/upload |

## Appendix B: References

- Project README: `README.md`
- User Flow: `USER_FLOW.md`
- Database Schema: Entity files trong `src/Domain/Entities/`

---

*Document End*
