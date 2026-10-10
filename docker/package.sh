#!/bin/bash
# Đóng gói để mang sang máy khác: mã nguồn + .env + dữ liệu DB hiện tại + ảnh đã lưu → docker/export/story-system-<ngày>.tar.gz
# Chạy ở thư mục gốc repo:  ./docker/package.sh
# Biến tuỳ chọn: PGHOST PGPORT PGUSER PGPASSWORD PGDATABASE (mặc định theo appsettings.json), SKIP_DB=1 để không kèm DB.
set -euo pipefail
cd "$(dirname "$0")/.."

export PGHOST="${PGHOST:-localhost}" PGPORT="${PGPORT:-5432}" PGUSER="${PGUSER:-postgres}" \
       PGPASSWORD="${PGPASSWORD:-admin123}" PGDATABASE="${PGDATABASE:-SurveillanceCamerasDb}"

mkdir -p docker/initdb docker/export docker/media
if [ "${SKIP_DB:-0}" != "1" ]; then
  echo "→ Xuất DB $PGDATABASE ($PGHOST:$PGPORT)…"
  # Nếu máy không có pg_dump cùng bản với server, dùng pg_dump trong container postgres.
  if command -v pg_dump >/dev/null 2>&1; then
    pg_dump --no-owner --no-privileges --clean --if-exists "$PGDATABASE" > docker/initdb/01-data.sql
  else
    docker run --rm -e PGPASSWORD --network host postgres:17 \
      pg_dump -h "$PGHOST" -p "$PGPORT" -U "$PGUSER" --no-owner --no-privileges --clean --if-exists "$PGDATABASE" \
      > docker/initdb/01-data.sql
  fi
fi

if [ -d src/Web/data/media ]; then
  echo "→ Chép ảnh đã lưu (data/media)…"
  rm -rf docker/media && cp -R src/Web/data/media docker/media
fi

OUT="docker/export/story-system-$(date +%Y%m%d-%H%M).tar.gz"
echo "→ Nén mã nguồn + dữ liệu vào $OUT…"
# .env (mật khẩu, khoá JWT) không nằm trong git nhưng cần mang theo để máy mới chạy ngay.
{ git ls-files -co --exclude-standard; find docker/initdb docker/media -type f 2>/dev/null; [ -f .env ] && echo .env; } \
  | grep -v '^docker/export/' | sort -u \
  | while IFS= read -r f; do [ -f "$f" ] && printf '%s\n' "$f"; done \
  | tar -czf "$OUT" -T -
ls -lh "$OUT"
echo "Xong. Chép file này sang máy mới rồi làm theo docker/README.md."
