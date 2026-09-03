FROM node:22-alpine AS build
WORKDIR /src

COPY package.json package-lock.json ./
RUN npm ci

COPY . .
RUN npm run build

# Unprivileged image listens on 8080 by default.
FROM nginxinc/nginx-unprivileged:alpine
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /src/dist/timeready/browser /usr/share/nginx/html

USER nginx
EXPOSE 8080

# Use 127.0.0.1 — in this image "localhost" prefers IPv6, but nginx binds IPv4.
HEALTHCHECK --interval=10s --timeout=5s --retries=5 --start-period=10s \
  CMD wget -q -O /dev/null http://127.0.0.1:8080/healthz || exit 1
