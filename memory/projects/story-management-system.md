---
name: story-management-system
description: Full scope of the automated novel/story management system — crawl, AI translate, publish to TYT, video/audio production, multi-channel distribution
metadata:
  type: project
---

# Hệ thống quản lý truyện tự động

## Mục tiêu chính

1. **Crawl + dịch AI**
   - Lấy dữ liệu truyện & chapter từ [[glossary|Wikidich]] (bản dịch word-by-word Trung→Việt, khó đọc)
   - Dịch lại bằng AI (Gemini) thành văn phong tự nhiên, dễ đọc
   - Đẩy truyện đã dịch lên nền tảng TYT qua API

2. **Tạo video truyện audio để đăng YouTube**
   - 2 nguồn nội dung:
     - Truyện do AI của hệ thống tự dịch
     - Truyện đã được dịch sẵn (lấy về từ nguồn khác, chỉ dùng để làm video, không qua bước dịch AI)

3. **Quản lý hệ thống**
   - Quản lý tài khoản, API key (nhiều key, nhiều provider)
   - Quản lý & tối ưu prompt dịch (kho prompt, có thể versioning/so sánh chất lượng)
   - Quản lý truyện trên hệ thống lẫn trên TYT (đồng bộ 2 chiều)
   - Lấy thông tin/thống kê từ TYT (lượt đọc, trạng thái đăng,...)

4. **Tiện ích làm video**
   - Tạo video theo lịch (schedule), video ngắn (shorts)
   - Quản lý giọng đọc (TTS — provider chưa chốt)
   - Nhạc nền, tạo hình ảnh minh họa, phụ đề (subtitle)

5. **Tích hợp mạng xã hội**
   - API YouTube, TikTok, Facebook — đẩy video và quản lý (lịch đăng, theo dõi hiệu suất)

6. **Mở rộng nguồn truyện**
   - Không chỉ Wikidich — hỗ trợ lấy truyện từ nhiều nguồn khác nhau

## Quyết định kỹ thuật đã chốt
- **AI dịch:** Google Gemini. Cần quản lý nhiều model (chọn model theo tác vụ/chi phí) và nhiều API key với cơ chế failover khi một key quá tải hoặc hết quota.
- **TYT domain:** https://tytnovel.info/, nhưng domain có thể đổi → cần chức năng cập nhật domain trong hệ thống (không hard-code).
- **TTS:** chưa chốt provider — thiết kế theo hướng abstraction để dễ đổi/thêm provider sau.

## Trạng thái
Giai đoạn khởi tạo (2026-08-07) — chưa có kiến trúc/code cụ thể, đang ở bước lên danh sách mục tiêu và task ban đầu (xem TASKS.md).
