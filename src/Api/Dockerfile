# syntax=docker/dockerfile:1

# ---------- Stage 1: build (toolchain completa, descartada) ----------
# Digest verificado em 2026-09-11 via `docker buildx imagetools inspect`
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine@sha256:4ac537e13e2f55d1d588ed3e618cb0cb6b82dd8deb17830de43d5086fbde958b AS build
WORKDIR /src

# Camada de restore separada: so invalida quando um .csproj muda (cache de dependencias)
COPY src/Api/MeuKanBan.Api.csproj src/Api/
COPY src/Application/MeuKanBan.Application.csproj src/Application/
COPY src/Domain/MeuKanBan.Domain.csproj src/Domain/
COPY src/Infrastructure/MeuKanBan.Infrastructure.csproj src/Infrastructure/
RUN dotnet restore src/Api/MeuKanBan.Api.csproj

COPY src/ src/
RUN dotnet publish src/Api/MeuKanBan.Api.csproj -c Release -o /app/publish --no-restore

# ---------- Stage 2: runtime minimo ----------
# Digest verificado em 2026-09-11 via `docker buildx imagetools inspect`
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine@sha256:6bb0fab0ef31f44f710a668c39c2263ae810f5adf868afa34cbd86815912c7fe AS runtime
WORKDIR /app

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

COPY --from=build /app/publish .

# Usuario nao-root embutido nas imagens .NET 8+ (uid 1654)
USER $APP_UID

# busybox wget existe na base alpine: 503 do /health/ready => exit code != 0 => unhealthy
HEALTHCHECK --interval=10s --timeout=3s --start-period=15s --retries=5 \
  CMD ["wget", "-q", "-O", "/dev/null", "http://127.0.0.1:8080/health/ready"]

ENTRYPOINT ["dotnet", "MeuKanBan.Api.dll"]
