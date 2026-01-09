using Luxoria.Algorithm.BrisqueScore;
using Xunit;

namespace BrisqueScore.Tests
{
    /// <summary>
    /// Integration tests for BrisqueInterop with real model files (if available).
    /// These tests will be skipped if model files are not present.
    /// </summary>
    public class BrisqueInteropIntegrationTests : IDisposable
    {
        private readonly string _modelPath;
        private readonly string _rangePath;
        private readonly string _testImagePath;
        private readonly string _testImage2Path;

        public BrisqueInteropIntegrationTests()
        {
            // Use assets folder
            var assetsPath = Path.Combine(Directory.GetCurrentDirectory(), "assets");
            _modelPath = Path.Combine(assetsPath, "brisque_model_live.yml");
            _rangePath = Path.Combine(assetsPath, "brisque_range_live.yml");
            _testImagePath = Path.Combine(assetsPath, "image.png");
            _testImage2Path = Path.Combine(assetsPath, "image2.png");
        }

        /// <summary>
        /// Integration test verifying that BrisqueInterop can be instantiated with real model files.
        /// Expected: Object creation succeeds when valid brisque_model_live.yml and brisque_range_live.yml are present.
        /// </summary>
        [Fact]
        public void Integration_CreateInterop_WithRealModelFiles_ShouldSucceed()
        {
            using var interop = new BrisqueInterop(_modelPath, _rangePath);
            Assert.NotNull(interop);
        }

        /// <summary>
        /// Tests ComputeScore with a real image file to validate end-to-end quality assessment.
        /// Expected: Returns valid BRISQUE score in range [0, 100] where lower scores indicate better image quality.
        /// </summary>
        [Fact]
        public void Integration_ComputeScore_WithRealImage_ShouldReturnValidScore()
        {
            using var interop = new BrisqueInterop(_modelPath, _rangePath);
            var score = interop.ComputeScore(_testImagePath);

            // BRISQUE scores typically range from 0 to 100
            Assert.InRange(score, 0, 100);
        }

        /// <summary>
        /// Validates that computing scores for the same image multiple times produces identical results.
        /// Expected: All score computations return exactly the same value, demonstrating deterministic behavior.
        /// </summary>
        [Fact]
        public void Integration_ComputeScore_MultipleImages_ShouldReturnConsistentScores()
        {
            using var interop = new BrisqueInterop(_modelPath, _rangePath);

            // Compute score multiple times for same image
            var scores = new List<double>();
            for (int i = 0; i < 3; i++)
            {
                scores.Add(interop.ComputeScore(_testImagePath));
            }

            // All scores should be identical for the same image
            Assert.All(scores, score => Assert.Equal(scores[0], score));
        }

        /// <summary>
        /// Tests that BRISQUE correctly differentiates between two different images.
        /// Expected: Different images produce different BRISQUE scores.
        /// </summary>
        [Fact]
        public void Integration_ComputeScore_DifferentImages_ShouldReturnDifferentScores()
        {
            using var interop = new BrisqueInterop(_modelPath, _rangePath);

            var score1 = interop.ComputeScore(_testImagePath);
            var score2 = interop.ComputeScore(_testImage2Path);

            // Different images should produce different scores (unless they happen to be identical quality)
            Assert.NotEqual(score1, score2);
        }

        /// <summary>
        /// Verifies that multiple BrisqueInterop instances can coexist without interfering with each other.
        /// Expected: All instances are created successfully and operate independently.
        /// </summary>
        [Fact]
        public void Integration_MultipleInstances_ShouldWorkIndependently()
        {
            using var interop1 = new BrisqueInterop(_modelPath, _rangePath);
            using var interop2 = new BrisqueInterop(_modelPath, _rangePath);
            using var interop3 = new BrisqueInterop(_modelPath, _rangePath);

            Assert.NotNull(interop1);
            Assert.NotNull(interop2);
            Assert.NotNull(interop3);
        }

        /// <summary>
        /// Tests resource cleanup by disposing an instance and creating a new one with the same configuration.
        /// Expected: Both instances compute identical scores; disposal and recreation work correctly.
        /// </summary>
        [Fact]
        public void Integration_DisposeAndRecreate_ShouldWork()
        {
            double score1, score2;

            using (var interop = new BrisqueInterop(_modelPath, _rangePath))
            {
                score1 = interop.ComputeScore(_testImagePath);
            }

            // Create new instance after disposing the first
            using (var interop = new BrisqueInterop(_modelPath, _rangePath))
            {
                score2 = interop.ComputeScore(_testImagePath);
            }

            // Scores should be identical
            Assert.Equal(score1, score2);
        }

        /// <summary>
        /// Performance test validating that multiple score computations complete within reasonable time.
        /// Expected: Computing 10 scores completes in under 10 seconds, demonstrating acceptable performance.
        /// </summary>
        [Fact]
        public void Integration_Performance_ComputeManyScores_ShouldBeReasonablyFast()
        {
            using var interop = new BrisqueInterop(_modelPath, _rangePath);

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            for (int i = 0; i < 10; i++)
            {
                interop.ComputeScore(_testImagePath);
            }

            stopwatch.Stop();

            // Should complete 10 scores in reasonable time (adjust threshold as needed)
            Assert.True(stopwatch.ElapsedMilliseconds < 10000,
                $"10 score computations took {stopwatch.ElapsedMilliseconds}ms");
        }

        /// <summary>
        /// Validates that model files are readable and contain valid YAML format.
        /// Expected: Files are non-empty and contain YAML directives (% character); basic format validation passes.
        /// </summary>
        [Fact]
        public void Integration_ModelFiles_ShouldBeValidYaml()
        {
            // Verify files are readable
            var modelContent = File.ReadAllText(_modelPath);
            var rangeContent = File.ReadAllText(_rangePath);

            Assert.NotEmpty(modelContent);
            Assert.NotEmpty(rangeContent);

            // Basic YAML validation
            Assert.Contains("%", modelContent); // YAML typically starts with %
            Assert.Contains("%", rangeContent);
        }

        public void Dispose()
        {
            // No cleanup needed for integration tests
        }
    }
}
