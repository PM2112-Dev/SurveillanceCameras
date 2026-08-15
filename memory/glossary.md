---
name: glossary
description: Decoder ring for terms, platforms, and shorthand used in the story-management-system project
metadata:
  type: reference
---

# Glossary

| Term | Meaning |
|------|---------|
| Wikidich | Nguồn dữ liệu truyện đầu vào. Truyện dịch Trung→Việt kiểu word-by-word (máy dịch thô, giữ ngữ pháp Trung), khó đọc với người Việt. Hệ thống crawl truyện + chapter từ đây rồi dịch lại bằng AI. |
| TYT / TYT Novel | Nền tảng đích để đăng truyện đã dịch, qua API. Domain hiện tại: https://tytnovel.info/. Domain có thể đổi trong tương lai — hệ thống cần cấu hình domain được, không hard-code. |
| word-by-word translation | Kiểu dịch máy dịch từng từ theo đúng thứ tự câu gốc tiếng Trung — lý do người Việt đọc khó hiểu, là nguyên nhân cần bước dịch AI lại thành văn phong tự nhiên. |
| Gemini | Google Gemini — AI provider chính được chọn để dịch truyện. Cần quản lý nhiều model Gemini và nhiều API key với failover khi quá tải/hết quota. |
| TTS | Text-to-Speech, dùng để tạo audio cho video truyện. Provider chưa chốt — thiết kế hệ thống để dễ đổi/thêm provider. |

See [[story-management-system]] for full project scope.
