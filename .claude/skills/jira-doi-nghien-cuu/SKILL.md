---
name: jira-doi-nghien-cuu
description: Điều phối một đội nhiều AI agent cùng nghiên cứu 1 ticket Jira — mỗi agent phân tích nghiệp vụ/kỹ thuật/rủi ro theo góc nhìn riêng, sau đó phản biện chéo nhiều vòng để chốt ra 1 phương án xử lý tối ưu (user flow + business logic rõ ràng cho cả BE và FE). Dùng sau khi đã có ticket qua bước lọc, trước khi giao cho đội phát triển.
---

# Đội nghiên cứu giải pháp (Research Team)

## Mục đích

Trước khi viết bất kỳ dòng code nào, một đội agent phải **cùng nghiên cứu và tranh luận** để chốt ra phương án xử lý tốt nhất — tránh việc 1 agent duy nhất chọn đại giải pháp đầu tiên nghĩ ra. Đầu ra của skill này là tài liệu bắt buộc phải có trước khi gọi `jira-doi-phat-trien`.

**Điều kiện tiên quyết**: đã có kết quả từ `jira-lay-va-loc-ticket` (issue_key, summary, description, `pham_vi_xu_ly`, acceptance_criteria nếu có).

## Bước 1 — Spawn đội agent nghiên cứu song song

Đọc `nhom_nghien_cuu.so_luong_agent` và `nhom_nghien_cuu.vai_tro` từ config. Dùng tool `Agent` (subagent_type phù hợp cho nghiên cứu/đọc code, ví dụ `general-purpose`), **gọi tất cả trong cùng 1 message** để chạy song song thật sự.

Mỗi agent nhận 1 prompt tự chứa đầy đủ, gồm:

- Toàn bộ nội dung ticket (summary, description, acceptance criteria).
- Góc nhìn được giao (`vai_tro[i].trong_tam`) — nhắc rõ agent CHỈ tập trung góc nhìn này, tránh lặp lại việc của agent khác.
- Yêu cầu output: 1 phương án xử lý gồm **User Flow** (các bước người dùng thực hiện), **Business Logic** (quy tắc nghiệp vụ, điều kiện, trường hợp biên), **Ảnh hưởng Backend** (entity/API nào cần thêm/sửa), **Ảnh hưởng Frontend** (màn hình/luồng nào cần thêm/sửa) — chỉ điền phần liên quan tới `pham_vi_xu_ly` của ticket.
- Được phép: đọc code hiện có trong repo (đặc biệt các feature tương tự dưới `src/Application/Local/<Entity>/`) để đề xuất bám sát pattern có sẵn; dùng `WebSearch`/`WebFetch` nếu cần tham khảo cách giải quyết tương tự bên ngoài.
- Không được: viết code thật, sửa file thật — giai đoạn này chỉ ra tài liệu.

## Bước 2 — Vòng phản biện chéo

Lặp lại đúng `nhom_nghien_cuu.so_vong_phan_bien` vòng. Mỗi vòng:

1. Thu thập output hiện tại của tất cả agent.
2. Với mỗi agent, gửi tiếp 1 lượt (dùng `SendMessage` để tiếp tục agent đã spawn, giữ ngữ cảnh) kèm **toàn bộ đề xuất của các agent khác** trong vòng trước, yêu cầu:
   - Chỉ ra điểm yếu/rủi ro/trường hợp bị bỏ sót trong đề xuất của agent khác (đúng vai trò `phan-bien` phải làm việc này gắt nhất).
   - Tự điều chỉnh lại đề xuất của chính mình nếu thấy góp ý của agent khác hợp lý.
3. Sau vòng cuối cùng, không phản biện thêm — chuyển sang Bước 3.

## Bước 3 — Tổng hợp phương án cuối cùng

Do người điều phối (không phải 1 trong các agent nghiên cứu, để tránh thiên vị) tổng hợp:

- Chọn phương án có nhiều đồng thuận nhất qua các vòng phản biện; nếu vẫn còn bất đồng lớn giữa các agent ở vòng cuối, nêu rõ 2 phương án còn lại và lý do, để người dùng quyết định thay vì tự chọn.
- Viết tài liệu giải pháp cuối cùng, **tái sử dụng đúng format** đã có sẵn trong repo — không bịa format mới:
  - Cấu trúc theo kiểu `FR-<n>` như trong [`../../../Doc/FRD/FRD_1_CreateStory.md`](../../../Doc/FRD/FRD_1_CreateStory.md) và [`../../../FRD_TEMPLATE.md`](../../../FRD_TEMPLATE.md) (Mô tả → Input → Output → Business Rules → Acceptance Criteria dạng checkbox).
  - Nếu cần mô tả entity/quan hệ dữ liệu mới, dùng đúng format trong [`../../../ENTITY_TEMPLATE.md`](../../../ENTITY_TEMPLATE.md) (bảng Field Details + Relationship Cards).
  - Nếu ticket có `pham_vi_xu_ly` gồm FE, bổ sung phần User Flow theo văn phong [`../../../USER_FLOW.md`](../../../USER_FLOW.md) đang dùng trong repo.

## Đầu ra

Một tài liệu Markdown (lưu tạm trong workspace của phiên làm việc, **chưa commit vào repo**) gồm:

- Tóm tắt phương án đã chọn.
- User Flow (nếu có FE).
- Business Logic / Business Rules.
- Danh sách file/entity/API dự kiến bị ảnh hưởng (Backend).
- Danh sách màn hình/component dự kiến bị ảnh hưởng (Frontend).
- Rủi ro đã được đội phản biện chỉ ra + cách giảm thiểu.

Tài liệu này là đầu vào bắt buộc cho skill [`jira-doi-phat-trien`](../jira-doi-phat-trien/SKILL.md).

## Không được làm

- Không để 1 agent duy nhất quyết định phương án cuối mà bỏ qua vòng phản biện.
- Không viết hoặc sửa code trong giai đoạn này.
- Không tự bịa ra format tài liệu mới khi repo đã có `FRD_TEMPLATE.md`/`ENTITY_TEMPLATE.md`/`USER_FLOW.md`.
