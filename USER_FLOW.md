# Hệ thống Quản lý Truyện Tự động - User Flow

## Phần 1: Operator Flow (Luồng Vận hành)

### 1.1 Discovery - Tìm Truyện Mới

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           DISCOVERY PHASE                                   │
└─────────────────────────────────────────────────────────────────────────────┘

  ┌──────────┐    ┌──────────────┐    ┌─────────────┐    ┌──────────────┐
  │ Operator │───▶│ Tìm truyện   │───▶│ Copy link   │───▶│ Paste vào    │
  │          │    │ trên         │    │ Wikidich    │    │ hệ thống     │
  │          │    │ Wikidich     │    │             │    │              │
  └──────────┘    └──────────────┘    └─────────────┘    └──────────────┘
                                                                   │
                                                                   ▼
                                                           ┌──────────────┐
                                                           │ System:      │
                                                           │ Crawl thông  │
                                                           │ tin truyện   │
                                                           └──────────────┘
```

**Chi tiết bước:**
1. Operator tìm truyện mới trên Wikidich (truyện Trung dịch sang Việt dạng word-by-word)
2. Copy URL của trang truyện
3. Paste vào field "Link Raw" trong hệ thống
4. Hệ thống tự động crawl và parse thông tin:
   - Title, Author, Total Chapters, Image, Description, Genres
   - Link tới Chapter 1

### 1.2 Review - Duyệt Thông Tin

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           REVIEW PHASE                                      │
└─────────────────────────────────────────────────────────────────────────────┘

  ┌──────────────┐    ┌─────────────┐    ┌─────────────┐    ┌───────────┐
  │ Xem thông    │───▶│ Edit Title  │───▶│ Edit        │───▶│ Select    │
  │ tin đã       │    │ (nếu cần)   │    │ Description │    │ Genres    │
  │ crawl        │    │             │    │ (nếu cần)   │    │           │
  └──────────────┘    └─────────────┘    └─────────────┘    └───────────┘
                                                                    │
                                                                    ▼
  ┌───────────┐    ┌─────────────┐    ┌─────────────┐    ┌──────────────┐
  │ Done      │◀───│ Confirm &   │◀───│ Upload      │◀───│ Check Image  │
  │           │    │ Save        │    │ Image       │    │ (validate)   │
  └───────────┘    └─────────────┘    └─────────────┘    └──────────────┘
```

**Chi tiết bước:**
1. Operator xem thông tin đã crawl, check độ chính xác
2. Edit Title nếu cần (tối ưu SEO)
3. Edit Description (viết lại cho tự nhiên hơn)
4. Select/confirm Genres phù hợp
5. Validate/Upload Image (cover truyện)
6. Confirm và lưu vào database

### 1.3 Translation - Dịch AI

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         TRANSLATION PHASE                                   │
└─────────────────────────────────────────────────────────────────────────────┘

  ┌──────────────┐    ┌─────────────┐    ┌─────────────┐    ┌───────────┐
  │ Select       │───▶│ Chọn        │───▶│ System:     │───▶│ Nhận      │
  │ Story để     │    │ translation │    │ Gọi Gemini  │    │ kết quả   │
  │ dịch         │    │ mode        │    │ API         │    │           │
  └──────────────┘    └─────────────┘    └─────────────┘    └───────────┘
                                                                    │
                                                                    ▼
  ┌──────────────┐    ┌─────────────┐    ┌─────────────┐    ┌───────────┐
  │ Save đã dịch │◀───│ Review &    │◀───│ Human       │◀───│ Output:   │
  │              │    │ Edit        │    │ Review      │    │ Content   │
  └──────────────┘    └─────────────┘    └─────────────┘    │ đã dịch   │
                                                            └───────────┘
