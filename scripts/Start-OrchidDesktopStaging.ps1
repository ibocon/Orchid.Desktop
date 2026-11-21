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

.PARAMETER NoLog
    지정 시 앱 로그를 실시간으로 출력하지 않고 기동 직후 반환한다.

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

    [switch] $NoLog,

    [Parameter(DontShow)]
    [string] $RepositoryRoot = (Resolve-Path -Path (Join-Path -Path $PSScriptRoot -ChildPath '..')).Path,

    [Parameter(DontShow)]
    [string] $DotnetCommand = 'dotnet'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
[Console]::InputEncoding = [Console]::OutputEncoding = $OutputEncoding = [Text.UTF8Encoding]::new($false)

function Get-OrchidDesktopLogFile {
    [OutputType([System.IO.FileInfo])]
    param(
        [Parameter(Mandatory)]
        [datetime] $StartedAt,

        [Parameter(Mandatory)]
        [string] $LogRoot,

        [ValidateRange(1, 120)]
        [int] $TimeoutSeconds = 60
    )

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        if (Test-Path -LiteralPath $LogRoot -PathType Container) {
            $logFile = Get-ChildItem -LiteralPath $LogRoot -Filter 'app*.log' -File -ErrorAction SilentlyContinue |
                Where-Object { $_.LastWriteTime -ge $StartedAt.AddSeconds(-2) } |
                Sort-Object -Property LastWriteTime -Descending |
                Select-Object -First 1
            if ($null -ne $logFile) {
                return $logFile
            }
        }

        Start-Sleep -Milliseconds 300
    }

    return $null
}

function Format-OrchidDesktopLogEntry {
    [OutputType([string])]
    param(
        [Parameter(Mandatory)]
        [hashtable] $Entry,

        [Parameter(Mandatory)]
        [int] $ProcessId
    )

    if ($Entry['ProcessId'] -ne $ProcessId) {
        return $null
    }

    $level = if ($Entry['@l']) { [string] $Entry['@l'] } else { 'Information' }
    $text = if ($Entry['@m']) {
        [string] $Entry['@m']
    }
    elseif ($Entry['@mt']) {
        [regex]::Replace([string] $Entry['@mt'], '\{@?([A-Za-z0-9_]+)(?:[:,][^}]*)?\}', {
                param($match)
                $key = $match.Groups[1].Value
                if ($Entry.ContainsKey($key)) { "$($Entry[$key])" } else { $match.Value }
            })
    }
    else {
        return $null
    }

    $time = if ($Entry['@t']) { ([datetime] $Entry['@t']).ToLocalTime().ToString('HH:mm:ss.fff') } else { '' }
    $sourceContext = if ($Entry['SourceContext']) { "$($Entry['SourceContext']) " } else { '' }
    $line = "[$time $($level.Substring(0, [Math]::Min(3, $level.Length)).ToUpper())] $sourceContext$text"
    if ($Entry['@x']) {
        $line += "`n$($Entry['@x'])"
    }

    return $line
}

function Receive-OrchidDesktopLogTail {
    param(
        [Parameter(Mandatory)]
        [System.IO.FileInfo] $LogFile,

        [Parameter(Mandatory)]
        [int] $ProcessId
    )

    $esc = [char] 27
    $colors = @{ Verbose = 90; Debug = 90; Information = 39; Warning = 33; Error = 31; Fatal = 35 }
    Get-Content -LiteralPath $LogFile.FullName -Wait -Encoding utf8 | ForEach-Object {
        try {
            $entry = $_ | ConvertFrom-Json -AsHashtable
        }
        catch {
            return
        }

        $line = Format-OrchidDesktopLogEntry -Entry $entry -ProcessId $ProcessId
        if ($null -eq $line) {
            return
        }

        $level = if ($entry['@l']) { [string] $entry['@l'] } else { 'Information' }
        $colorCode = if ($colors.ContainsKey($level)) { $colors[$level] } else { 39 }
        Write-Host "$esc[${colorCode}m$line$esc[0m"
    }
}

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

$RunningProcesses = @(Get-Process -Name 'Orchid.Desktop' -ErrorAction SilentlyContinue)
if ($RunningProcesses.Count -gt 0 -and $DotnetCommand -eq 'dotnet') {
    Write-Output "[orchid-desktop-staging] stopping $($RunningProcesses.Count) running Orchid.Desktop instance(s) before build..."
    $RunningProcesses | Stop-Process -Force -ErrorAction SilentlyContinue
    Start-Sleep -Milliseconds 300
}

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
    $StartedAt = Get-Date
    $Process = Start-Process -FilePath $ExeCandidate.FullName -WorkingDirectory $ExeCandidate.DirectoryName -PassThru
    Write-Host "Orchid.Desktop 시작됨. (PID $($Process.Id), DOTNET_ENVIRONMENT=Staging)"
    if ($NoLog -or $PassThru) {
        if ($PassThru) {
            return $Process
        }

        return
    }

    $LogRoot = Join-Path $env:LOCALAPPDATA 'BotanicSoft\Orchid\logs'
    $LogFile = Get-OrchidDesktopLogFile -StartedAt $StartedAt -LogRoot $LogRoot
    if ($null -eq $LogFile) {
        Write-Warning "로그 파일을 찾지 못했습니다($LogRoot\app*.log). 앱이 아직 로그를 쓰지 않았을 수 있습니다."
        return
    }

    Write-Host "로그: $($LogFile.FullName) (Ctrl+C 로 로그 보기만 종료하며 앱은 계속 실행됩니다)"
    Receive-OrchidDesktopLogTail -LogFile $LogFile -ProcessId $Process.Id
}
finally {
    [Environment]::SetEnvironmentVariable('DOTNET_ENVIRONMENT', $PreviousEnv, 'Process')
}
