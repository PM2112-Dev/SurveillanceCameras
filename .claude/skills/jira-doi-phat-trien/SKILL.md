---
name: jira-doi-phat-trien
description: Điều phối một đội AI agent phát triển (Backend/Frontend) hiện thực phương án đã được đội nghiên cứu chốt — đọc cấu trúc code hiện có để tái sử dụng, viết API/logic nghiệp vụ (BE) và UI theo template hệ thống (FE), rồi build/test tự sửa lỗi. Dùng sau jira-doi-nghien-cuu, trước jira-hoan-tat-quy-trinh.
---

# Đội phát triển (Developer Team)

## Mục đích

Hiện thực đúng phương án đã chốt ở bước nghiên cứu, **không tự ý đổi hướng giải pháp**. Nếu trong lúc code phát hiện phương án đã chọn không khả thi, phải dừng lại và báo rõ lý do (không tự sửa sang phương án khác).

**Điều kiện tiên quyết**: đã có tài liệu giải pháp từ `jira-doi-nghien-cuu` và `pham_vi_xu_ly` từ `jira-lay-va-loc-ticket`.

## Bước 1 — Xác định agent cần spawn theo phạm vi

Đọc `nhom_phat_trien.phan_cong` từ config, đối chiếu với `pham_vi_xu_ly` của ticket:

| `pham_vi_xu_ly` | Agent cần spawn |
|---|---|
| `BE` | chỉ agent `backend` |
| `FE` | chỉ agent `frontend` |
| `BOTH` | cả `backend` và `frontend`, **backend chạy xong và pass build trước**, sau đó mới chạy `frontend` (frontend cần OpenAPI contract mới nhất từ backend) |

Không spawn agent cho phía không liên quan tới ticket, kể cả khi config liệt kê nhiều hơn.

## Bước 2 — Chuẩn bị workspace cô lập

- Nếu `github.tu_dong_tao_nhanh_moi = true`: tạo nhánh mới từ `github.nhanh_goc` theo `github.mau_ten_nhanh` (thay `{issue_key}`, `{slug}`) **trước khi** agent bắt đầu sửa file — dùng `git worktree` nếu có thể để cô lập, tránh đụng nhánh đang làm việc khác của người dùng.
- Nếu `github.tu_dong_tao_nhanh_moi = false`: làm việc thẳng trên nhánh hiện tại, cảnh báo rõ trước khi bắt đầu.

## Bước 3 — Agent Backend

Trước khi viết code mới, agent **bắt buộc** đọc để hiểu pattern hiện có (Clean Architecture + CQRS/MediatR + Repository):

1. Đọc 1-2 feature tương tự gần nhất dưới `src/Application/Local/<Entity>/Commands/` và `Queries/` để nắm cấu trúc file (`<Verb><Entity>.cs` + `<Verb><Entity>CommandValidator.cs`), quy ước namespace, cách đăng ký trong `Application/DependencyInjection.cs`.
2. Kiểm tra `src/Application/Repositories/` xem đã có interface repository phù hợp chưa — **ưu tiên tái sử dụng method có sẵn**, chỉ thêm method mới vào interface khi thật sự cần, tránh tạo repository trùng chức năng.
3. Nếu cần entity/field mới: cập nhật `src/Domain/Entities/`, `src/Infrastructure/Data/Configurations/`, rồi tạo **migration mới bằng `dotnet ef migrations add`** — tuyệt đối không chỉnh sửa tay 1 migration đã tồn tại và đã áp dụng (xem bài học thực tế đã gặp trong dự án này: migration bị sửa tay sau khi đã chạy khiến DB lệch khỏi migration history).
4. Viết Command/Query + Validator (FluentValidation) + endpoint tương ứng trong `src/Web/Endpoints/`.
5. Build (`dotnet build`) trước khi báo hoàn thành phần BE.

## Bước 4 — Chạy generate-api nếu cần (chuyển giao BE → FE)

Nếu `kiem_thu.chay_generate_api_khi_doi_be = true` **và** BE có đổi OpenAPI contract (thêm/sửa endpoint, đổi DTO): chạy `npm run generate-api` trong `src/Web/ClientApp` (dùng NSwag) **trước khi** agent frontend bắt đầu, để client TypeScript luôn khớp API mới nhất.

## Bước 5 — Agent Frontend

1. Đọc flow xử lý tương tự đã có trong `src/Web/ClientApp/src/components/` để bám đúng cách tổ chức component hiện tại của hệ thống.
2. Thiết kế UI dùng đúng hệ thống hiện có: **Pico CSS** (`@picocss/pico`) cho style, icon dùng `lucide-react` — không tự ý thêm thư viện UI/CSS framework khác (Tailwind, MUI...) trừ khi người dùng yêu cầu rõ ràng.
3. Gọi API qua client sinh tự động ở `src/api/` (NSwag) — không viết `fetch`/`axios` thủ công song song với client đã generate.
4. Đảm bảo responsive (kiểm tra ở nhiều kích thước viewport nếu có thể chạy dev server để xem trực quan).

## Bước 6 — Build/Test và tự sửa lỗi

- Chạy tuần tự `kiem_thu.lenh_build` rồi `kiem_thu.lenh_test`.
- Nếu fail: agent tự sửa và chạy lại, tối đa `nhom_phat_trien.chay_lai_toi_da` (cho lỗi build) hoặc `kiem_thu.chay_lai_toi_da` (cho lỗi test) lần.
- Vượt quá số lần cho phép mà vẫn fail: **dừng lại, không cố sửa thêm**, báo cáo rõ lỗi cuối cùng và những gì đã thử, để skill điều phối chuyển ticket sang trạng thái bị chặn thay vì âm thầm mở PR với code lỗi.

## Đầu ra

- Code đã commit cục bộ (chưa push) trên nhánh làm việc, đã build/test pass.
- Bản tóm tắt thay đổi (dùng để điền `{tom_tat_ai}` khi tạo PR ở skill tiếp theo) và kế hoạch test thủ công gợi ý (`{ke_hoach_test}`).

## Không được làm

- Không tự đổi phương án đã chốt ở bước nghiên cứu khi gặp khó khăn — phải dừng và báo cáo.
- Không sửa tay 1 migration EF Core đã áp dụng — luôn tạo migration mới.
- Không thêm thư viện UI/CSS mới ngoài Pico CSS mà không hỏi người dùng.
- Không tự ý push hoặc tạo PR ở bước này — việc đó thuộc skill [`jira-hoan-tat-quy-trinh`](../jira-hoan-tat-quy-trinh/SKILL.md).
