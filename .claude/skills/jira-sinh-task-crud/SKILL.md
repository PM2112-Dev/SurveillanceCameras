---
name: jira-sinh-task-crud
description: Sinh và tạo trên Jira một bộ ticket Create/Update/Delete/Get(danh sách)/GetById/GetBy<thuộc tính tuỳ chọn> cho 1 entity, theo đúng format của ticket mẫu FRSTMTK-23 (Use Case, Basic Flow, Business Rules, API Contract). Dùng khi cần breakdown 1 entity/module mới thành các task CRUD chi tiết trên Jira trước khi giao cho dev.
---

# Sinh bộ task CRUD trên Jira theo template

## Mục đích

Từ 1 entity (VD `Chapter`, `Account`...), tạo nhanh **nhiều ticket Jira cùng format** với ticket mẫu [FRSTMTK-23](https://pm2112dev.atlassian.net/browse/FRSTMTK-23) — mỗi ticket ứng với 1 thao tác: Create, Update, Delete, Get (danh sách), GetById, và tuỳ chọn thêm GetBy\<field\> cho từng field người dùng chỉ định. Xem bảng ánh xạ chi tiết + nguyên văn template tại [`references/mau-template-fr.md`](./references/mau-template-fr.md).

**Đây là skill tạo nội dung quản lý dự án (ticket Jira), không phải skill sinh code.** Việc sinh code CQRS (Command/Query C#) cho các thao tác này là một skill khác, làm sau — không trộn 2 việc trong skill này.

## Đầu vào cần thu thập trước khi tạo

Hỏi rõ người dùng (đừng tự đoán) nếu chưa có đủ:

1. **Project key** Jira (VD `FRSTMTK`) và **Epic cha** (VD `FRSTMTK-21`) — ticket mới sẽ có `parent` là Epic này, `issuetype = Feature` (đúng type của FRSTMTK-23).
2. **Tên Entity** (PascalCase, VD `WebSource`, `Chapter`).
3. **Danh sách field** của entity kèm: tên, kiểu dữ liệu, bắt buộc hay không, field nào là unique (để sinh đúng `Business Rules` và `USED_<FIELD>` error code). Không tự bịa field ngoài danh sách này.
4. **Danh sách thao tác cần tạo**: mặc định cả 6 (Create/Update/Delete/Get danh sách/GetById), cộng thêm **GetBy\<field\>** cho những field người dùng chỉ định rõ (không tự ý thêm GetBy cho field không được yêu cầu).
5. **Auth roles** áp dụng — mặc định `Administrator, Manager, User` như FRSTMTK-23; nếu người dùng muốn khác, dùng đúng hằng số có thật trong `src/Domain/Constants/Roles.cs` (không tự đặt role mới).
6. **Link tài liệu entity** (dbdiagram.io hoặc `ENTITY_TEMPLATE.md`) nếu có — dùng để điền dòng `Entity <Entity>: <link>` cuối mỗi ticket; nếu không có thì bỏ hẳn dòng này.

## Các bước thực hiện

1. **Soạn nội dung từng ticket trước, chưa gọi API tạo gì cả.** Với mỗi thao tác đã chọn ở bước trên, điền template theo đúng bảng ánh xạ trong [`references/mau-template-fr.md`](./references/mau-template-fr.md) (Use Case, Basic Flow, Alternative Flows, Input, Preconditions, Postconditions, Business Rules, API Contract).

2. **Hiển thị toàn bộ nội dung đã soạn cho người dùng xác nhận** trước khi tạo thật trên Jira — vì đây là hành động tạo nội dung nhìn thấy được bởi cả team, không tự ý tạo hàng loạt ticket khi chưa được đồng ý rõ ràng.

3. **Sau khi được xác nhận**, tạo từng ticket bằng `createJiraIssue`:
   - `projectKey` = project đã xác nhận.
   - `issueTypeName` = `"Feature"` (đúng type của FRSTMTK-23; nếu project không có type này, hỏi lại người dùng dùng type nào).
   - `summary` = đúng theo cột "Use Case" trong bảng ánh xạ (VD `Tạo mới WebSource`, `Cập nhật WebSource`, `Tìm WebSource theo Code`...).
   - `description` = toàn bộ nội dung đã soạn ở Bước 1 (giữ nguyên định dạng Markdown/heading in đậm như FRSTMTK-23).
   - `parent` = Epic key đã xác nhận.

4. **Liên kết ticket con vào đúng Epic** — nếu `createJiraIssue` với `parent` chưa tự liên kết đúng cách trong project này, dùng thêm `addTeamworkGraphContext` (`jira-work-item-links-jira-work-item`) hoặc `createIssueLink` để đảm bảo hiển thị đúng dưới Epic.

5. **Báo cáo kết quả**: liệt kê lại toàn bộ ticket vừa tạo kèm key + link (`webUrl`), để người dùng vào Jira kiểm tra ngay.

## Không được làm

- Không tự bịa thêm field, business rule, hay error code ngoài những gì người dùng đã cung cấp ở bước thu thập đầu vào.
- Không tự tạo ticket thật (`createJiraIssue`) trước khi người dùng xác nhận nội dung đã soạn.
- Không tự đổi `issuetype` sang loại khác `Feature` hoặc tự chọn Epic khác khi chưa hỏi lại.
- Không trộn việc sinh code CQRS vào skill này — đó là phạm vi của 1 skill sinh code riêng (làm sau, theo đúng yêu cầu của người dùng).
