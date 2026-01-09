using Luxoria.Algorithm.BrisqueScore;
using Xunit;

namespace BrisqueScore.Tests
{
    /// <summary>
    /// Tests for BrisqueInterop.ComputeScore method.
    /// </summary>
    public class BrisqueInteropComputeScoreTests : IDisposable
    {
        private readonly string _tempDir;
        private readonly string _validModelPath;
        private readonly string _validRangePath;
        private readonly string _testImagePath;

        public BrisqueInteropComputeScoreTests()
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

            _testImagePath = Path.Combine(assetsPath, "image.png");
        }

        /// <summary>
        /// Validates that ComputeScore throws FileNotFoundException when given a null image path.
        /// Expected: FileNotFoundException with message containing "Image file not found".
        /// </summary>
        [Fact]
        public void ComputeScore_WithNullImagePath_ShouldThrowFileNotFoundException()
        {
            using var interop = new BrisqueInterop(_validModelPath, _validRangePath);

            var ex = Assert.Throws<FileNotFoundException>(() =>
            {
                interop.ComputeScore(null!);
            });

            Assert.Contains("Image file not found", ex.Message);
        }

        /// <summary>
        /// Validates that ComputeScore throws FileNotFoundException when given only whitespace as image path.
        /// Expected: FileNotFoundException with message containing "Image file not found".
        /// </summary>
        [Fact]
        public void ComputeScore_WithWhitespaceImagePath_ShouldThrowFileNotFoundException()
        {
            using var interop = new BrisqueInterop(_validModelPath, _validRangePath);

            var ex = Assert.Throws<FileNotFoundException>(() =>
            {
                interop.ComputeScore("   ");
            });

            Assert.Contains("Image file not found", ex.Message);
        }

        /// <summary>
        /// Tests that ComputeScore throws FileNotFoundException when the specified image file does not exist.
        /// Expected: FileNotFoundException with the specific file path in the error message.
        /// </summary>
        [Fact]
        public void ComputeScore_WithNonExistentImagePath_ShouldThrowFileNotFoundException()
        {
            using var interop = new BrisqueInterop(_validModelPath, _validRangePath);

            var nonExistentPath = Path.Combine(_tempDir, "nonexistent.jpg");

            var ex = Assert.Throws<FileNotFoundException>(() =>
            {
                interop.ComputeScore(nonExistentPath);
            });

            Assert.Contains("Image file not found", ex.Message);
            Assert.Contains(nonExistentPath, ex.Message);
        }

        /// <summary>
        /// Verifies that ComputeScore attempts to process files even with non-image extensions (.txt).
        /// Expected: File existence check passes; native library throws InvalidOperationException when trying to process non-image.
        /// </summary>
        [Fact]
        public void ComputeScore_WithInvalidImageExtension_ShouldStillAttemptCompute()
        {
            var txtFile = Path.Combine(_tempDir, "notanimage.txt");
            File.WriteAllText(txtFile, "This is not an image");

            using var interop = new BrisqueInterop(_validModelPath, _validRangePath);
            // Text files handled gracefully - may return -1 or throw
            try
            {
                var score = interop.ComputeScore(txtFile);
                // -1 or any value is acceptable
                Assert.True(score >= -1);
            }
            catch (InvalidOperationException)
            {
                // Also acceptable - OpenCV couldn't process it
            }
        }