```

**Translation Modes:**
| Mode | Mô tả | Khi nào dùng |
|------|-------|--------------|
| Full Auto | Dịch toàn bộ không cần review | Chapter đã dịch tốt |
| Auto + Review | Dịch + review nhanh | Chapter quan trọng |
| Human Edit | Dịch + edit kỹ | Chapter hay, cần chất lượng cao |

**Gemini Models có thể dùng:**
- `gemini-2.5-pro` - Chất lượng cao, chi phí cao (cho chapter quan trọng)
- `gemini-2.5-flash` - Nhanh, chi phí thấp (cho chapter thường)
- Multi-API-key failover khi quota hết

### 1.4 Upload - Đăng lên TYT

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           UPLOAD PHASE                                      │
└─────────────────────────────────────────────────────────────────────────────┘

  ┌──────────────┐    ┌─────────────┐    ┌─────────────┐    ┌───────────┐
  │ Select       │───▶│ Validate    │───▶│ Select      │───▶│ System:   │
  │ Chapters     │    │ Chapters    │    │ Account/    │    │ Login TYT │
  │ đã dịch      │    │ (check      │    │ Cookie      │    │ via       │
  │              │    │ empty)      │    │             │    │ Cookie    │
  └──────────────┘    └─────────────┘    └─────────────┘    └───────────┘
                                                                    │
                                                                    ▼
  ┌──────────────┐    ┌─────────────┐    ┌─────────────┐    ┌───────────┐
  │ Done!        │◀───│ Update      │◀───│ Upload      │◀───│ Create    │
  │ Chapter      │    │ DB status   │    │ content     │    │ Chapter   │
  │ đăng thành   │    │ (IsUploaded │    │ to TYT      │    │ on TYT    │
  │ công         │    │ = true)     │    │             │    │           │
  └──────────────┘    └─────────────┘    └─────────────┘    └───────────┘
```

**Chi tiết bước:**
1. Select các chapters đã dịch xong, chưa upload
2. Validate: kiểm tra content không rỗng, format đúng
3. Select TYT Account (hoặc dùng cookie đã lưu)
4. System tự động:
   - Login vào TYT qua cookie
   - Create chapter trên TYT
   - Update ChapterTytId
   - Set IsUploaded = true

### 1.5 Content Management - Quản lý Content

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                      CONTENT MANAGEMENT                                     │
└─────────────────────────────────────────────────────────────────────────────┘

  ┌──────────────────────────────────────────────────────────────────────────┐
  │                        STORY DASHBOARD                                  │
  │  ┌────────────┬────────────┬────────────┬────────────┬────────────┐  │
  │  │ Total      │ Uploaded   │ Pending    │ Earnings    │ Published  │  │
  │  │ Stories    │ Chapters   │ Chapters   │ (Ad revenue)│ Status     │  │
  │  │ 15         │ 1,234      │ 56         │ $1,234      │ Public/    │  │
  │  │            │            │            │            │ Private    │  │
  │  └────────────┴────────────┴────────────┴────────────┴────────────┘  │
  └──────────────────────────────────────────────────────────────────────────┘

  Story List View:
  ┌──────────────────────────────────────────────────────────────────────────┐
  │ [Title]        │ [Chapters] │ [Status]  │ [Actions]                      │
  ├──────────────────────────────────────────────────────────────────────────┤
  │ Đạo Hành Vô   │ 250/300    │ Editing   │ [Crawl] [Translate] [Upload]  │
  │ Thượng Hải    │            │           │ [Edit] [Delete]                │
  ├──────────────────────────────────────────────────────────────────────────┤
  │ Ma Đạo Tổ     │ 120/500    │ Pending   │ [Crawl] [Translate] [Upload]  │
  │ Thần Quân     │            │           │ [Edit] [Delete]                │
  └──────────────────────────────────────────────────────────────────────────┘
