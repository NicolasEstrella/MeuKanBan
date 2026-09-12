# syntax=docker/dockerfile:1

# ---------- Stage 1: build Angular ----------
# Digest verificado em 2026-09-11 via `docker buildx imagetools inspect`
FROM node:24-alpine@sha256:50c8e8ca1d27439048670df5883f32d57cf81cff6233222c893fd0d9884cbd81 AS build
WORKDIR /app

# Camada de dependencias separada do codigo-fonte (cache)
COPY package.json package-lock.json ./
RUN npm ci

COPY . .
RUN npm run build

# ---------- Stage 2: servidor estatico nao-root ----------
# nginxinc/nginx-unprivileged roda INTEIRO como uid 101 e escuta na 8080
# (o nginx oficial roda o master process como root - rejeitado).
# Digest verificado em 2026-09-11 via `docker buildx imagetools inspect`
FROM nginxinc/nginx-unprivileged:1.27-alpine@sha256:65e3e85dbaed8ba248841d9d58a899b6197106c23cb0ff1a132b7bfe0547e4c0 AS runtime

COPY nginx.conf /etc/nginx/conf.d/default.conf

# --chown=101:101 permite ao entrypoint (roda como nginx) gerar config.json em runtime
COPY --from=build --chown=101:101 /app/dist/angular-app/browser /usr/share/nginx/html

# Runtime config: envsubst de API_URL em assets/config.json
COPY --chmod=0755 docker-entrypoint.d/40-envsubst-config.sh /docker-entrypoint.d/40-envsubst-config.sh

EXPOSE 8080

HEALTHCHECK --interval=10s --timeout=3s --start-period=5s --retries=5 \
  CMD ["wget", "-q", "-O", "/dev/null", "http://127.0.0.1:8080/"]
