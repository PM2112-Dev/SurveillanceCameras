# Giải thích chi tiết schema cấu hình

File tham chiếu này giải thích từng field trong `config.yml` (xem file mẫu đầy đủ tại [`../config.example.yml`](../config.example.yml)). Dùng khi cần tra cứu ý nghĩa 1 field cụ thể, không cần đọc hết mỗi lần chạy skill chính.

## `jira.ket_noi`

| Field | Kiểu | Bắt buộc | Ý nghĩa |
|---|---|---|---|
| `site` | string | Có | Domain Jira, ví dụ `yourteam.atlassian.net`. Dùng làm `cloudId` khi gọi Atlassian MCP. |
| `project_key` | string | Có | Mã project Jira (viết hoa), ví dụ `PROJ`. Mọi JQL sinh tự động đều bắt đầu bằng `project = <project_key>`. |

## `jira.nguon_lay_ticket`

| Field | Kiểu | Ý nghĩa |
|---|---|---|
| `loai` | enum: `jql` \| `backlog` \| `sprint_hien_tai` | Cách xác định tập ticket ứng viên ban đầu, trước khi áp `bo_loc`. |
| `jql_tuy_chinh` | string | Chỉ dùng khi `loai = jql`. Là câu JQL đầy đủ, tự chịu trách nhiệm đúng cú pháp. |

**Vì sao không có "board" thật sự?** Bộ Atlassian MCP hiện có trong phiên làm việc chỉ có tool tìm kiếm theo JQL (`searchJiraIssuesUsingJql`) và các tool đọc/sửa issue đơn lẻ — không có tool gọi Agile API (board/backlog theo `boardId`). Vì vậy `backlog` và `sprint_hien_tai` chỉ là JQL định sẵn mang tính xấp xỉ. Nếu về sau có thêm tool Agile thật, cập nhật lại 2 nhánh này trong skill `jira-lay-va-loc-ticket`.

## `jira.bo_loc`

| Field | Kiểu | Ý nghĩa |
|---|---|---|
| `bat_buoc_start_date` | bool | `true` = ticket phải có Start Date và Start Date ≤ hôm nay mới hợp lệ. `false` = bỏ qua bước kiểm tra này hoàn toàn. |
| `field_start_date` | string | ID custom field Jira chứa Start Date (dạng `customfield_XXXXX`). Tra bằng `getJiraIssueTypeMetaWithFields` nếu chưa biết. |
| `assignee_account_id` | string | AccountId (không phải email/tên hiển thị) của người mà pipeline chỉ nhận task được assign cho họ. |
| `trang_thai_lay` | list\<string\> | Danh sách tên status hợp lệ để bắt đầu xử lý. Phải khớp chính xác tên hiển thị trên Jira (phân biệt hoa/thường theo Jira). |

## `jira.labels_phan_loai`

| Field | Ý nghĩa |
|---|---|
| `be` | Tên label đánh dấu ticket chỉ cần sửa Backend. |
| `fe` | Tên label đánh dấu ticket chỉ cần sửa Frontend. |
| `ca_hai` | (tuỳ chọn) tên label riêng cho "cả hai". Nếu để trống, quy tắc suy luận là: ticket có **cả** label `be` và `fe` → coi là `BOTH`. |

Xem thuật toán áp dụng đầy đủ trong skill [`jira-lay-va-loc-ticket`](../../jira-lay-va-loc-ticket/SKILL.md).

## `jira.chuyen_trang_thai`

Mỗi giá trị phải là **tên trạng thái đích** có thật trong workflow Jira của project (không phải tên transition). Trước khi lưu, nên xác nhận bằng `getTransitionsForJiraIssue` trên 1 ticket mẫu để chắc chắn tồn tại đường transition từ trạng thái hiện tại tới trạng thái đích — Jira workflow có thể không cho phép nhảy thẳng giữa 2 trạng thái bất kỳ.

