#Requires -Version 5.1
<#
.SYNOPSIS
    Smoke test da stack T02 (PostgreSQL + API + frontend) via Docker Compose.

.DESCRIPTION
    Reproduzivel e NAO destrutivo: nunca executa `docker compose down -v` e
    nunca imprime segredos. Falha com exit code != 0.

    Etapas:
      1. Valida `docker compose config` sem imprimir a saida (--quiet) e
         confirma, em memoria, que o segredo nao vaza na configuracao renderizada.
      2. Sobe a stack e aguarda os tres servicos healthy (--wait).
      3. Testa /health/live, /health/ready, frontend e assets/config.json
         (runtime config) e a conectividade browser->API pela URL configurada.
      4. Para o PostgreSQL, exige falha de readiness (sem falso positivo),
         reinicia e exige recuperacao.

    Pre-requisitos: Docker Desktop, .env criado a partir de .env.example e o
    arquivo de senha (ver README > Inicio local).

.EXAMPLE
    pwsh tests/Smoke/t02-compose-smoke.ps1
    pwsh tests/Smoke/t02-compose-smoke.ps1 -SkipBuild
#>
[CmdletBinding()]
param(
    [switch]$SkipBuild,
    [int]$TimeoutSeconds = 240
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$script:postgresStopped = $false

function Write-Step([string]$Message) { Write-Host "`n==> $Message" -ForegroundColor Cyan }

function Assert-ComposeOk([string]$Context) {
    if ($LASTEXITCODE -ne 0) { throw "docker compose falhou em: $Context (exit $LASTEXITCODE)" }
}

function Get-HttpStatus([string]$Url) {
    try {
        return [int](Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec 10).StatusCode
    } catch {
        if ($_.Exception.Response) { return [int]$_.Exception.Response.StatusCode }
        return -1
    }
}

function Wait-Until([scriptblock]$Condition, [int]$Seconds, [string]$What) {
    $deadline = (Get-Date).AddSeconds($Seconds)
    while ((Get-Date) -lt $deadline) {
        if (& $Condition) { return }
        Start-Sleep -Seconds 3
    }
    throw "Timeout (${Seconds}s) aguardando: $What"
}

function Resolve-SecretFile([string]$RepoRoot) {
    # Mesma resolucao do Compose: POSTGRES_PASSWORD_FILE do ambiente ou do
    # .env local; default ./infra/postgres/secrets/postgres_password.
    $configured = $env:POSTGRES_PASSWORD_FILE
    if (-not $configured) {
        $envFile = Join-Path $RepoRoot '.env'
        if (Test-Path $envFile) {
            $match = Select-String -Path $envFile -Pattern '^\s*POSTGRES_PASSWORD_FILE\s*=\s*(.+?)\s*$'
            if ($match) { $configured = $match.Matches[0].Groups[1].Value }
        }
    }
    if (-not $configured) { $configured = './infra/postgres/secrets/postgres_password' }
    return [System.IO.Path]::GetFullPath((Join-Path $RepoRoot $configured))
}

try {
    $repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
    Set-Location $repoRoot
    $apiBase = 'http://localhost:8080'
    $frontendBase = 'http://localhost:4200'

    # ------------------------------------------------------------------
    Write-Step '1/6 Validando docker compose config sem expor segredos'
    docker compose config --quiet | Out-Null
    Assert-ComposeOk 'config --quiet'
    Write-Host '    config aceita (saida suprimida com --quiet).'

    $secretFile = Resolve-SecretFile $repoRoot
    if (-not (Test-Path $secretFile) -or ((Get-Content $secretFile -Raw).Trim().Length -eq 0)) {
        throw "Arquivo de senha ausente ou vazio: $secretFile. Crie a partir das instrucoes do README/.env.example."
    }

    # Prova de nao-vazamento: renderiza em memoria e garante que o segredo nao aparece.
    $secretValue = (Get-Content $secretFile -Raw).Trim()
    $renderedConfig = (docker compose config 2>$null | Out-String)
    Assert-ComposeOk 'config (renderizacao em memoria)'
    if ($renderedConfig.Contains($secretValue)) {
        throw 'FALHA DE SEGURANCA: docker compose config expoe o segredo.'
    }
    Write-Host '    segredo ausente da configuracao renderizada.'

    # ------------------------------------------------------------------
    Write-Step '2/6 Subindo stack e aguardando servicos healthy'
    $upArgs = @('up', '-d', '--wait', '--wait-timeout', "$TimeoutSeconds")
    if (-not $SkipBuild) { $upArgs += '--build' }
    docker compose @upArgs
    Assert-ComposeOk 'up -d --wait'

    foreach ($service in 'meukanban-postgres', 'meukanban-api', 'meukanban-frontend') {
        $health = (docker inspect -f '{{.State.Health.Status}}' $service 2>$null).Trim()
        if ($health -ne 'healthy') { throw "Container $service nao esta healthy (estado: '$health')." }
        Write-Host "    $service healthy."
    }

    # ------------------------------------------------------------------
    Write-Step '3/6 Health checks da API'
    $live = Invoke-WebRequest -Uri "$apiBase/health/live" -UseBasicParsing -TimeoutSec 10
    if ($live.StatusCode -ne 200 -or $live.Content -ne 'Healthy') {
        throw "Liveness inesperado: HTTP $($live.StatusCode), corpo '$($live.Content)'."
    }
    if ((Get-HttpStatus "$apiBase/health/ready") -ne 200) {
        throw 'Readiness deveria ser 200 com a stack saudavel.'
    }
    Write-Host '    /health/live e /health/ready => 200 Healthy.'

    # ------------------------------------------------------------------
    Write-Step '4/6 Frontend e runtime config'
    if ((Get-HttpStatus "$frontendBase/") -ne 200) { throw 'Frontend nao respondeu 200.' }
    $runtimeConfig = Invoke-RestMethod -Uri "$frontendBase/assets/config.json" -TimeoutSec 10
    if (-not $runtimeConfig.apiUrl) { throw 'assets/config.json sem apiUrl (runtime config quebrado).' }
    if ((Get-HttpStatus "$($runtimeConfig.apiUrl)/health/live") -ne 200) {
        throw "API inalcancavel pela apiUrl do runtime config ($($runtimeConfig.apiUrl))."
    }
    Write-Host "    frontend 200; apiUrl do runtime config responde ($($runtimeConfig.apiUrl))."

    # ------------------------------------------------------------------
    Write-Step '5/6 Dependencia indisponivel: parando PostgreSQL'
    docker compose stop postgres | Out-Null
    Assert-ComposeOk 'stop postgres'
    $script:postgresStopped = $true
    Wait-Until { (Get-HttpStatus "$apiBase/health/ready") -eq 503 } 120 'readiness 503 com Postgres parado'
    Write-Host '    readiness => 503 (sem falso positivo).'

    Write-Step '6/6 Recuperacao: reiniciando PostgreSQL'
    docker compose start postgres | Out-Null
    Assert-ComposeOk 'start postgres'
    $script:postgresStopped = $false
    Wait-Until {
        (docker inspect -f '{{.State.Health.Status}}' meukanban-postgres 2>$null).Trim() -eq 'healthy'
    } $TimeoutSeconds 'container meukanban-postgres healthy novamente'
    Wait-Until { (Get-HttpStatus "$apiBase/health/ready") -eq 200 } $TimeoutSeconds 'readiness 200 apos restart'
    Wait-Until {
        (docker inspect -f '{{.State.Health.Status}}' meukanban-api 2>$null).Trim() -eq 'healthy'
    } $TimeoutSeconds 'container meukanban-api healthy novamente'
    Write-Host '    readiness => 200 e API healthy apos recuperacao.'

    Write-Host "`nSMOKE T02 PASSOU" -ForegroundColor Green
    exit 0
} catch {
    Write-Host "`nSMOKE T02 FALHOU: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
} finally {
    # Recupera o Postgres se a falha ocorreu com o banco parado (deixa a stack utilizavel).
    if ($script:postgresStopped) {
        Write-Host 'Restaurando PostgreSQL parado durante a falha...' -ForegroundColor Yellow
        docker compose start postgres | Out-Null
    }
}
