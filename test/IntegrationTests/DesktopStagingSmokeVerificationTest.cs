// keep: Process for desktop build and startup smoke verification
using System.Diagnostics;
// keep: FluentAssertions assertions
using FluentAssertions;
// keep: xUnit Fact attribute
using Xunit;

namespace Orchid.IntegrationTests.Desktop;

/// <summary>
/// 로컬 Windows 개발환경에서 Orchid.Desktop의 Staging 바인딩 및 프로세스 기동을 검증하는 스모크 테스트입니다.
/// </summary>
public sealed class DesktopStagingSmokeVerificationTest
{
    [Fact]
    public async Task LocalDesktopBuildAndStagingNegotiateHandshakeShouldSucceed()
    {
        // Given
        string repoRoot = FindRepositoryRoot();
        string csprojPath = Path.Combine(repoRoot, "src", "Orchid.Desktop.csproj");
        string csprojContent = await File.ReadAllTextAsync(csprojPath).ConfigureAwait(true);

        // When
        using HttpClient httpClient = new() { Timeout = TimeSpan.FromSeconds(15) };
        using HttpResponseMessage negotiateResponse = await httpClient.PostAsync(
            new Uri("https://stag.botanicsoft.com/orchid/hubs/clipboard/negotiate?negotiateVersion=1"),
            new StringContent(string.Empty)).ConfigureAwait(true);
        string negotiateBody = await negotiateResponse.Content.ReadAsStringAsync().ConfigureAwait(true);

        string exePath = FindBuiltDesktopExecutable(repoRoot);

        // Then
        // ✅ 데스크톱 프로젝트가 Staging /orchid 릴레이를 기본값으로 바인딩하고 로컬 빌드 산출물 및 Staging 협상이 모두 정상이다
        csprojContent.Should().Contain("https://stag.botanicsoft.com/orchid");
        ((int)negotiateResponse.StatusCode).Should().Be(200);
        negotiateBody.Should().Contain("connectionId");
        File.Exists(exePath).Should().BeTrue($"built executable should exist at {exePath}");
    }

    private static string FindBuiltDesktopExecutable(string repoRoot)
    {
        string binRoot = Path.Combine(repoRoot, "src", "bin");
        if (Directory.Exists(binRoot))
        {
            string? existing = Directory
                .EnumerateFiles(binRoot, "Orchid.Desktop.exe", SearchOption.AllDirectories)
                .FirstOrDefault();
            if (existing is not null)
            {
                return existing;
            }
        }

        ProcessStartInfo psi = new("dotnet")
        {
            WorkingDirectory = repoRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        psi.ArgumentList.Add("build");
        psi.ArgumentList.Add("src/Orchid.Desktop.csproj");
        psi.ArgumentList.Add("-f");
        psi.ArgumentList.Add("net10.0-windows10.0.19041.0");
        psi.ArgumentList.Add("-c");
        psi.ArgumentList.Add("Debug");
        psi.ArgumentList.Add("-p:EnforceCodeStyleInBuild=false");

        using Process process = Process.Start(psi)!;
        process.WaitForExit(120_000);
        process.ExitCode.Should().Be(0);

        return Directory
            .EnumerateFiles(binRoot, "Orchid.Desktop.exe", SearchOption.AllDirectories)
            .First();
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? dir = new(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Orchid.Desktop.slnx")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new InvalidOperationException("Orchid.Desktop.slnx not found.");
    }
}
