# Chạy hệ thống bằng Docker

Gồm các container: **web** (API + giao diện + Chrome), **db** (PostgreSQL 17), **kafka**, **kafka-init** (tạo topic rồi thoát), cùng 2 tuỳ chọn tunnel: **ngrok** (để test) và **cloudflared** (Cloudflare Tunnel cho production).

## Máy cũ: đóng gói

```bash
./docker/package.sh
```

Tạo file `docker/export/story-system-<ngày>.tar.gz` gồm mã nguồn, file `.env` (mật khẩu DB, khoá JWT, mật khẩu noVNC — tạo từ `docker/.env.example` nếu chưa có), **toàn bộ dữ liệu DB hiện tại** (`docker/initdb/01-data.sql`)
và ảnh đã lưu (`docker/media`). DB có API key AI, cookie tài khoản — giữ file này cẩn thận.

## Máy mới: chạy

Cần Docker (Docker Desktop hoặc Docker Engine + compose plugin), có Internet ở lần build đầu.

```bash
mkdir story-system && tar -xzf story-system-<ngày>.tar.gz -C story-system && cd story-system
docker compose up -d --build
```

- Giao diện: `http://<máy>:8080` — tài khoản có sẵn trong dữ liệu mang sang (máy trắng: `administrator@localhost` / `Administrator1!`).
- Lần đầu: DB trống thì nạp `docker/initdb/01-data.sql`, sau đó app tự chạy migration.
- Xem log: `docker compose logs -f web`

## Chạy qua Ngrok (Test) hoặc Cloudflare Tunnel (Production)

- **Chạy kèm Ngrok để test**:
  Điền `NGROK_AUTHTOKEN` và `NGROK_DOMAIN` trong `.env`, sau đó chạy:
  ```bash
  docker compose --profile ngrok up -d --build
  ```
- **Chạy kèm Cloudflare Tunnel cho Production**:
  Tạo tunnel trong Cloudflare Zero Trust → Networks → Tunnels, trỏ Public Hostname (vd. `https://api.ten-mien-cua-ban.com`) về `http://web:8080`, điền `CLOUDFLARE_TUNNEL_TOKEN` vào `.env`, sau đó chạy:
  ```bash
  docker compose --profile cloudflare up -d --build
  ```

## Đăng nhập tài khoản (TYT, Wikicv, TikTok)

Cookie/phiên Chrome của máy cũ **không dùng lại được** (Chrome trên macOS mã hoá cookie theo máy). Sau khi chạy:

1. Mở `http://<máy>:6080/vnc.html` → Connect → nhập `VNC_PASSWORD` trong `.env`. Đây là màn hình của Chrome chạy trong container.
2. Ở giao diện web, vào **Tài khoản** → bấm mở phiên đăng nhập của từng tài khoản → cửa sổ Chrome hiện trong màn hình noVNC → đăng nhập.
3. Cookie được lưu tự động; hồ sơ Chrome nằm trong volume `web-data` nên khởi động lại không phải đăng nhập lại.

## Dữ liệu

| Volume | Nội dung |
|---|---|
| `db-data` | PostgreSQL |
| `web-data` | `/app/data`: ảnh (`media`), hồ sơ Chrome (`browser-profiles`) |
| `kafka-data` | hàng đợi Kafka |

Sao lưu DB: `docker compose exec db pg_dump -U postgres SurveillanceCamerasDb > backup.sql`

Cập nhật mã nguồn mới: chép đè thư mục (giữ `.env`) rồi `docker compose up -d --build`. Volume giữ nguyên dữ liệu.

Nạp lại dữ liệu từ đầu (**xoá hết dữ liệu hiện có**): `docker compose down -v && docker compose up -d --build`.

## Cấu hình khác

Đặt thêm biến môi trường cho service `web` trong `docker-compose.yml`, dạng `Muc__Khoa`, ví dụ
`MovieDailySync__TimeOfDay: "03:00"`, `MovieDailySync__Enabled: "false"`.