```

**Story Status Flow:**
```
  ┌─────────┐    ┌───────────┐    ┌──────────┐    ┌─────────┐    ┌─────────┐
  │ New     │───▶│ Scraped   │───▶│ Edited   │───▶│ Ready   │───▶│ Full    │
  │         │    │ (crawl    │    │ (dịch    │    │ (đăng   │    │ (hết    │
  │         │    │ xong)     │    │ xong)    │    │ vài ch.)│    │ truyện) │
  └─────────┘    └───────────┘    └──────────┘    └─────────┘    └─────────┘
```

### 1.6 Full Automated Flow

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    FULL AUTOMATED FLOW                                      │
│                    (Không cần human intervention)                           │
└─────────────────────────────────────────────────────────────────────────────┘

  ┌─────────┐    ┌──────────────┐    ┌──────────────┐    ┌───────────────┐
  │ Cron/   │───▶│ Find new     │───▶│ Auto Crawl   │───▶│ Auto Create   │
  │ Trigger │    │ stories on   │    │ story info   │    │ Story in DB   │
  │         │    │ Wikidich    │    │ from URL     │    │               │
  └─────────┘    └──────────────┘    └──────────────┘    └───────────────┘
                                                                    │
                                                                    ▼
  ┌──────────────┐    ┌──────────────┐    ┌──────────────┐    ┌────────────┐
  │ Auto         │◀───│ Queue       │◀───│ Translate    │◀───│ Select    │
  │ Publish to   │    │ chapters    │    │ all chapters │    │ chapters  │
  │ TYT          │    │ for         │    │ via Gemini   │    │ to        │
  │              │    │ translation  │    │              │    │ translate │
  └──────────────┘    └──────────────┘    └──────────────┘    └────────────┘
```

---

## Phần 2: System Architecture Flow

### 2.1 High-Level Architecture

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         SYSTEM ARCHITECTURE                                 │
└─────────────────────────────────────────────────────────────────────────────┘

                              ┌─────────────────┐
                              │   Frontend      │
                              │   (React/Web)   │
                              └────────┬────────┘
                                       │ HTTP/JWT
                                       ▼
  ┌─────────────────────────────────────────────────────────────────────────┐
  │                         BACKEND (.NET API)                              │
  │  ┌─────────────────────────────────────────────────────────────────┐   │
  │  │                    MediatR Pipeline                             │   │
  │  │  ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────┐          │   │
  │  │  │Validation│─▶Logging │─▶Auth     │─▶Perf     │─▶Handler │   │
  │  │  │Behaviour │  │Behaviour │  │Behaviour │  │Behaviour │          │   │
  │  │  └──────────┘ └──────────┘ └──────────┘ └──────────┘          │   │
  │  └─────────────────────────────────────────────────────────────────┘   │
  │                                                                         │
  │  ┌─────────────────────────────────────────────────────────────────┐   │
  │  │                    Commands / Queries                            │   │
  │  │  ┌────────────┐ ┌────────────┐ ┌────────────┐ ┌────────────┐     │   │
  │  │  │CreateStory │ │GetStories  │ │Translate  │ │UploadToTYT │     │   │
  │  │  └────────────┘ └────────────┘ └────────────┘ └────────────┘     │   │
  │  └─────────────────────────────────────────────────────────────────┘   │
  └─────────────────────────────────────────────────────────────────────────┘
              │                              │                    │
              ▼                              ▼                    ▼
  ┌───────────────────┐        ┌───────────────────┐   ┌───────────────────┐
  │  WikiDich Service │        │   Gemini Service  │   │    TYT Service    │
  │  (Crawl)          │        │   (Translation)   │   │    (Upload)       │
  └───────────────────┘        └───────────────────┘   └───────────────────┘
              │                              │                    │
              ▼                              ▼                    ▼
  ┌───────────────────┐        ┌───────────────────┐   ┌───────────────────┐
  │   Wikidich.com    │        │   Google Gemini   │   │    TYT Platform   │
  │   (External)      │        │   (External)      │   │    (External)     │
  └───────────────────┘        └───────────────────┘   └───────────────────┘
