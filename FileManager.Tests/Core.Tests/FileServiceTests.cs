using FileManager.Core.Services;
using Xunit;

namespace FileManager.Tests.Core.Tests
{
    public class FileServiceTests : IDisposable
    {
        private readonly FileService _service;
        private readonly string _testDir;

        public FileServiceTests()
        {
            _service = new FileService();
            _testDir = Path.Combine(Path.GetTempPath(), "FileManagerTests_" + Guid.NewGuid());
            Directory.CreateDirectory(_testDir);
        }

        [Fact]
        public async Task ListDirectoryAsync_ReturnsItems_WhenDirectoryExists()
        {
            // Arrange
            File.WriteAllText(Path.Combine(_testDir, "test.txt"), "hello");
            Directory.CreateDirectory(Path.Combine(_testDir, "subdir"));

            // Act
            var items = (await _service.ListDirectoryAsync(_testDir)).ToList();

            // Assert
            Assert.Contains(items, i => i.Name == "test.txt" && !i.IsDirectory);
            Assert.Contains(items, i => i.Name == "subdir" && i.IsDirectory);
        }

        [Fact]
        public async Task CreateFolderAsync_CreatesFolder_WhenValid()
        {
            await _service.CreateFolderAsync(_testDir, "NuevaCarpeta");
            Assert.True(Directory.Exists(Path.Combine(_testDir, "NuevaCarpeta")));
        }

        [Fact]
        public async Task RenameAsync_RenamesFile_WhenValid()
        {
            var original = Path.Combine(_testDir, "original.txt");
            File.WriteAllText(original, "data");

            await _service.RenameAsync(original, "renombrado.txt");

            Assert.False(File.Exists(original));
            Assert.True(File.Exists(Path.Combine(_testDir, "renombrado.txt")));
        }

        [Fact]
        public async Task RenameAsync_Throws_WhenDestinationFileAlreadyExists()
        {
            var original = Path.Combine(_testDir, "original.txt");
            var existing = Path.Combine(_testDir, "existing.txt");
            File.WriteAllText(original, "data");
            File.WriteAllText(existing, "other");

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.RenameAsync(original, "existing.txt"));
            Assert.Contains("ya existe otro archivo", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task DeleteAsync_DeletesFile_WhenFileExists()
        {
            var file = Path.Combine(_testDir, "borrar.txt");
            File.WriteAllText(file, "borrar");

            await _service.DeleteAsync(file);

            Assert.False(File.Exists(file));
        }

        [Fact]
        public async Task CopyAsync_CopiesFile_WhenValid()
        {
            var source = Path.Combine(_testDir, "fuente.txt");
            var dest = Path.Combine(_testDir, "destino.txt");
            File.WriteAllText(source, "contenido");

            await _service.CopyAsync(source, dest);

            Assert.True(File.Exists(dest));
            Assert.Equal("contenido", File.ReadAllText(dest));
        }

        [Fact]
        public async Task SearchAsync_FindsFile_ByPattern()
        {
            File.WriteAllText(Path.Combine(_testDir, "documento_2024.txt"), "x");
            File.WriteAllText(Path.Combine(_testDir, "foto.jpg"), "x");

            var results = (await _service.SearchAsync(_testDir, "documento")).ToList();

            Assert.Single(results);
            Assert.Equal("documento_2024.txt", results[0].Name);
        }

        [Fact]
        public async Task Favorites_AddAndRemove_WorkCorrectly()
        {
            await _service.AddFavoriteAsync(_testDir);
            var favs = (await _service.GetFavoritesAsync()).ToList();
            Assert.Contains(favs, f => f == _testDir);

            await _service.RemoveFavoriteAsync(_testDir);
            favs = (await _service.GetFavoritesAsync()).ToList();
            Assert.DoesNotContain(favs, f => f == _testDir);
        }

        public void Dispose()
        {
            _service.Dispose();
            if (Directory.Exists(_testDir))
                Directory.Delete(_testDir, recursive: true);
        }
    }
}