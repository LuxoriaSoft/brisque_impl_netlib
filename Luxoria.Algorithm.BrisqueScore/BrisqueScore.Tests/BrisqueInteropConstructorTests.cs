using Luxoria.Algorithm.BrisqueScore;
using Xunit;

namespace BrisqueScore.Tests
{
    /// <summary>
    /// Tests for BrisqueInterop constructor and initialization scenarios.
    /// </summary>
    public class BrisqueInteropConstructorTests : IDisposable
    {
        private readonly string _tempDir;
        private readonly string _validModelPath;
        private readonly string _validRangePath;

        public BrisqueInteropConstructorTests()
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
        /// Tests that the constructor with valid model and range file paths attempts to create an instance.
        /// Expected: Throws InvalidOperationException when native library is not loaded, but validates file paths first.
        /// </summary>
        [Fact]
        public void Constructor_WithValidPaths_ShouldCreateInstance()
        {
            // Verify that constructor succeeds with valid model files
            using var interop = new BrisqueInterop(_validModelPath, _validRangePath);
            Assert.NotNull(interop);
        }

        /// <summary>
        /// Validates that passing a null model path to the constructor throws FileNotFoundException.
        /// Expected: FileNotFoundException with message containing "Model file not found".
        /// </summary>
        [Fact]
        public void Constructor_WithNullModelPath_ShouldThrowFileNotFoundException()
        {
            var ex = Assert.Throws<FileNotFoundException>(() =>
            {
                using var interop = new BrisqueInterop(null!, _validRangePath);
            });

            Assert.Contains("Model file not found", ex.Message);
        }

        /// <summary>
        /// Validates that passing an empty string as model path throws FileNotFoundException.
        /// Expected: FileNotFoundException with message containing "Model file not found".
        /// </summary>
        [Fact]
        public void Constructor_WithEmptyModelPath_ShouldThrowFileNotFoundException()
        {
            var ex = Assert.Throws<FileNotFoundException>(() =>
            {
                using var interop = new BrisqueInterop("", _validRangePath);
            });

            Assert.Contains("Model file not found", ex.Message);
        }

        /// <summary>
        /// Validates that passing only whitespace as model path throws FileNotFoundException.
        /// Expected: FileNotFoundException with message containing "Model file not found".
        /// </summary>
        [Fact]
        public void Constructor_WithWhitespaceModelPath_ShouldThrowFileNotFoundException()
        {
            var ex = Assert.Throws<FileNotFoundException>(() =>
            {
                using var interop = new BrisqueInterop("   ", _validRangePath);
            });

            Assert.Contains("Model file not found", ex.Message);
        }

        /// <summary>
        /// Tests that providing a path to a non-existent model file throws FileNotFoundException.
        /// Expected: FileNotFoundException with the specific file path in the error message.
        /// </summary>
        [Fact]
        public void Constructor_WithNonExistentModelPath_ShouldThrowFileNotFoundException()
        {
            var nonExistentPath = Path.Combine(_tempDir, "nonexistent_model.yml");

            var ex = Assert.Throws<FileNotFoundException>(() =>
            {
                using var interop = new BrisqueInterop(nonExistentPath, _validRangePath);
            });

            Assert.Contains("Model file not found", ex.Message);
            Assert.Contains(nonExistentPath, ex.Message);
        }

        /// <summary>
        /// Validates that passing a null range path to the constructor throws FileNotFoundException.
        /// Expected: FileNotFoundException with message containing "Range file not found".
        /// </summary>
        [Fact]
        public void Constructor_WithNullRangePath_ShouldThrowFileNotFoundException()
        {
            var ex = Assert.Throws<FileNotFoundException>(() =>
            {
                using var interop = new BrisqueInterop(_validModelPath, null!);
            });

            Assert.Contains("Range file not found", ex.Message);
        }

        /// <summary>
        /// Validates that passing an empty string as range path throws FileNotFoundException.
        /// Expected: FileNotFoundException with message containing "Range file not found".
        /// </summary>
        [Fact]
        public void Constructor_WithEmptyRangePath_ShouldThrowFileNotFoundException()
        {
            var ex = Assert.Throws<FileNotFoundException>(() =>
            {
                using var interop = new BrisqueInterop(_validModelPath, "");
            });

            Assert.Contains("Range file not found", ex.Message);
        }

        /// <summary>
        /// Validates that passing only whitespace as range path throws FileNotFoundException.
        /// Expected: FileNotFoundException with message containing "Range file not found".
        /// </summary>
        [Fact]
        public void Constructor_WithWhitespaceRangePath_ShouldThrowFileNotFoundException()
        {
            var ex = Assert.Throws<FileNotFoundException>(() =>
            {
                using var interop = new BrisqueInterop(_validModelPath, "   ");
            });

            Assert.Contains("Range file not found", ex.Message);
        }