| Field | Thời điểm áp dụng |
|---|---|
| `khi_bat_dau` | Ngay sau khi ticket qua hết `bo_loc`, trước khi đội nghiên cứu bắt đầu. |
| `khi_cho_review` | Ngay sau khi PR được mở (skill `jira-hoan-tat-quy-trinh`). |
| `khi_hoan_thanh` | Sau khi PR merge (nếu pipeline có theo dõi merge) hoặc ngay sau khi tạo PR nếu `tu_dong_merge = true`. |
| `khi_bi_chan` | Bất kỳ lúc nào pipeline dừng vì lỗi hoặc vượt số vòng thử lại tối đa. |

## `github`

| Field | Ý nghĩa |
|---|---|
| `repo` | `owner/ten-repo`, dùng cho mọi lệnh `gh`. |
| `nhanh_goc` | Branch nền để tạo nhánh mới và để `gh pr create --base`. |
| `tu_dong_tao_nhanh_moi` | `false` = làm việc thẳng trên nhánh hiện tại (rủi ro cao hơn, chỉ nên dùng khi test skill). |
| `mau_ten_nhanh` | Placeholder hỗ trợ: `{issue_key}`, `{slug}` (tóm tắt không dấu, cách nhau bằng `-`). |
| `mau_commit` | Placeholder hỗ trợ: `{issue_key}`, `{tom_tat}`. |
| `pull_request.mau_tieu_de` / `mau_noi_dung` | Placeholder hỗ trợ thêm: `{duong_dan_issue}`, `{tom_tat_ai}`, `{ke_hoach_test}`. |
| `pull_request.tu_dong_merge` | **Mặc định `false`.** Chỉ bật khi người dùng xác nhận rõ ràng — xem cảnh báo trong `SKILL.md` chính. |

## `nhom_nghien_cuu`

| Field | Ý nghĩa |
|---|---|
| `so_luong_agent` | Số agent chạy song song ở giai đoạn nghiên cứu. Tối thiểu 2 để có phản biện chéo thật sự. |
| `vai_tro[].ten` / `trong_tam` | Mỗi agent được giao 1 góc nhìn khác nhau (nghiệp vụ / kỹ thuật / phản biện...) — tránh 2 agent trùng góc nhìn vì sẽ ra kết quả gần giống nhau, lãng phí vòng phản biện. |
| `so_vong_phan_bien` | Số vòng các agent đọc chéo đề xuất của nhau và góp ý trước khi tổng hợp phương án cuối. |

## `nhom_phat_trien`

| Field | Ý nghĩa |
|---|---|
| `so_luong_agent` | Số agent phát triển. Skill điều phối sẽ tự giảm số này nếu `pham_vi_xu_ly` của ticket chỉ là BE hoặc chỉ là FE (không spawn agent cho phía không liên quan). |
| `phan_cong[].pham_vi` | Danh sách glob pattern xác định agent đó được đọc/sửa những đường dẫn nào — dùng để tránh 2 agent dẫm chân nhau khi chạy song song. |
| `chay_lai_toi_da` | Số lần 1 agent tự sửa code của chính mình khi build/test cục bộ thất bại, trước khi báo lỗi lên cho skill điều phối. |

## `kiem_thu`

| Field | Ý nghĩa |
|---|---|
| `lenh_build` / `lenh_test` | Danh sách lệnh chạy tuần tự; dừng ngay khi 1 lệnh fail. |
| `chay_generate_api_khi_doi_be` | `true` = sau khi agent backend đổi bất kỳ endpoint/contract nào, tự động chạy `npm run generate-api` (NSwag) trong `src/Web/ClientApp` trước khi agent frontend bắt đầu code — bắt buộc `true` nếu ticket có phạm vi `BOTH`. |
| `chay_lai_toi_da` | Số vòng quay lại đội phát triển để sửa khi test vẫn fail sau khi build đã pass. |
