# 💻 Orchid.Desktop 소스 구조

> Windows 및 macOS를 지원하는 .NET MAUI Blazor Hybrid 클라이언트입니다.

## 역할

- 시스템 클립보드 변경을 감시하고 페어링된 기기와 동기화합니다.
- Razor 기반 데스크톱 UI와 OS별 네이티브 어댑터를 하나의 MAUI 프로젝트로 제공합니다.

## 디렉터리 구조

```text
src/
├── Assets/                       # 앱에서 사용하는 이미지·기타 자산
├── Component/                    # 공유 Razor UI 컴포넌트
│   ├── ClipboardList/            # 목록, CSS, 리소스, props 콜로케이션
│   ├── DesktopApp/               # 앱 셸, CSS, 리소스, props 콜로케이션
│   ├── LoginPanel/               # 로그인 UI
│   ├── OrchidDesktopDesignSystemProvider/
│   └── PairingPanel/             # 페어링 UI, CSS, 리소스, props 콜로케이션
├── Platforms/                    # OS별 MAUI 및 클립보드 구현
│   ├── MacCatalyst/
│   └── Windows/
├── Resources/                    # MAUI 패키징 리소스
│   └── AppIcon/
├── Service/                      # 데스크톱 어댑터와 앱 서비스
│   ├── Localization/             # 언어·공유 리소스
│   └── Pairing/                  # 페어링 QR 생성
├── wwwroot/                      # Blazor Hybrid 정적 자산
├── App.xaml / App.xaml.cs         # MAUI 애플리케이션 진입점
├── MainPage.xaml / .cs           # BlazorWebView 호스트
├── MainWindowLayout.cs            # 창 레이아웃 설정
├── MauiProgram.cs                 # DI 및 MAUI 부트스트랩
├── Orchid.Desktop.csproj          # MAUI 프로젝트
└── Orchid.Desktop.Resources.props # 리소스 props 레지스트리
```

## 배치 규칙

- 공유 Razor 컴포넌트는 `Component/{ComponentName}/` 전용 폴더에 둡니다.
- 컴포넌트의 `.razor`, 코드비하인드, CSS, `.resx`, `.props` 자산은 같은 폴더에 콜로케이션합니다.
- OS 의존 코드는 `Platforms/{Platform}/`에, 데스크톱 서비스는 `Service/`에 둡니다.
- 단위 테스트는 저장소 루트의 `test/UnitTests/`에 `{Sut}Test.cs` 형식으로 둡니다.
