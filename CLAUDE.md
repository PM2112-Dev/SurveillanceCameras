# Memory

## Me
Đang xây dựng hệ thống quản lý truyện tự động (crawl → dịch AI → đăng nền tảng → sản xuất video → đa kênh phân phối).

## Terms
| Term | Meaning |
|------|---------|
| Wikidich | Nguồn crawl truyện — bản dịch Trung→Việt kiểu word-by-word (dịch máy thô, khó đọc), cần dịch lại bằng AI cho tự nhiên |
| TYT | Nền tảng đăng truyện đích, hiện tại https://tytnovel.info/. Domain có thể đổi → cần chức năng cập nhật domain trong hệ thống, không hard-code |
| word-by-word translation | Kiểu dịch máy dịch từng từ theo thứ tự gốc tiếng Trung, giữ nguyên văn phạm Trung — người Việt đọc khó hiểu, là lý do cần bước dịch AI lại |

## Projects
| Name | What |
|------|------|
| **Hệ thống quản lý truyện tự động** | Xem [[story-management-system]] cho chi tiết đầy đủ về scope, kiến trúc, và các module |

## Preferences
- Dịch truyện dùng Gemini, cần quản lý nhiều model Gemini (chọn model theo tác vụ) và quản lý nhiều API key với cơ chế failover khi quá tải/hết quota
- TTS (giọng đọc audio) chưa chốt nhà cung cấp — thiết kế hệ thống theo hướng dễ đổi/thêm provider TTS (interface trừu tượng, không hard-code 1 nhà cung cấp)
- TYT domain có thể đổi — không hard-code domain, cần cấu hình/update được domain trong hệ thống
