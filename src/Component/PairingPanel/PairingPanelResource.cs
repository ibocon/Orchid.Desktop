namespace Orchid.Desktop.Component;

/// <summary>
/// PairingPanel 리소스 키 상수 마커 클래스.
/// </summary>
public sealed class PairingPanelResource
{
    /// <summary>페어링 제목 리소스 키.</summary>
    public const string Title = nameof(Title);

    /// <summary>페어링 시작 버튼 문구 리소스 키.</summary>
    public const string StartPairing = nameof(StartPairing);

    /// <summary>페어링 키 스캔/입력 안내 문구 리소스 키.</summary>
    public const string ScanOrEnterKeyGuide = nameof(ScanOrEnterKeyGuide);

    /// <summary>페어링 키 레이블 리소스 키.</summary>
    public const string PairingKeyLabel = nameof(PairingKeyLabel);

    /// <summary>페어링 상태 준비 배지 문구 리소스 키.</summary>
    public const string StatusReadyBadge = nameof(StatusReadyBadge);

    /// <summary>페어링 키 복사 버튼 문구 리소스 키.</summary>
    public const string CopyKeyButton = nameof(CopyKeyButton);

    /// <summary>페어링 키 복사 완료 버튼 문구 리소스 키.</summary>
    public const string CopiedKeyButton = nameof(CopiedKeyButton);

    /// <summary>새 페어링 키 재발급 버튼 문구 리소스 키.</summary>
    public const string RegenerateKeyButton = nameof(RegenerateKeyButton);

    /// <summary>QR 코드 스캔 안내 캡션 리소스 키.</summary>
    public const string QrScanCaption = nameof(QrScanCaption);

    private PairingPanelResource()
    {
    }
}
