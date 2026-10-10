# Web API + giao diện React + Chrome (Playwright) chạy trên màn hình ảo, xem/đăng nhập qua noVNC.

# ── 1. Frontend ──────────────────────────────────────────────────────────────
FROM node:22-bookworm-slim AS frontend
WORKDIR /src/ClientApp
COPY src/Web/ClientApp/package.json src/Web/ClientApp/package-lock.json ./
RUN npm ci
COPY src/Web/ClientApp/ ./
# web-api-client.ts đã có sẵn trong mã nguồn: chỉ build Vite (bỏ bước sinh client bằng NSwag).
RUN npx vite build

# ── 2. Backend ───────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS backend
ARG TARGETARCH
WORKDIR /src
COPY . .
RUN RID=$([ "$TARGETARCH" = "arm64" ] && echo linux-arm64 || echo linux-x64) \
 && dotnet publish src/Web/Web.csproj -c Release -r "$RID" --self-contained false -o /app \
      -p:SkipSpaBuild=true -p:OpenApiGenerateDocuments=false

# ── 3. Runtime ───────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
ENV PLAYWRIGHT_BROWSERS_PATH=/ms-playwright \
    DISPLAY=:99 \
    ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    TZ=Asia/Ho_Chi_Minh

# Màn hình ảo + VNC + noVNC (xem cửa sổ Chrome qua trình duyệt), font tiếng Việt/Trung.
RUN apt-get update \
 && DEBIAN_FRONTEND=noninteractive apt-get install -y --no-install-recommends \
      xvfb x11vnc fluxbox novnc websockify tzdata ca-certificates \
      fonts-noto-core fonts-noto-cjk \
 && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY --from=backend /app ./
COPY --from=frontend /src/ClientApp/build ./wwwroot

# Chromium đúng phiên bản của Microsoft.Playwright, cài bằng driver đi kèm (không cần PowerShell).
RUN NODE=$(ls -d /app/.playwright/node/*/node | head -1) \
 && "$NODE" /app/.playwright/package/cli.js install --with-deps chromium \
 && rm -rf /var/lib/apt/lists/*

COPY docker/entrypoint.sh /entrypoint.sh
RUN chmod +x /entrypoint.sh && mkdir -p /app/data

EXPOSE 8080 6080
VOLUME ["/app/data"]
ENTRYPOINT ["/entrypoint.sh"]
