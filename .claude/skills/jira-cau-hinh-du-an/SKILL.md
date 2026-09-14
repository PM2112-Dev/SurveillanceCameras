---
name: jira-cau-hinh-du-an
description: Thiết lập lần đầu file cấu hình cho pipeline "Jira → AI hoàn thành task" — kết nối Jira, kết nối GitHub, quy tắc lọc ticket, quy mô đội agent nghiên cứu/phát triển. Dùng khi khởi tạo dự án hoặc khi cần sửa lại cấu hình đang có.
---

# Thiết lập cấu hình dự án cho pipeline Jira → AI

## Mục đích

Skill này tạo/cập nhật **một file cấu hình duy nhất** để 5 skill còn lại trong họ `jira-*` đọc và tuân theo. Đây là bước **chạy trước tiên**, thường chỉ 1 lần khi khởi tạo dự án, hoặc mỗi khi cần đổi thông số (đổi assignee, đổi repo, đổi số lượng agent...).

Không skill nào khác được phép tự đoán giá trị cấu hình — nếu thiếu, phải quay lại chạy skill này trước.

## Vị trí file

- File mẫu (có comment giải thích từng field): [`config.example.yml`](./config.example.yml)
- File cấu hình thật của dự án: `.claude/jira-ai-pipeline/config.yml` (tạo mới bằng cách copy từ file mẫu)
- Giải thích chi tiết từng field: [`references/schema-cau-hinh.md`](./references/schema-cau-hinh.md)

## Các bước thực hiện

1. **Kiểm tra file đã tồn tại chưa**: đọc `.claude/jira-ai-pipeline/config.yml`.
   - Nếu đã có → hiển thị lại nội dung hiện tại cho người dùng, hỏi họ muốn sửa phần nào (đừng ghi đè toàn bộ nếu không cần).
   - Nếu chưa có → copy từ `config.example.yml` sang `config.yml` làm điểm bắt đầu.

2. **Thu thập thông tin Jira**:
   - `jira.ket_noi.site`, `jira.ket_noi.project_key` — hỏi trực tiếp người dùng nếu chưa biết.
   - `jira.nguon_lay_ticket.loai` — hỏi người dùng muốn lấy theo `jql` (tự viết), `backlog`, hay `sprint_hien_tai`. **Lưu ý báo trước cho người dùng**: hệ thống dùng JQL để mô phỏng backlog/sprint vì bộ Atlassian MCP hiện tại không có API Board/Agile riêng — nếu người dùng cần chính xác theo 1 board cụ thể, khuyên dùng `jql_tuy_chinh` với điều kiện lọc rõ ràng.
   - `jira.bo_loc.assignee_account_id` — nếu người dùng chỉ biết tên/email, dùng tool `lookupJiraAccountId` để tra accountId, không tự bịa.
   - `jira.bo_loc.bat_buoc_start_date` và `field_start_date` — nếu người dùng không dùng field Start Date, đặt `bat_buoc_start_date: false`.
   - `jira.bo_loc.trang_thai_lay` — danh sách trạng thái hợp lệ để lấy xử lý (thường là cột "To Do"/"Sẵn sàng" trên board).
   - `jira.labels_phan_loai` — tên label thật đang dùng trong project cho phạm vi BE/FE/cả hai. Nếu project chưa có các label này, đề nghị người dùng tạo trước trên Jira (skill không tự tạo label mới).
   - `jira.chuyen_trang_thai` — map đúng tên trạng thái có thật trong workflow của project (dùng `getTransitionsForJiraIssue` trên 1 ticket mẫu để xác nhận tên transition hợp lệ, tránh cấu hình sai tên rồi lúc chạy thật mới phát hiện).
   - `jira.phan_hoi` — mặc định bật hết (comment + gắn PR); hỏi người dùng nếu muốn tắt bớt.

3. **Thu thập thông tin GitHub**:
   - `github.repo`, `github.nhanh_goc` — xác nhận bằng `gh repo view` nếu đang đứng trong đúng repo.
   - `github.tu_dong_tao_nhanh_moi`, `tu_dong_commit`, `tu_dong_push`, `tu_dong_tao_pull_request` — hỏi rõ từng cờ, đừng gộp chung thành 1 câu hỏi "có tự động không" vì đây là 4 quyết định độc lập theo đúng yêu cầu ban đầu của người dùng.
   - `github.pull_request.tu_dong_merge` — **mặc định luôn `false`**. Chỉ đặt `true` nếu người dùng xác nhận rõ ràng bằng lời, và nên nhắc lại rủi ro trước khi ghi giá trị này.

4. **Thu thập cấu hình đội Nghiên cứu và đội Phát triển**:
   - `nhom_nghien_cuu.so_luong_agent` + `vai_tro` — tối thiểu 2 agent (1 đề xuất, 1 phản biện) để việc "đánh giá chéo" có ý nghĩa.
   - `nhom_phat_trien.so_luong_agent` + `phan_cong` — mặc định 2 (1 backend, 1 frontend); nếu dự án của người dùng chỉ có BE hoặc chỉ có FE thì giảm còn 1 và bỏ phần còn lại trong `phan_cong`.
   - `kiem_thu.lenh_build` / `lenh_test` — xác nhận đúng lệnh build/test thật của project (repo này hiện dùng `dotnet build` / `dotnet test`; nếu FE có test riêng thì bổ sung thêm dòng lệnh `npm test` v.v.).

5. **Ghi file** `config.yml` với toàn bộ giá trị đã xác nhận, giữ nguyên các comment giải thích từ file mẫu (không xoá comment khi ghi lại).

6. **Validate trước khi kết thúc**:
   - Báo lỗi rõ ràng nếu thiếu field bắt buộc: `jira.ket_noi.site`, `jira.ket_noi.project_key`, `jira.bo_loc.assignee_account_id`, `github.repo`.
   - Nếu `jira.nguon_lay_ticket.loai = "jql"` mà `jql_tuy_chinh` để trống → báo lỗi, không cho qua.
   - In ra bản tóm tắt cấu hình vừa lưu để người dùng xác nhận lần cuối.

## Không được làm

- Không tự chạy pipeline thật (lấy ticket, code, tạo PR) trong skill này — skill này **chỉ cấu hình**.
- Không tự đoán `assignee_account_id`, tên label, hay tên trạng thái Jira nếu chưa xác nhận qua tool — sai tên transition sẽ khiến toàn bộ pipeline sau này thất bại ở bước cuối.
- Không đặt `github.pull_request.tu_dong_merge = true` mà không hỏi lại người dùng.
