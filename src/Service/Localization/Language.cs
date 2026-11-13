namespace Orchid.Desktop.Service;

/// <summary>
/// 사용자가 선택한 표시 언어.
/// </summary>
public enum Language
{
    /// <summary>
    /// OS 설치 언어를 따른다(지원하지 않는 언어는 영어로 폴백).
    /// </summary>
    System = 0,

    /// <summary>
    /// 영어(en-US)로 고정.
    /// </summary>
    English = 1,

    /// <summary>
    /// 한국어(ko-KR)로 고정.
    /// </summary>
    Korean = 2,
}
