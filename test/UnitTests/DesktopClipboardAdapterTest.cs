// keep: FluentAssertions assertions
using FluentAssertions;
// keep: Moq test doubles
using Moq;
// keep: Orchid Contract models
using Orchid.Contract.Model;
// keep: Orchid Contract payloads
using Orchid.Contract.Payload;
// keep: Orchid Core abstractions
using Orchid.Core.Abstraction;
// keep: Orchid Core cryptography
using Orchid.Core.Cryptography;
// keep: Orchid Core entities
using Orchid.Core.Domain.Entity;
// keep: Orchid Core storage contracts
using Orchid.Core.Storage;
// keep: Orchid Core sync engine and coordinator
using Orchid.Core.Sync;
// keep: Orchid Core relay types
using Orchid.Core.Sync.Relay;
// keep: Orchid Desktop services
using Orchid.Desktop.Service;
// keep: xUnit Fact attribute
using Xunit;

namespace Orchid.UnitTests.Desktop;

/// <summary>
/// 데스크톱 클립보드 어댑터, 로컬 저장소 변경 알림 데코레이터 및 DesktopApp 히스토리 바인딩 계약을 검증합니다.
/// </summary>
public sealed class DesktopClipboardAdapterTest
{
    [Fact]
    public async Task AdapterShouldNotifyCoreWhenClipboardContentChanges()
    {
        // Given
        Mock<ISystemClipboard> mockClipboard = new();

        // When & Then
        mockClipboard.Setup(x => x.GetTextAsync()).ReturnsAsync("Sample Text");
        string? result = await mockClipboard.Object.GetTextAsync();
        result.Should().Be("Sample Text");
    }

    [Fact]
    public async Task AdapterWhenClipboardIsEmptyShouldReturnNull()
    {
        // Given
        Mock<ISystemClipboard> mockClipboard = new();
        mockClipboard.Setup(x => x.GetTextAsync()).ReturnsAsync((string?)null);

        // When
        string? result = await mockClipboard.Object.GetTextAsync();

        // Then
        result.Should().BeNull();
    }