```

### 2.2 Data Flow - Crawl Story

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    DATA FLOW: CRAWL STORY                                  │
└─────────────────────────────────────────────────────────────────────────────┘

  ┌─────────┐         ┌─────────────┐         ┌─────────────────┐
  │ Frontend│         │ API         │         │ WikiDich        │
  │         │         │ Controller  │         │ Service         │
  └───┬─────┘         └──────┬──────┘         └────────┬────────┘
      │                       │                          │
      │ POST /stories         │                          │
      │ {linkRaw: "..."}      │                          │
      │──────────────────────▶│                          │
      │                       │                          │
      │                       │ FetchStory(link)         │
      │                       │────────────────────────▶│
      │                       │                          │
      │                       │    HTML Response        │
      │                       │◀────────────────────────│
      │                       │                          │
      │                       │ Parse HTML              │
      │                       │ (HtmlAgilityPack)       │
      │                       │                          │
      │                       │ Create Story entity     │
      │                       │ Save to PostgreSQL      │
      │                       │                          │
      │     201 Created       │                          │
      │     {id: 123}         │                          │
      │◀───────────────────────│                          │
      │                       │                          │
```

### 2.3 Data Flow - Translate Chapter

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                 DATA FLOW: TRANSLATE CHAPTER (GEMINI)                       │
└─────────────────────────────────────────────────────────────────────────────┘

  ┌─────────┐         ┌─────────────┐         ┌─────────────────┐
  │ Frontend│         │ Gemini      │         │ Google Gemini   │
  │         │         │ Service     │         │ API             │
  └───┬─────┘         └──────┬──────┘         └────────┬────────┘
      │                       │                          │
      │ POST /translate       │                          │
      │ {chapterId: 123}     │                          │
      │─────────────────────▶│                          │
      │                       │                          │
      │                       │ Get chapter content     │
      │                       │ (ContentRaw)           │
      │                       │                          │
      │                       │ Build prompt            │
      │                       │ (Context + content)     │
      │                       │                          │
      │                       │ Call Gemini API         │
      │                       │ (with model selection)  │
      │                       │────────────────────────▶│
      │                       │                          │
      │                       │    Translated text      │
      │                       │◀────────────────────────│
      │                       │                          │
      │                       │ Save ContentEdit       │
      │                       │ Update status          │
      │                       │                          │
      │     Success          │                          │
      │◀─────────────────────│                          │
      │                       │                          │
```

### 2.4 Data Flow - Upload to TYT

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    DATA FLOW: UPLOAD TO TYT                                │
└─────────────────────────────────────────────────────────────────────────────┘

  ┌─────────┐         ┌─────────────┐         ┌─────────────────┐
  │ Frontend│         │ TYT         │         │ TYT Platform    │
  │         │         │ Service     │         │ (tytnovel.info) │
  └───┬─────┘         └──────┬──────┘         └────────┬────────┘
      │                       │                          │
      │ POST /upload          │                          │
      │ {chapterId: 123}     │                          │
      │─────────────────────▶│                          │
      │                       │                          │
      │                       │ Get TYT account          │
      │                       │ (with cookie)           │
      │                       │                          │
      │                       │ Authenticate via cookie  │
      │                       │ (POST /login)           │
      │                       │────────────────────────▶│
      │                       │                          │
      │                       │    Session cookie       │
      │                       │◀────────────────────────│
      │                       │                          │
      │                       │ Create chapter          │
      │                       │ (POST /chapter/create)  │
      │                       │────────────────────────▶│
      │                       │                          │
      │                       │    Chapter ID           │
      │                       │◀────────────────────────│
      │                       │                          │
      │                       │ Update DB:              │
      │                       │ - ChapterTytId          │
      │                       │ - IsUploaded = true      │
      │                       │                          │
      │     Success          │                          │
      │◀─────────────────────│                          │
      │                       │                          │
```

