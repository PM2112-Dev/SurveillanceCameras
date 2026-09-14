# Template chuẩn cho 1 ticket Jira (dựa trên FRSTMTK-23)

Đây là bản tham chiếu đầy đủ, tổng quát hoá từ ticket mẫu **FRSTMTK-23 — "Tạo mới Web Source"** (project `FRSTMTK`, issuetype `Feature`, parent Epic `FRSTMTK-21 — Quản lý Web Source`). Mọi ticket do skill này tạo ra phải đi đúng cấu trúc này — **không thêm/bớt heading**, chỉ thay nội dung.

## Cấu trúc gốc (nguyên văn format của FRSTMTK-23)

```
**Use Case: <Verb> <Entity>.**

**Summary**: <mô tả 1 câu>

**Basic Flow:**

1. <bước 1>
2. <bước 2>
3. ...

**Alternative Flows:**

* Bước <n>: <điều kiện> → <xử lý thay thế>

**Input:**

* <Field>: <mô tả>

**Extension Points:**

* None

**Preconditions:**

* <điều kiện trước>

**Postconditions:**

* <điều kiện sau khi thành công>

**Business Rules:**

* <quy tắc 1>
* <quy tắc 2>

**API Contract**

Endpoint: <method> <route>

Description: <mô tả API>

Auth: <danh sách role, dùng đúng hằng số trong Roles.cs: Administrator, Manager, User>

Request Body:

| Field | Type | Required | Ghi chú |
| --- | --- | --- | --- |
| <Field> | <Type> | <Yes/No> | <ghi chú> |

Response lỗi (nếu có):

| Status | Error code | Khi nào |
| --- | --- | --- |
| <status> | <ERROR_CODE> | <điều kiện> |

Entity <Entity>: <link tài liệu entity — dbdiagram.io hoặc ENTITY_TEMPLATE.md, chỉ điền nếu người dùng cung cấp, không tự bịa link>
```

## Bảng ánh xạ theo từng loại thao tác

Skill chính [`../SKILL.md`](../SKILL.md) dùng bảng này để điền nội dung cụ thể cho từng loại ticket. `{Entity}` = tên entity (PascalCase, ví dụ `WebSource`), `{entity}` = camelCase, `{Fields}` = danh sách field do người dùng cung cấp.

| Thao tác | Use Case | HTTP | Request Body | Response đặc trưng | Precondition đặc trưng | Postcondition đặc trưng |
|---|---|---|---|---|---|---|
| **Create** | `Tạo mới {Entity}` | `POST /api/{Entity}` | Toàn bộ field ghi được (không gồm Id) | `400 USED_<FIELD>` nếu field unique bị trùng (nếu có field unique) | User đã đăng nhập | `{Entity}` mới xuất hiện trong danh sách |
| **Update** | `Cập nhật {Entity}` | `PUT /api/{Entity}/{id}` | `Id` (path) + field cho phép sửa | `404 NOT_FOUND` nếu Id không tồn tại; `400 USED_<FIELD>` nếu đổi sang giá trị trùng | `{Entity}` với Id đó đã tồn tại | Dữ liệu `{Entity}` được cập nhật đúng field đã gửi |
| **Delete** | `Xoá {Entity}` | `DELETE /api/{Entity}/{id}` | Không có body, chỉ `id` trên path | `404 NOT_FOUND` nếu Id không tồn tại | `{Entity}` với Id đó đã tồn tại | `{Entity}` không còn xuất hiện trong danh sách |
| **Get (danh sách)** | `Xem danh sách {Entity}` | `GET /api/{Entity}` | Không có body; query param phân trang nếu project đang dùng (kiểm tra pattern `PaginatedList` hiện có trước khi thêm) | Không có lỗi nghiệp vụ đặc biệt | User đã đăng nhập | Trả về danh sách `{Entity}` (có thể rỗng) |
| **GetById** | `Xem chi tiết {Entity} theo Id` | `GET /api/{Entity}/{id}` | Không có body | `404 NOT_FOUND` nếu Id không tồn tại | `{Entity}` với Id đó đã tồn tại | Trả về đúng 1 `{Entity}` |
| **GetBy\<Field\>** | `Tìm {Entity} theo {Field}` | `GET /api/{Entity}/by-{field}?{field}=<value>` | Không có body; query param `{field}` | `404 NOT_FOUND` nếu không có bản ghi khớp (hoặc trả mảng rỗng nếu field không unique — xác nhận với người dùng loại nào) | User đã đăng nhập | Trả về `{Entity}` khớp điều kiện `{field}` |

## Nguyên tắc khi điền

- **Input / Business Rules / Request Body** phải lấy đúng từ danh sách field người dùng cung cấp khi gọi skill — không tự bịa thêm field không được nhắc tới.
- **Auth** mặc định `Administrator, Manager, User` (giống FRSTMTK-23) trừ khi người dùng chỉ định khác — nếu chỉ định, dùng đúng tên hằng số trong `src/Domain/Constants/Roles.cs`, không tự đặt role mới.
- **Error code** đặt tên theo quy ước `USED_<FIELD_VIẾT_HOA>` (trùng dữ liệu) hoặc `NOT_FOUND` (không tồn tại) — nhất quán với `USED_TITLE` đã dùng trong FRSTMTK-23.
- **Entity <Entity>: <link>** — chỉ điền dòng này nếu người dùng đưa link thật (VD link dbdiagram.io của entity đó); nếu không có, bỏ hẳn dòng này thay vì để trống hoặc bịa link.
