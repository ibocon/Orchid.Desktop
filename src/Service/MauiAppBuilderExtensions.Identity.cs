using System.Reflection;
using System.Text.Json;
using Garden.Share;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Orchid.Core.Login;

namespace Orchid.Desktop.Service;

public static partial class MauiAppBuilderExtensions
{
    private const string AppSettingsStagingResourcePath = "Orchid.Desktop.Resources.appsettings.Staging.json";

    /// <summary>
    /// Peony.Mobile 및 Garden.Maui 규약에 따라 임베드된 <c>appsettings.Staging.json</c>의
    /// <see cref="IdentityOptions"/> 설정과 데스크톱 OIDC 인증 서비스를 등록합니다.
    /// </summary>
    /// <param name="builder">구성할 MAUI 앱 빌더.</param>
    /// <returns>체이닝을 위한 동일 <paramref name="builder"/> 인스턴스.</returns>
    public static MauiAppBuilder ConfigureIdentity(this MauiAppBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        IdentityOptions configuredOptions = LoadEmbeddedIdentityOptions();
        builder.Services.AddSingleton<IOptions<IdentityOptions>>(Options.Create(configuredOptions));
        builder.Services.AddSingleton<IOrchidDesktopAuthService, OrchidDesktopAuthService>();
        builder.Services.TryAddSingleton<IResourceOwnerAuth, OrchidDesktopResourceOwnerAuth>();
        builder.Services.TryAddScoped<OrchidLoginOutcomeHandler>();
        builder.Services.TryAddScoped<ILoginOutcomeHandler>(provider => provider.GetRequiredService<OrchidLoginOutcomeHandler>());

        return builder;
    }

    private static IdentityOptions LoadEmbeddedIdentityOptions()
    {
        IdentityOptions options = OrchidDesktopAuthService.CreateDefaultIdentityOptions();
        Assembly assembly = typeof(MauiAppBuilderExtensions).Assembly;
        using Stream? stream = assembly.GetManifestResourceStream(AppSettingsStagingResourcePath);
        if (stream is null)
        {
            return options;
        }

        using JsonDocument document = JsonDocument.Parse(stream);
        if (!document.RootElement.TryGetProperty(IdentityOptions.DefaultSectionName, out JsonElement identitySection))
        {
            return options;
        }

        if (identitySection.TryGetProperty(nameof(IdentityOptions.BaseAddress), out JsonElement baseAddressElement) &&
            Uri.TryCreate(baseAddressElement.GetString(), UriKind.Absolute, out Uri? baseAddress))
        {
            options.BaseAddress = baseAddress;
        }

        if (identitySection.TryGetProperty(nameof(IdentityOptions.Realm), out JsonElement realmElement) &&
            !string.IsNullOrWhiteSpace(realmElement.GetString()))
        {
            options.Realm = realmElement.GetString()!;
        }

        if (identitySection.TryGetProperty(nameof(IdentityOptions.ClientId), out JsonElement clientIdElement) &&
            !string.IsNullOrWhiteSpace(clientIdElement.GetString()))
        {
            options.ClientId = clientIdElement.GetString()!;
        }

        if (identitySection.TryGetProperty(nameof(IdentityOptions.RedirectUrl), out JsonElement redirectUrlElement) &&
            Uri.TryCreate(redirectUrlElement.GetString(), UriKind.Absolute, out Uri? redirectUrl))
        {
            options.RedirectUrl = redirectUrl;
        }

        if (identitySection.TryGetProperty(nameof(IdentityOptions.ValidAudience), out JsonElement validAudienceElement) &&
            !string.IsNullOrWhiteSpace(validAudienceElement.GetString()))
        {
            options.ValidAudience = validAudienceElement.GetString()!;
        }

        return options;
    }
}