### 2.5 Event-Driven Architecture (Kafka)

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    EVENT-DRIVEN FLOW (KAFKA)                               │
└─────────────────────────────────────────────────────────────────────────────┘

  ┌─────────────┐         ┌─────────────┐         ┌─────────────┐
  │ Story       │         │ Kafka       │         │ Worker      │
  │ Created     │         │ Topic       │         │ Services    │
  └───┬─────────┘         └──────┬──────┘         └──────┬──────┘
      │                          │                          │
      │ Publish event            │                          │
      │ "story.created"         │                          │
      │────────────────────────▶│                          │
      │                          │                          │
      │                          │ Consumer: CrawlWorker    │
      │                          │────────────────────────▶│
      │                          │                          │
      │                          │                          │ Crawl chapters
      │                          │                          │ from Wikidich
      │                          │                          │
      │                          │ Publish event            │
      │                          │◀────────────────────────│
      │                          │ "chapter.crawled"       │
      │                          │                          │
      │                          │ Consumer: TranslateWorker│
      │                          │────────────────────────▶│
      │                          │                          │
      │                          │                          │ Call Gemini
      │                          │                          │ Translate
      │                          │                          │
      │                          │ Publish event            │
      │                          │◨────────────────────────│
      │                          │ "chapter.translated"     │
      │                          │                          │
      │                          │ Consumer: UploadWorker   │
      │                          │────────────────────────▶│
      │                          │                          │
      │                          │                          │ Upload to TYT
      │                          │                          │
```

**Kafka Topics:**
| Topic | Trigger | Consumer |
|-------|---------|----------|
| `story.created` | Story mới được tạo | CrawlWorker |
| `chapter.crawled` | Chapter được crawl | TranslateWorker |
| `chapter.translated` | Chapter được dịch | UploadWorker |
| `chapter.uploaded` | Chapter được upload | NotificationWorker |

---

## Phần 3: Entity State Diagrams

### 3.1 Story State Machine

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         STORY STATE MACHINE                                │
└─────────────────────────────────────────────────────────────────────────────┘

                    ┌─────────────────────────────────────┐
                    │                                     │
                    ▼                                     │
  ┌─────────┐  ┌─────────┐  ┌──────────┐  ┌────────┐  ┌─────────┐
  │  New    │─▶│Scraping │─▶│ Scraped  │─▶│Editing │─▶│ Ready   │
  └─────────┘  └─────────┘  └──────────┘  └────────┘  └─────────┘
                                              │           │
                                              │           ▼
                                        ┌──────────┐  ┌─────────┐
                                        │ Translated─▶│ Full   │
                                        └──────────┘  └─────────┘
                                                            │
  ┌─────────────────────────────────────────────────────────┴──────────────┐
  │ FLAGS:                                                                  │
  │  IsScraped ───▶ TRUE khi đã crawl xong info                          │
  │  IsEdited ────▶ TRUE khi đã dịch xong content                        │
  │  IsFull ──────▶ TRUE khi tất cả chapters đã upload                    │
  │  Publish ─────▶ Public / Private (quyết định hiển thị trên TYT)      │
  └─────────────────────────────────────────────────────────────────────────┘
```

### 3.2 Chapter State Machine

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                       CHAPTER STATE MACHINE                                │
└─────────────────────────────────────────────────────────────────────────────┘

  ┌────────────┐    ┌────────────┐    ┌────────────┐    ┌────────────┐
  │   Raw      │───▶│ Translated │───▶│  Ready     │───▶│ Uploaded   │
  │ (from      │    │ (Gemini    │    │ (human     │    │ (on TYT)   │
  │  Wikidich) │    │  done)     │    │  review)   │    │            │
  └────────────┘    └────────────┘    └────────────┘    └────────────┘

  Fields:
  ┌──────────────────────────────────────────────────────────────────────────┐
  │ ContentRaw ──────▶ Nội dung word-by-word từ Wikidich                    │
  │ ContentEdit ─────▶ Nội dung đã dịch lại (tự nhiên)                      │
  │ ChapterTytId ────▶ ID của chapter trên TYT (sau khi upload)             │
  │ IsUploaded ──────▶ TRUE khi đã đăng lên TYT                            │
  └──────────────────────────────────────────────────────────────────────────┘
