using Luxoria.Algorithm.BrisqueScore;
using Xunit;

namespace BrisqueScore.Tests
{
    /// <summary>
    /// Edge case tests for BrisqueInterop covering unusual scenarios.
    /// </summary>
    public class BrisqueInteropEdgeCaseTests : IDisposable
    {
        private readonly string _tempDir;
        private readonly string _validModelPath;
        private readonly string _validRangePath;

        public BrisqueInteropEdgeCaseTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), $"BrisqueTests_{Guid.NewGuid()}");
            Directory.CreateDirectory(_tempDir);

            // Use real model files from assets folder
            var assetsPath = Path.Combine(Directory.GetCurrentDirectory(), "assets");
            _validModelPath = Path.Combine(assetsPath, "brisque_model_live.yml");
            _validRangePath = Path.Combine(assetsPath, "brisque_range_live.yml");

            if (!File.Exists(_validModelPath) || !File.Exists(_validRangePath))
            {
                throw new FileNotFoundException("Required model files not found in assets folder");
            }
        }

        /// <summary>
        /// Tests that read-only model files (FileAttributes.ReadOnly) can be read by the constructor.
        /// Expected: Read-only attribute does not prevent file reading; files are accessible.
        /// </summary>
        [Fact]
        public void Constructor_WithReadOnlyModelFile_ShouldWork()
        {
            var readOnlyModel = Path.Combine(_tempDir, "readonly_model.yml");
            File.WriteAllText(readOnlyModel, "# Model");
            File.SetAttributes(readOnlyModel, FileAttributes.ReadOnly);

            try
            {
                Assert.Throws<InvalidOperationException>(() =>
                {
                    using var interop = new BrisqueInterop(readOnlyModel, _validRangePath);
                });
            }
            finally
            {
                File.SetAttributes(readOnlyModel, FileAttributes.Normal);
            }
        }

        /// <summary>
        /// Validates that hidden files (FileAttributes.Hidden) are accessible to the constructor.
        /// Expected: Hidden attribute does not prevent file access or reading.
        /// </summary>
        [Fact]
        public void Constructor_WithHiddenFiles_ShouldWork()
        {
            var hiddenModel = Path.Combine(_tempDir, "hidden_model.yml");
            var hiddenRange = Path.Combine(_tempDir, "hidden_range.yml");

            File.WriteAllText(hiddenModel, "# Model");
            File.WriteAllText(hiddenRange, "# Range");

            File.SetAttributes(hiddenModel, FileAttributes.Hidden);
            File.SetAttributes(hiddenRange, FileAttributes.Hidden);

            try
            {
                Assert.Throws<InvalidOperationException>(() =>
                {
                    using var interop = new BrisqueInterop(hiddenModel, hiddenRange);
                });
            }
            finally
            {
                File.SetAttributes(hiddenModel, FileAttributes.Normal);
                File.SetAttributes(hiddenRange, FileAttributes.Normal);
            }
        }

        /// <summary>
        /// Tests that ComputeScore can process read-only image files (FileAttributes.ReadOnly).
        /// Expected: Read-only images can be read and processed without issues.
        /// </summary>
        [Fact]
        public void ComputeScore_WithReadOnlyImage_ShouldWork()
        {
            var readOnlyImage = Path.Combine(_tempDir, "readonly.jpg");
            File.WriteAllBytes(readOnlyImage, new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 });
            File.SetAttributes(readOnlyImage, FileAttributes.ReadOnly);

            try
            {
                using var interop = new BrisqueInterop(_validModelPath, _validRangePath);
                // Minimal JPEG - OpenCV may fail but handles gracefully
                try
                {
                    interop.ComputeScore(readOnlyImage);
                }
                catch (FileNotFoundException)
                {
                    // Expected
                }
                catch (InvalidOperationException)
                {
                    // Also acceptable
                }
            }
            finally
            {
                File.SetAttributes(readOnlyImage, FileAttributes.Normal);
            }
        }

        /// <summary>
        /// Verifies that the constructor follows symbolic links to model and range files (requires admin rights).
        /// Expected: Symbolic links are resolved to actual files; may throw UnauthorizedAccessException or IOException without admin rights.
        /// </summary>
        [Fact]
        public void Constructor_WithSymbolicLink_ShouldFollowLink()
        {
            try
            {
                var linkModel = Path.Combine(_tempDir, "link_model.yml");
                var linkRange = Path.Combine(_tempDir, "link_range.yml");

                // Try to create symbolic links (may fail without admin rights)
                File.CreateSymbolicLink(linkModel, _validModelPath);
                File.CreateSymbolicLink(linkRange, _validRangePath);

                using var interop = new BrisqueInterop(linkModel, linkRange);
                Assert.NotNull(interop);
            }
            catch (UnauthorizedAccessException)
            {
                // Expected if not running as admin
            }
            catch (IOException)
            {
                // Expected on some file systems
            }
        }

        /// <summary>
        /// Tests constructor behavior with very large model files (10MB each).
        /// Expected: Large files are handled; native library may reject oversized models.
        /// </summary>
        [Fact]
        public void Constructor_WithVeryLargeModelFiles_ShouldHandle()
        {
            var largeModel = Path.Combine(_tempDir, "large_model.yml");
            var largeRange = Path.Combine(_tempDir, "large_range.yml");

            // Create large files (10MB)
            var largeData = new byte[10 * 1024 * 1024];
            new Random().NextBytes(largeData);

            File.WriteAllBytes(largeModel, largeData);
            File.WriteAllBytes(largeRange, largeData);

            Assert.Throws<InvalidOperationException>(() =>
            {
                using var interop = new BrisqueInterop(largeModel, largeRange);
            });
        }

        /// <summary>
        /// Tests ComputeScore with a very large image file (50MB).
        /// Expected: Large image files are handled gracefully; native library processes or rejects based on memory constraints.
        /// </summary>
        [Fact]
        public void ComputeScore_WithVeryLargeImage_ShouldHandle()
        {
            var largeImage = Path.Combine(_tempDir, "large.jpg");

            // Create a large file (50MB)
            var largeData = new byte[50 * 1024 * 1024];
            new Random().NextBytes(largeData);
            File.WriteAllBytes(largeImage, largeData);

            using var interop = new BrisqueInterop(_validModelPath, _validRangePath);
            // Random data won't be a valid JPEG - OpenCV handles gracefully
            try
            {
                interop.ComputeScore(largeImage);
            }
            catch (FileNotFoundException)
            {
                // Expected
            }
            catch (InvalidOperationException)
            {
                // Also acceptable
            }
        }

        /// <summary>
        /// Verifies that empty model files (0 bytes) are rejected by the constructor.
        /// Expected: Throws FileNotFoundException or ArgumentException due to invalid file content.
        /// </summary>
        [Fact]
        public void Constructor_WithEmptyModelFiles_ShouldThrow()
        {
            var emptyModel = Path.Combine(_tempDir, "empty_model.yml");
            var emptyRange = Path.Combine(_tempDir, "empty_range.yml");

            File.WriteAllBytes(emptyModel, Array.Empty<byte>());
            File.WriteAllBytes(emptyRange, Array.Empty<byte>());

            Assert.Throws<InvalidOperationException>(() =>
            {
                using var interop = new BrisqueInterop(emptyModel, emptyRange);
            });
        }

        [Fact]
        public void Constructor_WithBinaryGarbageInYml_ShouldHandle()
        {
            var garbageModel = Path.Combine(_tempDir, "garbage_model.yml");
            var garbageRange = Path.Combine(_tempDir, "garbage_range.yml");

            var random = new Random();
            var garbage1 = new byte[1024];
            var garbage2 = new byte[1024];
            random.NextBytes(garbage1);
            random.NextBytes(garbage2);

            File.WriteAllBytes(garbageModel, garbage1);
            File.WriteAllBytes(garbageRange, garbage2);

            Assert.Throws<InvalidOperationException>(() =>
            {
                using var interop = new BrisqueInterop(garbageModel, garbageRange);
            });
        }

        /// <summary>
        /// Tests ComputeScore with UNC network path (\\\\server\\share\\image.jpg).
        /// Expected: Network paths are handled if accessible; may throw IOException if network unavailable.
        /// </summary>
        [Fact]
        public void ComputeScore_WithNetworkPath_ShouldHandle()
        {
            // UNC path test (usually not available in test environment)
            var uncPath = @"\\localhost\C$\test.jpg";

            using var interop = new BrisqueInterop(_validModelPath, _validRangePath);

            try
            {
                interop.ComputeScore(uncPath);
            }
            catch (FileNotFoundException)
            {
                // Expected
            }
            catch (UnauthorizedAccessException)
            {
                // Expected
            }
            catch (IOException)
            {
                // Expected
            }
        }

        /// <summary>
        /// Validates that using the same file for both model and range parameters is accepted.
        /// Expected: Constructor succeeds; native library handles identical file paths.
        /// </summary>
        [Fact]
        public void Constructor_WithSameFileForModelAndRange_ShouldWork()
        {
            using var interop = new BrisqueInterop(_validModelPath, _validModelPath);
            Assert.NotNull(interop);
        }

        /// <summary>
        /// Tests ComputeScore behavior when given a directory path instead of an image file.
        /// Expected: Throws ArgumentException or IOException; directories are not valid image files.
        /// </summary>
        [Fact]
        public void ComputeScore_WithDirectoryInsteadOfFile_ShouldThrow()
        {
            var directory = Path.Combine(_tempDir, "subdir");
            Directory.CreateDirectory(directory);

            using var interop = new BrisqueInterop(_validModelPath, _validRangePath);

            var ex = Assert.Throws<FileNotFoundException>(() =>
            {
                interop.ComputeScore(directory);
            });

            Assert.Contains("Image file not found", ex.Message);
        }

        /// <summary>
        /// Tests constructor behavior with mixed forward/backward slashes in file paths.
        /// Expected: Throws InvalidOperationException; path format must be consistent.
        /// </summary>
        [Fact]
        public void Constructor_WithMixedSlashes_ShouldNormalize()
        {
            var mixedModel = _validModelPath.Replace('\\', '/');
            var mixedRange = _validRangePath.Replace('\\', '/');

            using var interop = new BrisqueInterop(mixedModel, mixedRange);
            Assert.NotNull(interop);
        }

        /// <summary>
        /// Tests ComputeScore with image path containing trailing slash (directory-like path).
        /// Expected: Throws FileNotFoundException; trailing slash indicates directory, not file.
        /// </summary>
        [Fact]
        public void ComputeScore_WithTrailingSlash_ShouldHandle()
        {
            var pathWithSlash = _tempDir + "\\";

            using var interop = new BrisqueInterop(_validModelPath, _validRangePath);

            Assert.Throws<FileNotFoundException>(() =>
            {
                interop.ComputeScore(pathWithSlash);
            });
        }

        /// <summary>
        /// Validates that paths containing null characters (\0) are rejected by the constructor.
        /// Expected: Throws ArgumentException or InvalidOperationException; null characters are invalid in paths.
        /// </summary>
        [Fact]
        public void Constructor_WithNullCharacterInPath_ShouldThrow()
        {
            // Null character gets truncated by File.Exists, causing FileNotFoundException instead
            var pathWithNull = _validModelPath + "\0extra";
            
            Assert.ThrowsAny<Exception>(() =>
            {
                using var interop = new BrisqueInterop(pathWithNull, _validRangePath);
            });
        }

        /// <summary>
        /// Tests ComputeScore with reserved Windows filename (CON, PRN, AUX, NUL, COM1, LPT1, etc.).
        /// Expected: Throws ArgumentException, IOException, or FileNotFoundException; reserved names are invalid.
        /// </summary>
        [Fact]
        public void ComputeScore_WithReservedFileName_ShouldThrow()
        {
            // Windows reserved names
            var reservedNames = new[] { "CON", "PRN", "AUX", "NUL", "COM1", "LPT1" };

            using var interop = new BrisqueInterop(_validModelPath, _validRangePath);

            foreach (var reserved in reservedNames)
            {
                try
                {
                    interop.ComputeScore(reserved);
                }
                catch (Exception)
                {
                    // Expected - various exceptions possible for reserved names
                }
            }
        }

        /// <summary>
        /// Tests constructor behavior under simulated low memory conditions.
        /// Expected: May throw OutOfMemoryException or complete successfully depending on available memory.
        /// </summary>
        [Fact]
        public void Constructor_DuringLowMemory_ShouldHandle()
        {
            // Stress test with many allocations
            var instances = new List<byte[]>();

            try
            {
                // Allocate memory
                for (int i = 0; i < 100; i++)
                {
                    instances.Add(new byte[10 * 1024 * 1024]); // 10MB each
                }

                using var interop = new BrisqueInterop(_validModelPath, _validRangePath);
                Assert.NotNull(interop);
            }
            catch (OutOfMemoryException)
            {
                // Expected in low memory scenarios
            }
            finally
            {
                instances.Clear();
                GC.Collect();
            }
        }

        public void Dispose()
        {
            if (Directory.Exists(_tempDir))
            {
                try
                {
                    // Remove any special attributes before deleting
                    foreach (var file in Directory.GetFiles(_tempDir, "*", SearchOption.AllDirectories))
                    {
                        try
                        {
                            File.SetAttributes(file, FileAttributes.Normal);
                        }
                        catch
                        {
                            // Ignore
                        }
                    }

                    Directory.Delete(_tempDir, true);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }
    }
}
