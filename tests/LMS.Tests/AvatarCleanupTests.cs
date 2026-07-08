using FluentAssertions;
using LMS.Application.Common.Abstractions;
using LMS.Application.Common.Storage;
using Xunit;

namespace LMS.Tests;

public class AvatarCleanupTests
{
    /// <summary>Fake store that just records which stored names it was asked to delete.</summary>
    private sealed class RecordingStore : IAvatarFileStore
    {
        public List<string> Deleted { get; } = new();

        public Task<string> SaveAsync(Stream source, string originalFileName, string mimeType, CancellationToken ct) =>
            Task.FromResult("unused");

        public Task<bool> DeleteAsync(string storedFileName, CancellationToken ct)
        {
            Deleted.Add(storedFileName);
            return Task.FromResult(true);
        }
    }

    [Fact]
    public async Task Deletes_previous_local_file_when_replaced()
    {
        var store = new RecordingStore();
        await AvatarCleanup.DeletePreviousAsync(store, "/uploads/avatars/old.png", "/uploads/avatars/new.png", default);
        store.Deleted.Should().ContainSingle().Which.Should().Be("old.png");
    }

    [Fact]
    public async Task Deletes_previous_local_file_when_removed()
    {
        var store = new RecordingStore();
        await AvatarCleanup.DeletePreviousAsync(store, "/uploads/avatars/old.png", null, default);
        store.Deleted.Should().ContainSingle().Which.Should().Be("old.png");
    }

    [Fact]
    public async Task Skips_when_url_is_unchanged()
    {
        var store = new RecordingStore();
        await AvatarCleanup.DeletePreviousAsync(store, "/uploads/avatars/same.png", "/uploads/avatars/same.png", default);
        store.Deleted.Should().BeEmpty();
    }

    [Fact]
    public async Task Skips_external_urls()
    {
        var store = new RecordingStore();
        await AvatarCleanup.DeletePreviousAsync(store, "https://t.me/i/photo.jpg", null, default);
        store.Deleted.Should().BeEmpty();
    }

    [Fact]
    public async Task Skips_when_there_is_no_previous_avatar()
    {
        var store = new RecordingStore();
        await AvatarCleanup.DeletePreviousAsync(store, null, "/uploads/avatars/new.png", default);
        store.Deleted.Should().BeEmpty();
    }
}
