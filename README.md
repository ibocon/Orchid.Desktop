# Orchid.Desktop (Universal Clipboard Bridge)

Windows 및 macOS 데스크톱 환경을 지원하는 .NET MAUI Blazor Hybrid E2EE 클립보드 브리지 클라이언트입니다.

## 로컬 Windows에서 Staging 환경 빌드 및 실행

Hanul Staging 백엔드(`https://stag.botanicsoft.com/orchid`)를 대상으로 로컬 데스크톱 앱을 빌드하고 즉시 실행하려면 아래 PowerShell 스크립트를 사용합니다.

### 1. 기본 실행 (Staging 사전 점검 + Debug 빌드 + 앱 실행)

```pwsh
pwsh -NoProfile -ExecutionPolicy Bypass -File D:\Source\BotanicSoft\Orchid.Desktop\scripts\Start-OrchidDesktopStaging.ps1
```

저장소 루트(`D:\Source\BotanicSoft\Orchid.Desktop`)에 위치한 경우:

```pwsh
pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/Start-OrchidDesktopStaging.ps1
```

### 2. 주요 옵션

```pwsh
# Release 구성으로 빌드 및 실행
pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/Start-OrchidDesktopStaging.ps1 -Configuration Release

# 앱 창을 띄우지 않고 Staging 사전 점검 및 빌드만 검증
pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/Start-OrchidDesktopStaging.ps1 -BuildOnly

# 네트워크 사전 점검(preflight) 생략 후 즉시 빌드·실행
pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/Start-OrchidDesktopStaging.ps1 -SkipPreflight
```

### 3. 동작 순서

1. **Staging Preflight**: `POST https://stag.botanicsoft.com/orchid/hubs/clipboard/negotiate?negotiateVersion=1`을 호출해 YARP(`/orchid`) 라우팅과 `Orchid.Clipboard` SignalR 허브가 정상 응답(`connectionId`)하는지 먼저 확인합니다.
2. **MSBuild Staging 바인딩**: `src/Orchid.Desktop.csproj`를 `-f net10.0-windows10.0.19041.0 -p:WindowsPackageType=None -p:OrchidRelayUrl=https://stag.botanicsoft.com/orchid`로 빌드합니다.
3. **프로세스 기동**: `DOTNET_ENVIRONMENT=Staging`을 주입해 빌드된 `Orchid.Desktop.exe`를 기동합니다.

### 4. Staging E2EE 왕복 통합 테스트 실행

```pwsh
dotnet test test/IntegrationTests/Orchid.Desktop.IntegrationTests.csproj
```
