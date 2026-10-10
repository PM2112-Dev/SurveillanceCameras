#!/bin/sh
# Khởi động màn hình ảo cho Chrome, VNC + noVNC để xem/đăng nhập tài khoản, rồi chạy Web.
set -e

# Ảnh mang từ máy cũ (docker/media): chỉ chép khi kho ảnh còn trống.
if [ -d /seed/media ] && [ -n "$(ls -A /seed/media 2>/dev/null)" ] && [ -z "$(ls -A /app/data/media 2>/dev/null)" ]; then
  mkdir -p /app/data/media && cp -R /seed/media/. /app/data/media/
fi

rm -f /tmp/.X99-lock
Xvfb :99 -screen 0 "${SCREEN_SIZE:-1440x900x24}" -nolisten tcp &
sleep 1
fluxbox >/dev/null 2>&1 &

if [ -n "$VNC_PASSWORD" ]; then
  x11vnc -storepasswd "$VNC_PASSWORD" /tmp/vncpass >/dev/null
  x11vnc -display :99 -rfbauth /tmp/vncpass -forever -shared -quiet -rfbport 5900 &
else
  echo "CẢNH BÁO: chưa đặt VNC_PASSWORD — màn hình Chrome xem được không cần mật khẩu." >&2
  x11vnc -display :99 -nopw -forever -shared -quiet -rfbport 5900 &
fi
websockify --web /usr/share/novnc 6080 localhost:5900 >/dev/null 2>&1 &

exec dotnet SurveillanceCameras.Web.dll