        /// <summary>
        /// Tests that providing a path to a non-existent range file throws FileNotFoundException.
        /// Expected: FileNotFoundException with the specific file path in the error message.
        /// </summary>
        [Fact]
        public void Constructor_WithNonExistentRangePath_ShouldThrowFileNotFoundException()
        {
            var nonExistentPath = Path.Combine(_tempDir, "nonexistent_range.yml");

            var ex = Assert.Throws<FileNotFoundException>(() =>
            {
                using var interop = new BrisqueInterop(_validModelPath, nonExistentPath);
            });

            Assert.Contains("Range file not found", ex.Message);
            Assert.Contains(nonExistentPath, ex.Message);
        }

        /// <summary>
        /// Verifies that file paths containing special characters (!@#$%) are handled correctly.
        /// Expected: Files are found and InvalidOperationException is thrown from native library loading, not path validation.
        /// </summary>
        [Fact]
        public void Constructor_WithPathContainingSpecialCharacters_ShouldHandleCorrectly()
        {
            var specialDir = Path.Combine(_tempDir, "special!@#$%chars");
            Directory.CreateDirectory(specialDir);

            var specialModelPath = Path.Combine(specialDir, "model.yml");
            var specialRangePath = Path.Combine(specialDir, "range.yml");

            File.WriteAllText(specialModelPath, "# Model");
            File.WriteAllText(specialRangePath, "# Range");

            // Should throw InvalidOperationException (not FileNotFoundException)
            Assert.Throws<InvalidOperationException>(() =>
            {
                using var interop = new BrisqueInterop(specialModelPath, specialRangePath);
            });
        }

        /// <summary>
        /// Tests that very long file paths (100+ character directory names) are handled properly.
        /// Expected: Long paths are accepted and InvalidOperationException is thrown from native library, not path validation.
        /// </summary>
        [Fact]
        public void Constructor_WithLongPaths_ShouldHandleCorrectly()
        {
            var longDirName = new string('a', 100);
            var longDir = Path.Combine(_tempDir, longDirName);
            Directory.CreateDirectory(longDir);

            var longModelPath = Path.Combine(longDir, "model.yml");
            var longRangePath = Path.Combine(longDir, "range.yml");

            File.WriteAllText(longModelPath, "# Model");
            File.WriteAllText(longRangePath, "# Range");

            // Should throw InvalidOperationException (not FileNotFoundException)
            Assert.Throws<InvalidOperationException>(() =>
            {
                using var interop = new BrisqueInterop(longModelPath, longRangePath);
            });
        }

        /// <summary>
        /// Validates that relative file paths (not absolute) work correctly with the constructor.
        /// Expected: Relative paths are resolved and InvalidOperationException is thrown from native library.
        /// </summary>
        [Fact]
        public void Constructor_WithRelativePaths_ShouldWork()
        {
            // Create files in current directory
            var relativeModel = "test_relative_model.yml";
            var relativeRange = "test_relative_range.yml";

            try
            {
                File.WriteAllText(relativeModel, "# Model");
                File.WriteAllText(relativeRange, "# Range");

                // Should throw InvalidOperationException (not FileNotFoundException)
                Assert.Throws<InvalidOperationException>(() =>
                {
                    using var interop = new BrisqueInterop(relativeModel, relativeRange);
                });
            }
            finally
            {
                if (File.Exists(relativeModel)) File.Delete(relativeModel);
                if (File.Exists(relativeRange)) File.Delete(relativeRange);
            }
        }

        /// <summary>
        /// Tests that passing null for both model and range paths throws FileNotFoundException.
        /// Expected: FileNotFoundException for model file (checked first) with appropriate error message.
        /// </summary>
        [Fact]
        public void Constructor_WithBothNullPaths_ShouldThrowFileNotFoundException()
        {
            var ex = Assert.Throws<FileNotFoundException>(() =>
            {
                using var interop = new BrisqueInterop(null!, null!);
            });

            Assert.Contains("Model file not found", ex.Message);
        }

        /// <summary>
        /// Verifies behavior when model and range file paths are swapped (range passed as model, model as range).
        /// Expected: Files exist so path validation passes, InvalidOperationException thrown from native library.
        /// </summary>
        [Fact]
        public void Constructor_WithSwappedPaths_ShouldStillValidate()
        {
            // Both files exist, so should throw InvalidOperationException from native code
            Assert.Throws<InvalidOperationException>(() =>
            {
                using var interop = new BrisqueInterop(_validRangePath, _validModelPath);
            });
        }

        public void Dispose()
        {
            if (Directory.Exists(_tempDir))
            {
                try
                {
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