```

---

## Phần 4: API Endpoints Reference

### 4.1 Stories API

```
POST   /api/stories              - Tạo story mới (hoặc crawl từ link)
GET    /api/stories              - Lấy danh sách stories
GET    /api/stories/{id}         - Lấy chi tiết story
PUT    /api/stories/{id}         - Cập nhật story
DELETE /api/stories/{id}         - Xóa story
```

### 4.2 Chapters API (TODO)

```
POST   /api/stories/{id}/chapters        - Tạo chapter
GET    /api/stories/{id}/chapters        - Lấy danh sách chapters
GET    /api/chapters/{id}                - Lấy chi tiết chapter
PUT    /api/chapters/{id}                - Cập nhật chapter
DELETE /api/chapters/{id}                - Xóa chapter
```

### 4.3 Translation API (TODO)

```
POST   /api/chapters/{id}/translate      - Dịch chapter với Gemini
POST   /api/stories/{id}/translate-all   - Dịch tất cả chapters
```

### 4.4 Upload API (TODO)

```
POST   /api/chapters/{id}/upload         - Upload chapter lên TYT
POST   /api/stories/{id}/upload-pending - Upload tất cả chapters pending
```

### 4.5 Account API (TODO)

```
GET    /api/accounts                     - Lấy danh sách TYT accounts
POST   /api/accounts                     - Thêm TYT account
PUT    /api/accounts/{id}                - Cập nhật account
DELETE /api/accounts/{id}               - Xóa account
```

---

## Phần 5: Error Handling & Retry

### 5.1 Retry Strategy

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                          RETRY FLOW                                        │
└─────────────────────────────────────────────────────────────────────────────┘

  ┌────────────┐
  │   Start    │
  └─────┬──────┘
        │
        ▼
  ┌────────────┐      ┌────────────┐
  │  Execute   │─────▶│  Success?  │
  └────────────┘      └─────┬──────┘
                            │
               ┌────────────┼────────────┐
               │ Yes        │            │ No
               ▼            │            ▼
          ┌─────────┐       │     ┌────────────┐
          │ Done!   │       │     │ Retry      │
          └─────────┘       │     │ count < 3? │
                            │     └─────┬──────┘
                            │           │
                            │     ┌─────┴──────┐
                            │     │            │
                            │  Yes ▼        No ▼
                            │ ┌────────┐ ┌────────┐
                            │ │Wait 2s │ │ Fail   │
                            │ │Retry++ │ │ Log &  │
                            │ └────────┘ │ Alert  │
                            │            └────────┘
```

### 5.2 Error Types

| Error Type | Action | Retry? |
|------------|--------|--------|
| Network timeout | Retry with backoff | Yes (3x) |
| 429 Rate Limited | Wait & retry | Yes (exponential backoff) |
| 401 Unauthorized | Refresh cookie | Yes (1x) |
| 500 Server Error | Retry | Yes (3x) |
| Invalid content | Skip, alert | No |

---

## Phụ lục: File Locations

| Component | Path |
|-----------|------|
| Domain Entities | `src/Domain/Entities/` |
| Story Commands | `src/Application/Local/Stories/Commands/` |
| Story Queries | `src/Application/Local/Stories/Queries/` |
| WikiDich Service | `src/Infrastructure/Service/WikiDichService/` |
| API Endpoints | `src/Web/Endpoints/Stories.cs` |
| Flow Docs | `FLOW.md` |

---

*Document version: 1.0*
*Last updated: 2026-08-23*
