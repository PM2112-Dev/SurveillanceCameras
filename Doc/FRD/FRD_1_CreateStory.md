### FR-1: Create Story

**Mô tả:** User chọn nền tảng Crawl (vd: wikidich, tyt,...) nhập URL, chọn tài khoản tyt, youtube -> hệ thống tự động crawl, dịch và tạo story

**Input:**
- Foundation: wikidich, tyt,... (required)
- URL Foundation (required)
- TYT Account ID
- YouTube Account ID

**Output:**
- Story ID mới được tạo
- Trạng thái: "Processing" → "Completed" hoặc "Failed"

**Business Rules:**
- URL phải hợp lệ và tồn tại
- TYT Account phải active và có cookie hợp lệ
- Nếu crawl fail → retry 3 lần, sau đó báo lỗi
- Translation dùng Gemini 2.5-flash mặc định

**Acceptance Criteria:**
- [ ] User nhập URL → hiển thị loading
- [ ] Crawl thành công → hiển thị thông tin story đã crawl
- [ ] Translation thành công → story được tạo với chapters
- [ ] Có lỗi → hiển thị thông báo lỗi cụ thể