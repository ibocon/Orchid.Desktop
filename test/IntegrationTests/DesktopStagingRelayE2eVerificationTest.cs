// keep: FluentAssertions assertions
using FluentAssertions;
// keep: Orchid Core abstractions
using Orchid.Core.Abstraction;
// keep: Orchid Core cryptography
using Orchid.Core.Cryptography;
// keep: Orchid Core storage
using Orchid.Core.Storage;
// keep: Orchid Core sync coordinator and engine
using Orchid.Core.Sync;
// keep: Orchid Core relay types
using Orchid.Core.Sync.Relay;
// keep: xUnit Fact attribute
using Xunit;

namespace Orchid.IntegrationTests.Desktop;

/// <summary>
/// 로컬 개발환경에서 Hanul Staging(/orchid) SignalR 허브로 연결해 E2EE 클립보드 왕복 동기화를 검증하는 통합 테스트입니다.
/// </summary>
public sealed class DesktopStagingRelayE2eVerificationTest
{
    private const string StagingHubUrl = "https://stag.botanicsoft.com/orchid/hubs/clipboard";

    [Fact]
    public async Task TwoCoordinatorsConnectedToStagingRelayShouldRoundTripEncryptedClipboardText()
    {
        // Given
        string tempDir = Path.Combine(Path.GetTempPath(), $"orchid-stag-e2e-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            PairingKey pairingKey = PairingKey.Generate();
            InMemoryClipboard senderClipboard = new("Orchid-Staging-E2EE-RoundTrip-" + Guid.NewGuid().ToString("N"));
            InMemoryClipboard receiverClipboard = new(string.Empty);
            ManualClipboardChangeSource senderChangeSource = new();
            NullClipboardChangeSource receiverChangeSource = new();

            ISyncEngine senderEngine = new SyncEngine(
                new AesGcmCryptoService(),
                new SqliteClipboardRepository(Path.Combine(tempDir, "sender.db")));
            ISyncEngine receiverEngine = new SyncEngine(
                new AesGcmCryptoService(),
                new SqliteClipboardRepository(Path.Combine(tempDir, "receiver.db")));

            await using SignalRClipboardRelayClient senderRelay = new(new RelayOptions(new Uri(StagingHubUrl)));
            await using SignalRClipboardRelayClient receiverRelay = new(new RelayOptions(new Uri(StagingHubUrl)));

            await using ClipboardSyncCoordinator senderCoordinator = new(
                senderEngine,
                senderClipboard,
                senderChangeSource,
                senderRelay);
            await using ClipboardSyncCoordinator receiverCoordinator = new(
                receiverEngine,
                receiverClipboard,
                receiverChangeSource,
                receiverRelay);

            using CancellationTokenSource cts = new(TimeSpan.FromSeconds(25));
            await receiverCoordinator.StartAsync(pairingKey, Guid.NewGuid(), cts.Token);
            await senderCoordinator.StartAsync(pairingKey, Guid.NewGuid(), cts.Token);

            // When
            string expectedText = senderClipboard.CurrentText!;
            await senderChangeSource.RaiseChangedAsync();
            string actualReceived = await receiverClipboard.WaitForTextAsync(cts.Token);

            // Then
            // ✅ 로컬 송신 코디네이터가 암호화한 텍스트가 Staging SignalR 허브를 거쳐 수신 코디네이터에서 정확히 복호화된다
            actualReceived.Should().Be(expectedText);
        }
        finally
        {
            try
            {
                Directory.Delete(tempDir, recursive: true);
            }
            catch
            {
                // SQLite connection pool file lock cleanup is best-effort in temp directory
            }
        }
    }

    private sealed class InMemoryClipboard(string initialText) : ISystemClipboard
    {
        private readonly TaskCompletionSource<string> _receivedTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public string? CurrentText { get; private set; } = initialText;

        public Task<string?> GetTextAsync() => Task.FromResult(CurrentText);

        public Task SetTextAsync(string text)
        {
            CurrentText = text;
            _receivedTcs.TrySetResult(text);
            return Task.CompletedTask;
        }

        public Task<byte[]?> GetImageAsync() => Task.FromResult<byte[]?>(null);

        public Task SetImageAsync(byte[] imageBytes) => Task.CompletedTask;

        public async Task<string> WaitForTextAsync(CancellationToken cancellationToken)
        {
            using CancellationTokenRegistration registration = cancellationToken.Register(
                () => _receivedTcs.TrySetCanceled(cancellationToken));
            return await _receivedTcs.Task.ConfigureAwait(false);
        }
    }

    private sealed class ManualClipboardChangeSource : IClipboardChangeSource
    {
        public event Func<Task>? Changed;

        public void Start()
        {
        }

        public Task RaiseChangedAsync() => Changed?.Invoke() ?? Task.CompletedTask;
    }
}