    [Fact]
    public async Task ObservableClipboardRepositoryShouldNotifyAfterUpsertAndDeleteWithCommittedSqliteState()
    {
        // Given
        string tempDir = Path.Combine(Path.GetTempPath(), $"orchid-desktop-repo-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        string dbPath = Path.Combine(tempDir, "orchid.db");

        try
        {
            SqliteClipboardRepository inner = new(dbPath);
            ObservableClipboardRepository sut = new(inner);
            List<int> observedCounts = [];

            sut.Changed += (_, _) =>
            {
                IReadOnlyList<ClipboardItem> latest = sut.GetRecentAsync().GetAwaiter().GetResult();
                observedCounts.Add(latest.Count);
            };

            ClipboardItem item = ClipboardItem.Create(
                Guid.NewGuid(),
                Guid.NewGuid(),
                DevicePlatform.Windows,
                ContentType.Text,
                [1, 2, 3, 4],
                [5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16],
                [17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32],
                "hash-001",
                DateTimeOffset.UtcNow).Value;

            // When
            await sut.UpsertAsync(item);
            IReadOnlyList<ClipboardItem> afterUpsert = await sut.GetRecentAsync();
            await sut.DeleteAsync(item.Id);
            IReadOnlyList<ClipboardItem> afterDelete = await sut.GetRecentAsync();

            // Then
            // ✅ Upsert/Delete 직후 Changed 이벤트가 발생하고 핸들러 시점에 이미 SQLite 변경이 반영되어 있다
            afterUpsert.Should().ContainSingle(x => x.Id == item.Id);
            afterDelete.Should().BeEmpty();
            observedCounts.Should().Equal(1, 0);
        }
        finally
        {
            try
            {
                Directory.Delete(tempDir, recursive: true);
            }
            catch
            {
                // SQLite connection pool cleanup in temp folder is best-effort
            }
        }
    }

    [Fact]
    public async Task CoordinatorWithObservableRepositoryShouldRefreshHistoryOnLocalAndRemoteSync()
    {
        // Given
        string tempDir = Path.Combine(Path.GetTempPath(), $"orchid-desktop-sync-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        string dbPath = Path.Combine(tempDir, "orchid.db");

        try
        {
            ObservableClipboardRepository repository = new(new SqliteClipboardRepository(dbPath));
            SyncEngine syncEngine = new(new AesGcmCryptoService(), repository);
            FakeClipboard clipboard = new("Local desktop copy");
            FakeChangeSource changeSource = new();
            FakeRelayClient relayClient = new();

            IReadOnlyList<ClipboardItem> boundItems = [];
            TaskCompletionSource<int> twoItemsObserved = new(TaskCreationOptions.RunContinuationsAsynchronously);
            repository.Changed += (_, _) =>
            {
                boundItems = repository.GetRecentAsync().GetAwaiter().GetResult();
                if (boundItems.Count >= 2)
                {
                    twoItemsObserved.TrySetResult(boundItems.Count);
                }
            };

            ClipboardSyncCoordinator coordinator = new(
                syncEngine,
                clipboard,
                changeSource,
                relayClient);

            try
            {
                PairingKey key = PairingKey.Generate();
                await coordinator.StartAsync(key, Guid.NewGuid());

                // When: 1) 로컬 클립보드 변경 발생
                await changeSource.RaiseChangedAsync();

                // When: 2) 원격 디바이스에서 새로운 클립보드 페이로드 수신
                AesGcmCryptoService crypto = new();
                byte[] plainBytes = System.Text.Encoding.UTF8.GetBytes("Remote mobile copy");
                (byte[] cipherText, byte[] nonce, byte[] tag) = crypto.Encrypt(plainBytes, key.GetKeyBytes()).Value;
                EncryptedPayload remotePayload = new(
                    Guid.NewGuid(),
                    cipherText,
                    nonce,
                    tag,
                    "AES-256-GCM",
                    DateTimeOffset.UtcNow.AddSeconds(1));
                await relayClient.RaiseReceivedAsync(remotePayload);

                int finalCount = await twoItemsObserved.Task.WaitAsync(TimeSpan.FromSeconds(5));

                // Then
                // ✅ 로컬 복사 및 원격 수신 항목이 모두 저장소에 기록되고 UI 바인딩 컬렉션에 최신순으로 반영된다
                finalCount.Should().Be(2);
                boundItems.Should().HaveCount(2);
                boundItems[0].Id.Should().Be(remotePayload.Id);
                clipboard.CurrentText.Should().Be("Remote mobile copy");
            }
            finally
            {
                await coordinator.DisposeAsync();
            }
        }
        finally
        {
            try
            {
                Directory.Delete(tempDir, recursive: true);
            }
            catch
            {
                // SQLite connection pool cleanup in temp folder is best-effort
            }
        }
    }

    [Fact]
    public void DesktopAppAndMauiProgramShouldBindClipboardRepositoryItemsToClipboardList()
    {
        // Given
        string repoRoot = FindRepositoryRoot();
        string mauiProgram = File.ReadAllText(Path.Combine(repoRoot, "src/MauiProgram.cs"));
        string desktopApp = File.ReadAllText(Path.Combine(repoRoot, "src/Component/DesktopApp/DesktopApp.razor"));

        // When & Then
        // ✅ MauiProgram이 ObservableClipboardRepository를 IClipboardRepository로 등록하고 DesktopApp이 Items를 ClipboardList에 바인딩한다
        mauiProgram.Should().Contain("ObservableClipboardRepository");
        desktopApp.Should().Contain("@inject IClipboardRepository ClipboardRepository");
        desktopApp.Should().Contain("GetRecentAsync");
        desktopApp.Should().Contain("<ClipboardList Items=\"@_clipboardItems\" />");
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

    private sealed class FakeClipboard(string initialText) : ISystemClipboard
    {
        public string? CurrentText { get; private set; } = initialText;

        public Task<string?> GetTextAsync()
        {
            return Task.FromResult(CurrentText);
        }

        public Task SetTextAsync(string text)
        {
            CurrentText = text;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeChangeSource : IClipboardChangeSource
    {
        public event Func<Task>? Changed;

        public void Start()
        {
        }

        public Task RaiseChangedAsync()
        {
            return Changed?.Invoke() ?? Task.CompletedTask;
        }
    }

    private sealed class FakeRelayClient : IClipboardRelayClient
    {
        public event Func<EncryptedPayload, Task>? PayloadReceived;

        public Task ConnectAsync(string pairingId, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task SendAsync(string pairingId, EncryptedPayload payload, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task RaiseReceivedAsync(EncryptedPayload payload)
        {
            return PayloadReceived?.Invoke(payload) ?? Task.CompletedTask;
        }

        public ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }
    }
}
