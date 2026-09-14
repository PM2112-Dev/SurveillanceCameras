---
name: jira-dieu-phoi-toan-trinh
description: Orchestrator nối toàn bộ pipeline "Jira → AI hoàn thành task" từ đầu đến cuối — gọi tuần tự các skill lọc ticket, nghiên cứu, phát triển, hoàn tất. Dùng khi người dùng gõ lệnh xử lý 1 ticket cụ thể, hoặc khi chạy polling nền tự động lấy ticket mới.
---

# Điều phối toàn trình (Orchestrator)

## Mục đích

Đây là **điểm vào duy nhất** người dùng (hoặc tác vụ lập lịch) cần gọi để chạy cả pipeline. Skill này không tự làm việc chi tiết — nó gọi đúng 4 skill con theo thứ tự, xử lý điều kiện dừng sớm, và đảm bảo trạng thái Jira luôn phản ánh đúng tình trạng thật.

**Điều kiện tiên quyết**: đã có `.claude/jira-ai-pipeline/config.yml` hợp lệ. Nếu chưa có, dừng ngay và hướng dẫn chạy [`jira-cau-hinh-du-an`](../jira-cau-hinh-du-an/SKILL.md) trước.

## Hai cách kích hoạt

### 1. Lệnh thủ công — có issue key cụ thể
Người dùng gọi kèm 1 issue key, ví dụ "xử lý PROJ-123". Bỏ qua bước tìm ticket tiếp theo, chạy thẳng `jira-lay-va-loc-ticket` ở **chế độ thủ công** với issue key đó.

### 2. Polling tự động — không có issue key
Được gọi định kỳ (qua scheduled task cấu hình sẵn theo `trigger.scheduled.cron`, xem ghi chú thiết lập lịch ở cuối file). Chạy `jira-lay-va-loc-ticket` ở **chế độ polling**: tự tìm ticket đầu tiên qua hết bộ lọc và chưa có marker "đang xử lý". Nếu không có ticket nào phù hợp, kết thúc êm (không phải lỗi).

Tôn trọng `trigger.scheduled.max_concurrent_tasks`: nếu đã có ticket khác đang được xử lý (còn marker "đang xử lý" nhưng chưa "đã xong"/"bị chặn"), không lấy thêm ticket mới cho tới khi dưới ngưỡng.

## Luồng chạy tuần tự

```
[1] jira-lay-va-loc-ticket
      │  không có ticket phù hợp → dừng êm, không báo lỗi
      ▼  có ticket → tiếp tục
[2] jira-doi-nghien-cuu
      │  không chốt được phương án (bất đồng lớn, cần người quyết) → dừng, comment hỏi người dùng, transition sang "khi_bi_chan"
      ▼  có tài liệu giải pháp → tiếp tục
[3] jira-doi-phat-trien
      │  build/test fail vượt quá số lần cho phép → dừng, comment lỗi, transition sang "khi_bi_chan"
      ▼  code pass build/test → tiếp tục
[4] jira-hoan-tat-quy-trinh
      →  Git (theo 4 cờ độc lập) + cập nhật Jira (theo kết quả thật)
```

Mỗi bước chỉ được gọi khi bước trước **thành công rõ ràng**. Không có chuyện "cứ chạy hết 4 bước rồi mới kiểm tra lỗi" — dừng ngay tại bước fail.

## Xử lý lỗi & escalate

- Bất kỳ bước nào dừng vì lỗi: đảm bảo Jira đã được comment lý do cụ thể và transition sang `jira.chuyen_trang_thai.khi_bi_chan` **trước khi** kết thúc orchestrator (không để ticket treo ở trạng thái "In Progress" mà không ai biết vì sao dừng).
- Không tự động thử lại toàn bộ pipeline từ đầu khi 1 bước fail — số lần thử lại chỉ áp dụng **trong nội bộ** từng skill con (VD trong `jira-doi-phat-trien`) theo đúng cấu hình của skill đó.
- Nếu đang ở chế độ polling và 1 ticket bị chặn, **không dừng cả job polling** — vẫn kết thúc lượt chạy hiện tại bình thường; lượt polling kế tiếp sẽ không lấy lại ticket này nữa (đã có marker) trừ khi người dùng gỡ marker thủ công.

## Ghi chú thiết lập polling

Việc lập lịch chạy định kỳ (cron) nằm ngoài phạm vi bản thân skill này — dùng cơ chế lập lịch sẵn có (`scheduled-tasks` MCP hoặc `/schedule`) để gọi lại `/jira-dieu-phoi-toan-trinh` (không kèm issue key) theo đúng `trigger.scheduled.cron` trong config. Skill này không tự đăng ký lịch cho chính nó.

## Không được làm

- Không tự viết lại logic lọc/nghiên cứu/phát triển/git ngay trong skill này — luôn gọi qua skill con tương ứng bằng tool `Skill`, để giữ mỗi phần trách nhiệm ở đúng 1 nơi.
- Không bỏ qua bước cập nhật trạng thái "bị chặn" khi dừng giữa chừng do lỗi thật sự.
- Không lấy thêm ticket mới khi đã đạt `trigger.scheduled.max_concurrent_tasks`.
