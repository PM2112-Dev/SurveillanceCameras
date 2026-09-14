---
name: jira-hoan-tat-quy-trinh
description: Bước cuối của pipeline Jira → AI — theo cấu hình GitHub để commit/push/tạo pull request, sau đó cập nhật trạng thái và comment lên Jira. Dùng ngay sau khi jira-doi-phat-trien báo code đã build/test pass.
---

# Hoàn tất quy trình (Git + cập nhật Jira)

## Mục đích

Chuyển kết quả code đã sẵn sàng (từ `jira-doi-phat-trien`) thành 1 pull request thật trên GitHub theo đúng mức độ tự động người dùng đã cấu hình, rồi phản ánh đúng trạng thái lên Jira. Đây là bước duy nhất được phép chạm vào Git remote và Jira transition/comment cho kết quả cuối.

**Điều kiện tiên quyết**: `jira-doi-phat-trien` đã báo thành công (build/test pass).

## Bước 1 — Git, theo đúng 4 cờ độc lập trong `github`

Thực hiện tuần tự, **mỗi cờ kiểm tra riêng** (đúng yêu cầu gốc: đây là 4 quyết định độc lập, không gộp):

1. `tu_dong_tao_nhanh_moi` — nếu `true` và chưa tạo ở bước phát triển, tạo nhánh theo `mau_ten_nhanh`. Nếu `false`, dùng nhánh hiện tại.
2. `tu_dong_commit` — nếu `true`, commit toàn bộ thay đổi với message theo `mau_commit`. Nếu `false`, dừng lại ở đây và báo cho người dùng tự commit.
3. `tu_dong_push` — nếu `true` (và đã commit ở bước 2), push nhánh lên remote. Nếu `false`, dừng lại, không tạo PR (không thể tạo PR từ nhánh chưa push).
4. `tu_dong_tao_pull_request` — nếu `true` (và đã push ở bước 3), tạo PR bằng `gh pr create`:
   - `--base` = `github.nhanh_goc`
   - `--title` = render `pull_request.mau_tieu_de`
   - `--body` = render `pull_request.mau_noi_dung` (điền `{duong_dan_issue}`, `{tom_tat_ai}`, `{ke_hoach_test}` từ đầu ra của `jira-doi-phat-trien`)
   - `--draft` nếu `pull_request.draft = true`
   - `--label` theo `pull_request.labels`, `--reviewer` theo `pull_request.reviewers` (nếu danh sách không rỗng)
   - Nếu `pull_request.tu_dong_merge = true`: **chỉ merge sau khi đã có PR mở thành công**, dùng `gh pr merge` — nhưng nhắc lại đây là cấu hình rủi ro cao, nên xác nhận lại với người dùng ngay cả khi config đã bật, trừ khi người dùng đã minh thị yêu cầu chạy hoàn toàn không giám sát.

Nếu bất kỳ cờ nào ở trên là `false`, dừng đúng tại đó — không "tự động hoá thêm" các bước sau nó.

## Bước 2 — Cập nhật Jira theo kết quả

- **PR được tạo thành công** (dù draft hay không):
  - `transitionJiraIssue` sang `jira.chuyen_trang_thai.khi_cho_review`.
  - Nếu `jira.phan_hoi.gan_link_pr = true`: gắn link PR vào issue (remote link/dev panel).
  - Nếu `jira.phan_hoi.comment_ket_qua = true`: comment tóm tắt thay đổi + link PR bằng `addCommentToJiraIssue`.
  - Nếu `pull_request.tu_dong_merge = true` và đã merge thành công: transition tiếp sang `jira.chuyen_trang_thai.khi_hoan_thanh`.

- **Dừng giữa chừng vì cờ config tắt** (VD `tu_dong_commit = false`): comment rõ lý do dừng (không phải lỗi, mà do cấu hình), **không** transition sang trạng thái bị chặn — đây là hành vi mong muốn theo cấu hình, không phải sự cố.

- **Có lỗi thật sự** (git push fail, `gh pr create` fail, v.v.): comment mô tả lỗi cụ thể, transition sang `jira.chuyen_trang_thai.khi_bi_chan`.

## Bước 3 — Thông báo người dùng

Dùng thông báo cuối phiên hoặc `PushNotification` (nếu người dùng đang không theo dõi trực tiếp) để báo: PR đã sẵn sàng ở đâu, cần review/merge thủ công (vì `tu_dong_merge` mặc định `false`).

## Không được làm

- Không bỏ qua thứ tự phụ thuộc giữa 4 cờ Git (VD không thể push khi chưa commit, không thể tạo PR khi chưa push) — nếu người dùng cấu hình mâu thuẫn (VD `tu_dong_push=false` nhưng `tu_dong_tao_pull_request=true`), báo lỗi cấu hình ngay từ đầu quy trình thay vì cố lách.
- Không tự merge PR nếu `tu_dong_merge` không phải `true` một cách tường minh trong config.
- Không transition Jira sang trạng thái "hoàn thành" chỉ vì đã tạo xong PR — trạng thái hoàn thành chỉ dành cho khi PR đã thực sự merge (hoặc auto-merge thành công).
