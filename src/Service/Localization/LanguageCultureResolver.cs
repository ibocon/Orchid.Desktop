using System.Globalization;

namespace Orchid.Desktop.Service;

/// <summary>
/// <see cref="Language"/> 선택을 <see cref="CultureInfo"/>로 해석하고
/// 프로세스 기본 컬처와 현재 스레드 컬처에 적용한다.
/// </summary>
public static class LanguageCultureResolver
{
    private const string EnglishCultureName = "en-US";
    private const string KoreanCultureName = "ko-KR";

    /// <summary>
    /// 언어 선택에 대응하는 컬처를 해석한다. <see cref="Language.System"/>은
    /// 설치 UI 컬처의 2자리 ISO 코드가 "ko"면 ko-KR, 그 외 언어는 en-US로 폴백한다.
    /// </summary>
    /// <param name="language">해석할 언어 선택.</param>
    /// <param name="installedUiCulture">
    /// <see cref="Language.System"/> 판별에 사용할 설치 UI 컬처.
    /// <see langword="null"/>이면 <see cref="CultureInfo.InstalledUICulture"/>를 사용한다.
    /// </param>
    /// <returns>해석된 <see cref="CultureInfo"/>.</returns>
    public static CultureInfo Resolve(Language language, CultureInfo? installedUiCulture = null)
    {
        return language switch
        {
            Language.English => new CultureInfo(EnglishCultureName),
            Language.Korean => new CultureInfo(KoreanCultureName),
            _ => (installedUiCulture ?? CultureInfo.InstalledUICulture)
                    .TwoLetterISOLanguageName.Equals("ko", StringComparison.OrdinalIgnoreCase)
                ? new CultureInfo(KoreanCultureName)
                : new CultureInfo(EnglishCultureName),
        };
    }

    /// <summary>
    /// 스레드 풀 전체와 현재 스레드의 <c>CurrentCulture</c>/<c>CurrentUICulture</c>를
    /// 해석된 컬처로 설정한다.
    /// </summary>
    /// <param name="language">적용할 언어 선택.</param>
    /// <param name="installedUiCulture">
    /// <see cref="Language.System"/> 판별에 사용할 설치 UI 컬처.
    /// <see langword="null"/>이면 <see cref="CultureInfo.InstalledUICulture"/>를 사용한다.
    /// </param>
    public static void Apply(Language language, CultureInfo? installedUiCulture = null)
    {
        CultureInfo culture = Resolve(language, installedUiCulture);

        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
    }
}
