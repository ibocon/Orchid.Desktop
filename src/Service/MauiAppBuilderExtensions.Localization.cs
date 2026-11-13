namespace Orchid.Desktop.Service;

/// <summary>
/// MAUI 앱 빌더에 지역화 서비스를 등록하는 확장 메서드를 제공한다.
/// </summary>
public static class MauiAppBuilderExtensions
{
    /// <summary>
    /// 지역화 서비스(<see cref="Microsoft.Extensions.Localization.IStringLocalizer{T}"/> 등)를
    /// DI 컨테이너에 등록한다.
    /// https://learn.microsoft.com/en-us/aspnet/core/blazor/globalization-localization
    /// </summary>
    /// <param name="builder">MAUI 앱 빌더.</param>
    /// <returns>체이닝을 위한 동일 <paramref name="builder"/> 인스턴스.</returns>
    public static MauiAppBuilder ConfigureLocalization(this MauiAppBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Services.AddLocalization();
        return builder;
    }
}
