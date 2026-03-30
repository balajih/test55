using Moq;
using YourProject.Caching;

namespace YourProject.Tests.Caching;

public class RequestCacheTests : IDisposable
{
    private readonly RequestCache _sut = new();

    public void Dispose() => _sut.Dispose();

    [Fact]
    public void Get_ReturnsDefault_WhenKeyMissing()
    {
        Assert.Null(_sut.Get<string>("missing"));
        Assert.Equal(0, _sut.Get<int>("missing"));
    }

    [Fact]
    public void Set_ThenGet_ReturnsValue()
    {
        _sut.Set("name", "Alice");

        Assert.Equal("Alice", _sut.Get<string>("name"));
    }

    [Fact]
    public void Set_OverwritesPreviousValue()
    {
        _sut.Set("key", "old");
        _sut.Set("key", "new");

        Assert.Equal("new", _sut.Get<string>("key"));
    }

    [Fact]
    public void GetOrAdd_CreatesWhenMissing_SkipsWhenExists()
    {
        var result1 = _sut.GetOrAdd("key", () => "created");
        var factoryCalled = false;
        var result2 = _sut.GetOrAdd("key", () => { factoryCalled = true; return "ignored"; });

        Assert.Equal("created", result1);
        Assert.Equal("created", result2);
        Assert.False(factoryCalled);
    }

    [Fact]
    public async Task GetOrAddAsync_CreatesWhenMissing_SkipsWhenExists()
    {
        var result1 = await _sut.GetOrAddAsync("key", () => Task.FromResult(42));
        var result2 = await _sut.GetOrAddAsync("key", () => Task.FromResult(99));

        Assert.Equal(42, result1);
        Assert.Equal(42, result2);
    }

    [Fact]
    public void ContainsKey_ReturnsTrueOnlyWhenPresent()
    {
        Assert.False(_sut.ContainsKey("key"));

        _sut.Set("key", 1);

        Assert.True(_sut.ContainsKey("key"));
    }

    [Fact]
    public void Remove_DeletesKey_ReturnsFalseWhenMissing()
    {
        _sut.Set("key", "value");

        Assert.True(_sut.Remove("key"));
        Assert.False(_sut.Remove("key"));
        Assert.False(_sut.ContainsKey("key"));
    }

    [Fact]
    public void Clear_RemovesAllEntries()
    {
        _sut.Set("a", 1);
        _sut.Set("b", 2);

        _sut.Clear();

        Assert.False(_sut.ContainsKey("a"));
        Assert.False(_sut.ContainsKey("b"));
    }

    [Fact]
    public void Dispose_CallsDisposeOnStoredDisposables()
    {
        var mockDisposable = new Mock<IDisposable>();
        _sut.Set("resource", mockDisposable.Object);

        _sut.Dispose();

        mockDisposable.Verify(d => d.Dispose(), Times.Once);
    }

    [Fact]
    public void Dispose_IsIdempotent()
    {
        _sut.Set("plain", "text");

        _sut.Dispose();
        _sut.Dispose(); // should not throw
    }

    [Fact]
    public void AllMethods_ThrowAfterDispose()
    {
        _sut.Dispose();

        Assert.Throws<ObjectDisposedException>(() => _sut.Get<string>("k"));
        Assert.Throws<ObjectDisposedException>(() => _sut.Set("k", "v"));
        Assert.Throws<ObjectDisposedException>(() => _sut.GetOrAdd("k", () => "v"));
        Assert.Throws<ObjectDisposedException>(() => _sut.Remove("k"));
        Assert.Throws<ObjectDisposedException>(() => _sut.ContainsKey("k"));
        Assert.Throws<ObjectDisposedException>(() => _sut.Clear());
    }

    [Fact]
    public void GetOrAdd_ThrowsOnNullFactory()
    {
        Assert.Throws<ArgumentNullException>(() => _sut.GetOrAdd<string>("k", null!));
    }

    [Fact]
    public async Task GetOrAddAsync_ThrowsOnNullFactory()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _sut.GetOrAddAsync<string>("k", null!));
    }
}
