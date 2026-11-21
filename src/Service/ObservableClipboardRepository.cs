using Orchid.Core.Domain.Entity;
using Orchid.Core.Storage;

namespace Orchid.Desktop.Service;

/// <summary>
/// 로컬 클립보드 저장소 변경(<see cref="UpsertAsync"/>, <see cref="DeleteAsync"/>) 완료 직후 UI 갱신 이벤트를 발행하는 데코레이터입니다.
/// </summary>
public sealed class ObservableClipboardRepository : IClipboardRepository
{
    private readonly IClipboardRepository _inner;

    /// <summary>
    /// <see cref="ObservableClipboardRepository"/> 클래스의 새 인스턴스를 초기화합니다.
    /// </summary>
    /// <param name="inner">실제 영속화를 수행할 내부 저장소</param>
    public ObservableClipboardRepository(IClipboardRepository inner)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    }

    /// <summary>
    /// 저장소에 클립보드 항목이 추가·갱신·삭제된 직후 발생하는 이벤트입니다.
    /// </summary>
    public event EventHandler? Changed;

    /// <inheritdoc />
    public Task InitializeAsync()
    {
        return _inner.InitializeAsync();
    }

    /// <inheritdoc />
    public async Task UpsertAsync(ClipboardItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        await
            _inner.UpsertAsync(item)
            .ConfigureAwait(false);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc />
    public Task<ClipboardItem?> GetByIdAsync(Guid id)
    {
        return _inner.GetByIdAsync(id);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<ClipboardItem>> GetRecentAsync(int skip = 0, int take = 50)
    {
        return _inner.GetRecentAsync(skip, take);
    }

    /// <inheritdoc />
    public Task<bool> ExistsByHashAsync(string payloadHash)
    {
        return _inner.ExistsByHashAsync(payloadHash);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid id)
    {
        await
            _inner.DeleteAsync(id)
            .ConfigureAwait(false);
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
