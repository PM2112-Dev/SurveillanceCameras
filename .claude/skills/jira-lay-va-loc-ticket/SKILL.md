---
name: jira-lay-va-loc-ticket
description: Lấy danh sách ticket ứng viên từ Jira và lọc tuần tự theo Start Date, Assignee, Status, rồi xác định phạm vi xử lý (Backend/Frontend/cả hai) qua label. Dùng làm bước đầu tiên của pipeline, cả khi chạy thủ công lẫn khi polling tự động.
---

# Lấy & lọc ticket từ Jira

## Mục đích

Từ toàn bộ ticket trong project, chọn ra **đúng 1 ticket** (hoặc danh sách ticket, tuỳ chế độ gọi) thật sự sẵn sàng để AI xử lý, theo đúng thứ tự điều kiện người dùng đã mô tả — không được đảo thứ tự vì mỗi bước có chi phí gọi API khác nhau (nên loại sớm ở bước rẻ trước).

**Điều kiện tiên quyết**: đã có `.claude/jira-ai-pipeline/config.yml` hợp lệ (chạy skill `jira-cau-hinh-du-an` trước nếu chưa có).

## Hai chế độ gọi

1. **Chế độ thủ công** — được gọi với 1 issue key cụ thể (VD `PROJ-123`): vẫn chạy đủ 4 bước lọc bên dưới để đảm bảo nhất quán, nhưng chỉ trên 1 ticket đó. Nếu ticket không qua lọc, báo rõ lý do cho người dùng thay vì âm thầm bỏ qua.
2. **Chế độ polling** — không có issue key đầu vào: tự query danh sách ứng viên rồi lọc, trả về ticket đầu tiên đạt (ưu tiên theo `priority` giảm dần, nếu JQL không tự sắp xếp thì sort thủ công), bỏ qua các ticket đã có marker "đang xử lý" từ trước.

## Bước 1 — Lấy danh sách ticket ứng viên

Dựa vào `jira.nguon_lay_ticket.loai` trong config:

| `loai` | JQL sinh ra |
|---|---|
| `jql` | dùng thẳng `jira.nguon_lay_ticket.jql_tuy_chinh` |
| `backlog` | `project = <project_key> AND status = "Backlog" ORDER BY priority DESC` |
| `sprint_hien_tai` | `project = <project_key> AND sprint in openSprints() ORDER BY priority DESC` |

Gọi `searchJiraIssuesUsingJql` với JQL trên, `fields` nên gồm tối thiểu: `summary`, `status`, `assignee`, `labels`, `priority`, và field Start Date (`jira.bo_loc.field_start_date`).

## Bước 2 — Lọc tuần tự từng ticket (dừng ngay khi fail 1 điều kiện)

Với mỗi ticket ứng viên, áp dụng đúng thứ tự sau:

1. **Start Date hợp lệ** (chỉ kiểm tra nếu `bo_loc.bat_buoc_start_date = true`):
   - Ticket phải có giá trị ở field `bo_loc.field_start_date`.
   - Giá trị đó phải `<=` ngày hiện tại.
   - Không thoả → **bỏ qua ticket này**, không xét tiếp các bước sau.

2. **Assignee trùng khớp**:
   - So `assignee.accountId` của ticket với `jira.bo_loc.assignee_account_id`.
   - Không trùng → **bỏ qua**.

3. **Status nằm trong danh sách cho phép**:
   - So `status.name` của ticket với danh sách `jira.bo_loc.trang_thai_lay`.
   - Không nằm trong danh sách → **bỏ qua**.

4. **Xác định phạm vi xử lý qua label** (chỉ chạy khi ticket đã qua cả 3 bước trên):
   - Đọc `labels` của ticket.
   - Có label `labels_phan_loai.ca_hai` (nếu cấu hình có dùng field này), **hoặc** có đồng thời cả label `be` và `fe` → `pham_vi_xu_ly = BOTH`.
   - Chỉ có label `be` → `pham_vi_xu_ly = BE`.
   - Chỉ có label `fe` → `pham_vi_xu_ly = FE`.
   - Không có label nào trong 2 label `be`/`fe` → ticket **không xác định được phạm vi**: dừng lại, comment hỏi người phụ trách gắn label rồi mới xử lý tiếp (không tự đoán phạm vi).

## Bước 3 — Đánh dấu ticket đang được xử lý

Ngay khi 1 ticket qua hết Bước 2:

- Thêm label `trigger.idempotency.marker_label` (nếu có cấu hình, mặc định gợi ý `ai-in-progress`) bằng `editJiraIssue` để chế độ polling lần sau không lấy trùng ticket này.
- Nếu `jira.phan_hoi.comment_khi_bat_dau = true`, comment vào ticket bằng `addCommentToJiraIssue`, nêu rõ: đã nhận task, phạm vi xử lý xác định được (BE/FE/BOTH).
- Transition ticket sang `jira.chuyen_trang_thai.khi_bat_dau` bằng `transitionJiraIssue` (tra transition id hợp lệ qua `getTransitionsForJiraIssue` trước khi gọi).

## Đầu ra

Trả về cho skill gọi tiếp theo (`jira-doi-nghien-cuu`) một cấu trúc gồm:

```
{
  issue_key, issue_url, summary, description,
  pham_vi_xu_ly: "BE" | "FE" | "BOTH",
  acceptance_criteria (nếu field này tồn tại trong config),
  comments (nếu jira.bo_loc hoặc bước phân tích cần thêm ngữ cảnh)
}
```

## Không được làm

- Không đảo thứ tự 4 điều kiện lọc ở Bước 2 — thứ tự này phản ánh đúng mô tả nghiệp vụ gốc và giúp loại sớm ticket sai ở bước rẻ nhất (Start Date/Assignee) trước khi tốn thêm lệnh gọi.
- Không tự suy đoán `pham_vi_xu_ly` khi thiếu label rõ ràng.
- Không transition hoặc comment lên ticket **chưa qua hết** Bước 2 (tránh gây nhiễu Jira của người dùng với các ticket không liên quan).
