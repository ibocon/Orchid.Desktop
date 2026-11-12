#!/usr/bin/env pwsh
#requires -Version 7.0

<#
.SYNOPSIS
    Orchid.Desktop을 Hanul Staging(/orchid) 환경 설정으로 빌드하고 로컬 Windows에서 실행한다.

.DESCRIPTION
    1) Staging SignalR 협상 엔드포인트(/hubs/clipboard/negotiate) 사전 점검(preflight)을 수행한다.
    2) src/Orchid.Desktop.csproj를 net10.0-windows10.0.19041.0 타깃과 -p:OrchidRelayUrl=<StagingUrl>로 빌드한다.
    3) DOTNET_ENVIRONMENT=Staging 환경 변수를 주입해 빌드된 Orchid.Desktop.exe를 실행한다.

.PARAMETER Configuration
    빌드 구성(Debug 또는 Release). 기본값은 Debug다.

.PARAMETER RelayUrl
    데스크톱 클라이언트가 연결할 릴레이 베이스 URL. 기본값은 https://stag.botanicsoft.com/orchid 다.

.PARAMETER BuildOnly
    지정 시 사전 점검과 빌드만 수행하고 프로세스를 기동하지 않는다.

.PARAMETER SkipPreflight
    지정 시 Staging 릴레이 엔드포인트 사전 연결 점검을 건너뛴다.

.PARAMETER PassThru
    지정 시 기동된 프로세스(System.Diagnostics.Process) 또는 빌드 결과 객체를 반환한다.

.EXAMPLE
    pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/Start-OrchidDesktopStaging.ps1
    Staging 사전 점검 후 Debug 빌드 및 앱 실행을 수행한다.

.EXAMPLE
    pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/Start-OrchidDesktopStaging.ps1 -Configuration Release -BuildOnly
    Release 구성으로 Staging 빌드만 수행한다.
#>

[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string] $Configuration = 'Debug',

    [string] $RelayUrl = 'https://stag.botanicsoft.com/orchid',

    [switch] $BuildOnly,

    [switch] $SkipPreflight,

    [switch] $PassThru,

    [Parameter(DontShow)]
    [string] $RepositoryRoot = (Resolve-Path -Path (Join-Path -Path $PSScriptRoot -ChildPath '..')).Path,

    [Parameter(DontShow)]
    [string] $DotnetCommand = 'dotnet'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$NormalizedRelayUrl = $RelayUrl.TrimEnd('/')
$NegotiateUri = "$NormalizedRelayUrl/hubs/clipboard/negotiate?negotiateVersion=1"
$TargetFramework = 'net10.0-windows10.0.19041.0'
$ProjectPath = Join-Path -Path $RepositoryRoot -ChildPath 'src\Orchid.Desktop.csproj'

if (-not (Test-Path -LiteralPath $ProjectPath -PathType Leaf)) {
    throw "[orchid-desktop-staging] project-not-found: $ProjectPath"
}

if (-not $SkipPreflight) {
    Write-Output "[orchid-desktop-staging] preflight -> POST $NegotiateUri"
    $NegotiateResponse = Invoke-RestMethod -Method Post -Uri $NegotiateUri -Body '' -TimeoutSec 15
    if ($null -eq $NegotiateResponse -or [string]::IsNullOrWhiteSpace([string] $NegotiateResponse.connectionId)) {
        throw "[orchid-desktop-staging] preflight-failed: connectionId missing from $NegotiateUri"
    }

    Write-Output "[orchid-desktop-staging] preflight OK (connectionId=$($NegotiateResponse.connectionId))"
}

$BuildArgs = @(
    'build'
    $ProjectPath
    '-f', $TargetFramework
    '-c', $Configuration
    '-p:WindowsPackageType=None'
    "-p:OrchidRelayUrl=$NormalizedRelayUrl"
    '-p:EnforceCodeStyleInBuild=false'
)

Write-Output "[orchid-desktop-staging] building src/Orchid.Desktop.csproj ($Configuration, OrchidRelayUrl=$NormalizedRelayUrl)..."
& $DotnetCommand @BuildArgs
if ($LASTEXITCODE -ne 0) {
    throw "[orchid-desktop-staging] build-failed: exit code $LASTEXITCODE"
}

$BinDirectory = Join-Path -Path $RepositoryRoot -ChildPath "src\bin\$Configuration\$TargetFramework"
$ExeCandidate = Get-ChildItem -Path $BinDirectory -Recurse -Filter 'Orchid.Desktop.exe' -ErrorAction SilentlyContinue |
    Sort-Object -Property LastWriteTimeUtc -Descending |
    Select-Object -First 1

if ($null -eq $ExeCandidate) {
    throw "[orchid-desktop-staging] executable-not-found under $BinDirectory"
}

Write-Output "[orchid-desktop-staging] executable ready: $($ExeCandidate.FullName)"

if ($BuildOnly) {
    if ($PassThru) {
        return [pscustomobject]@{
            ExecutablePath = $ExeCandidate.FullName
            RelayUrl       = $NormalizedRelayUrl
            Configuration  = $Configuration
            Environment    = 'Staging'
            Launched       = $false
        }
    }

    return
}

$PreviousEnv = [Environment]::GetEnvironmentVariable('DOTNET_ENVIRONMENT', 'Process')
try {
    [Environment]::SetEnvironmentVariable('DOTNET_ENVIRONMENT', 'Staging', 'Process')
    $Process = Start-Process -FilePath $ExeCandidate.FullName -WorkingDirectory $ExeCandidate.DirectoryName -PassThru
    Write-Output "[orchid-desktop-staging] launched Orchid.Desktop.exe (PID=$($Process.Id), DOTNET_ENVIRONMENT=Staging)"
    if ($PassThru) {
        return $Process
    }
}
finally {
    [Environment]::SetEnvironmentVariable('DOTNET_ENVIRONMENT', $PreviousEnv, 'Process')
}
