# 💻 Orchid.Desktop (`src/Desktop`)

> **Windows & macOS** 데스크톱 환경을 지원하는 **.NET MAUI Blazor Hybrid 클라이언트 애플리케이션**

---

## 1. 프로젝트 역할 및 책임
- **역할**: Windows와 macOS 데스크톱 사용자를 위한 백그라운드 클립보드 상시 감시 및 시스템 트레이 상주 앱.
- **특징**:
  - `Monolith/src/Oxygen/Desktop`의 실무 구조를 계승하여 Windows(`net10.0-windows10.0.19041`)와 Mac Catalyst(`net10.0-maccatalyst`)를 단일 프로젝트(`SingleProject=true`)로 타깃팅.
  - UI는 고성능 Razor 컴포넌트(Blazor Hybrid)로 렌더링하고, 백그라운드 클립보드 이벤트는 OS 네이티브 훅으로 수신.

---

## 2. 폴더 및 디렉터리 구조

```text
src/Desktop/
├── Platforms/                   # OS별 네이티브 클립보드 리스너 구현부
│   ├── Windows/                 # Windows 전용 (Win32 P/Invoke)
│   │   ├── Win32ClipboardListener.cs # AddClipboardFormatListener 기반 백그라운드 훅
│   │   ├── Win32SystemTray.cs   # 작업 표시줄 시스템 트레이 아이콘 관리
│   │   └── GlobalHotkeyHook.cs  # 클립보드 팝업용 글로벌 단축키 바인딩
│   └── MacCatalyst/             # macOS 전용 (AppKit Cocoa)
│       ├── MacPasteboardListener.cs  # NSPasteboard changeCount 폴링 감시
│       └── MacStatusItem.cs     # 상단 메뉴바(Status Bar) 아이콘 관리
├── Component/                   # Blazor Desktop 전용 UI 컴포넌트
│   ├── ClipboardList.razor      # 검색 가능한 클립보드 히스토리 목록
│   ├── ItemCard.razor           # 텍스트, 코드 하이라이트, 이미지 썸네일 카드
│   └── SettingsModal.razor      # E2EE 키 설정 및 디바이스 페어링 모달
├── Service/                     # 데스크톱 어댑터 구현체
│   ├── DesktopClipboardAdapter.cs # ISystemClipboard 데스크톱 구현체
│   └── TrayNotificationService.cs # 데스크톱 네이티브 토스트 알림
├── Properties/                  # 앱 매니페스트 및 패키징 설정
│   └── launchSettings.json
├── wwwroot/                     # 정적 웹 자산 (CSS, JS)
│   ├── css/app.css
│   └── index.html               # Blazor Hybrid 진입점
├── App.xaml / App.xaml.cs       # MAUI 애플리케이션 진입점
├── MainPage.xaml                # BlazorWebView 호스트 컨테이너
├── MauiProgram.cs               # DI 컨테이너 및 호스트 부트스트랩
├── Orchid.Desktop.csproj        # 프로젝트 파일 (.NET MAUI Blazor)
└── README.md                    # 본 설명 문서
```