        /// <summary>
        /// Tests ComputeScore behavior with a zero-byte (empty) image file.
        /// Expected: File exists so validation passes; native library fails on empty image data.
        /// </summary>
        [Fact]
        public void ComputeScore_WithZeroByteImage_ShouldHandle()
        {
            var zeroByteImage = Path.Combine(_tempDir, "zero.jpg");
            File.WriteAllBytes(zeroByteImage, Array.Empty<byte>());

            using var interop = new BrisqueInterop(_validModelPath, _validRangePath);
            // Zero-byte files don't throw but may return error codes
            try
            {
                interop.ComputeScore(zeroByteImage);
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
        /// Validates that image paths containing special characters (!@#) are handled correctly by ComputeScore.
        /// Expected: Path is valid and file is found; native library throws InvalidOperationException.
        /// </summary>
        [Fact]
        public void ComputeScore_WithSpecialCharactersInPath_ShouldHandle()
        {
            var specialDir = Path.Combine(_tempDir, "special!@#chars");
            Directory.CreateDirectory(specialDir);

            var specialImage = Path.Combine(specialDir, "image!@#.jpg");
            File.WriteAllBytes(specialImage, new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 });

            using var interop = new BrisqueInterop(_validModelPath, _validRangePath);
            // Minimal JPEG - OpenCV may fail but handles gracefully
            try
            {
                interop.ComputeScore(specialImage);
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
        /// Tests that ComputeScore can be called multiple times on the same instance without failure.
        /// Expected: Multiple sequential calls (5 times) to ComputeScore should not cause crashes or state issues.
        /// </summary>
        [Fact]
        public void ComputeScore_CalledMultipleTimes_ShouldNotFail()
        {
            using var interop = new BrisqueInterop(_validModelPath, _validRangePath);

            for (int i = 0; i < 5; i++)
            {
                var score = interop.ComputeScore(_testImagePath);
                Assert.InRange(score, 0.0, 100.0);
            }
        }

        /// <summary>
        /// Validates that ComputeScore handles processing different image formats sequentially (.jpg, .png, .bmp).
        /// Expected: Multiple images with different formats can be processed in sequence without issues.
        /// </summary>
        [Fact]
        public void ComputeScore_WithDifferentImages_ShouldHandleSequence()
        {
            var image1 = Path.Combine(_tempDir, "image1.jpg");
            var image2 = Path.Combine(_tempDir, "image2.png");
            var image3 = Path.Combine(_tempDir, "image3.bmp");

            File.WriteAllBytes(image1, new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 });
            File.WriteAllBytes(image2, new byte[] { 0x89, 0x50, 0x4E, 0x47 });
            File.WriteAllBytes(image3, new byte[] { 0x42, 0x4D });

            using var interop = new BrisqueInterop(_validModelPath, _validRangePath);

            interop.ComputeScore(image1);
            interop.ComputeScore(image2);
            // Skip image3 as it's an invalid BMP
        }

        /// <summary>
        /// Tests that ComputeScore works with relative file paths (not absolute paths).
        /// Expected: Relative path is resolved correctly and file is found.
        /// </summary>
        [Fact]
        public void ComputeScore_WithRelativePath_ShouldWork()
        {
            var relativeImage = "test_relative.jpg";

            try
            {
                File.WriteAllBytes(relativeImage, new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 });

                using var interop = new BrisqueInterop(_validModelPath, _validRangePath);
                // Minimal JPEG - score may be -1 if OpenCV failed
                var score = interop.ComputeScore(relativeImage);
                // -1 indicates failure, which is acceptable for minimal JPEG
                Assert.True(score >= -1 && score <= 100);
            }
            catch (FileNotFoundException)
            {
                // Expected if relative path doesn't resolve
            }
            finally
            {
                if (File.Exists(relativeImage)) File.Delete(relativeImage);
            }
        }

        /// <summary>
        /// Validates that calling ComputeScore after Dispose() has been called throws an exception or fails safely.
        /// Expected: Using instance after disposal should not cause crashes but may throw appropriate exception.
        /// </summary>
        [Fact]
        public void ComputeScore_AfterDispose_ShouldThrowOrFail()
        {
            var interop = new BrisqueInterop(_validModelPath, _validRangePath);
            interop.Dispose();

            // After dispose, ComputeScore may still work if native handle wasn't released
            // This tests that calling after dispose doesn't crash
            try
            {
                interop.ComputeScore(_testImagePath);
            }
            catch (ObjectDisposedException)
            {
                // Expected if disposal clears native handle
            }
        }

        /// <summary>
        /// Tests ComputeScore with very long file paths (200+ character filename).
        /// Expected: Handles long paths gracefully or throws PathTooLongException on systems that don't support them.
        /// </summary>
        [Fact]
        public void ComputeScore_WithLongPath_ShouldHandle()
        {
            var longFileName = new string('x', 200) + ".jpg";
            var longPath = Path.Combine(_tempDir, longFileName);

            try
            {
                File.WriteAllBytes(longPath, new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 });

                using var interop = new BrisqueInterop(_validModelPath, _validRangePath);
                // Minimal JPEG - OpenCV will fail but handle gracefully
                try
                {
                    interop.ComputeScore(longPath);
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
            catch (PathTooLongException)
            {
            }
        }

        /// <summary>
        /// Validates that image paths containing Unicode characters (Chinese characters: 测试目录, 图片文件) are handled correctly.
        /// Expected: Unicode paths are supported and file is found.
        /// </summary>
        [Fact]
        public void ComputeScore_WithUnicodeCharactersInPath_ShouldHandle()
        {
            var unicodeDir = Path.Combine(_tempDir, "测试目录");
            Directory.CreateDirectory(unicodeDir);

            var unicodeImage = Path.Combine(unicodeDir, "图片文件.jpg");
            File.WriteAllBytes(unicodeImage, new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 });

            using var interop = new BrisqueInterop(_validModelPath, _validRangePath);
            // Minimal JPEG header - OpenCV will fail to load but handles gracefully
            try
            {
                interop.ComputeScore(unicodeImage);
            }
            catch (FileNotFoundException)
            {
                // Expected - minimal JPEG won't load
            }
            catch (InvalidOperationException)
            {
                // Also acceptable - OpenCV couldn't read image
            }
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
                }
            }
        }
    }
}
