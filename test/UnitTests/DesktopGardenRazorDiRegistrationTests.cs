using FluentAssertions;
using Garden.Razor;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using Xunit;

namespace Orchid.UnitTests.Desktop;

public sealed class DesktopGardenRazorDiRegistrationTests
{
    [Fact]
    public void GivenDesktopServiceCollection_WhenRegisteringGardenRazor_ThenServicesAreResolvable()
    {
        // Given
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddLocalization();
        services.AddMudServices();
        services.AddGardenRazor();
        services.AddDesignSystem();

        // When
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        using var scope = provider.CreateScope();

        // Then
        var dialogService = scope.ServiceProvider.GetService<MudBlazor.IDialogService>();
        var presenter = scope.ServiceProvider.GetService<ExceptionPresenter>();
        var designSystem = scope.ServiceProvider.GetService<DesignSystem>();

        dialogService.Should().NotBeNull();
        presenter.Should().NotBeNull();
        designSystem.Should().NotBeNull();
    }
}
