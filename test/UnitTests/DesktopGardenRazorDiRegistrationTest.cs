using FluentAssertions;
using Garden.Razor;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using Xunit;

namespace Orchid.UnitTests.Desktop;

public sealed class DesktopGardenRazorDiRegistrationTest
{
    [Fact]
    public void GivenDesktopServiceCollectionWhenRegisteringGardenRazorThenServicesAreResolvable()
    {
        // Given
        ServiceCollection services = new();
        services.AddLogging();
        services.AddLocalization();
        services.AddMudServices();
        services.AddGardenRazor();
        services.AddDesignSystem();

        // When
        using ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        using IServiceScope scope = provider.CreateScope();

        // Then
        MudBlazor.IDialogService? dialogService = scope.ServiceProvider.GetService<MudBlazor.IDialogService>();
        ExceptionPresenter? presenter = scope.ServiceProvider.GetService<ExceptionPresenter>();
        DesignSystem? designSystem = scope.ServiceProvider.GetService<DesignSystem>();

        dialogService.Should().NotBeNull();
        presenter.Should().NotBeNull();
        designSystem.Should().NotBeNull();
    }

    [Fact]
    public void GivenSubpathRelayUrlWhenComposingHubUriThenOrchidSubpathIsPreserved()
    {
        // Given
        const string relayUrl = "https://stag.botanicsoft.com/orchid";

        // When
        Uri hubUri = new(new Uri(relayUrl.TrimEnd('/') + "/"), "hubs/clipboard");

        // Then
        hubUri.AbsoluteUri.Should().Be("https://stag.botanicsoft.com/orchid/hubs/clipboard");
    }
}
