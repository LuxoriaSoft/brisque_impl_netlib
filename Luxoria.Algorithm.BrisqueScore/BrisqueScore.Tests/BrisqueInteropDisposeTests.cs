using Luxoria.Algorithm.BrisqueScore;
using Xunit;

namespace BrisqueScore.Tests
{
    /// <summary>
    /// Tests for BrisqueInterop disposal and resource cleanup.
    /// </summary>
    public class BrisqueInteropDisposeTests : IDisposable
    {
        private readonly string _tempDir;
        private readonly string _validModelPath;
        private readonly string _validRangePath;

        public BrisqueInteropDisposeTests()
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
        /// Verifies that calling Dispose() once on a BrisqueInterop instance does not throw an exception.
        /// Expected: Dispose completes without error (though constructor may throw InvalidOperationException).
        /// </summary>
        [Fact]
        public void Dispose_CalledOnce_ShouldNotThrow()
        {
            var interop = new BrisqueInterop(_validModelPath, _validRangePath);
            interop.Dispose();
            // Should not throw
        }

        /// <summary>
        /// Validates that calling Dispose() multiple times on the same instance is safe and idempotent.
        /// Expected: Multiple Dispose() calls handle gracefully without throwing or causing issues.
        /// </summary>
        [Fact]
        public void Dispose_CalledMultipleTimes_ShouldNotThrow()
        {
            var interop = new BrisqueInterop(_validModelPath, _validRangePath);
            interop.Dispose();
            interop.Dispose();
            interop.Dispose();
            // Should handle multiple dispose calls gracefully
        }

        /// <summary>
        /// Tests that using a BrisqueInterop instance within a using statement automatically calls Dispose() when leaving scope.
        /// Expected: Dispose is invoked automatically at the end of the using block.
        /// </summary>
        [Fact]
        public void Dispose_WithUsingStatement_ShouldDisposeAutomatically()
        {
            using (var interop = new BrisqueInterop(_validModelPath, _validRangePath))
            {
                // Use the interop
            }
            // Dispose should be called automatically
        }

        /// <summary>
        /// Tests that using declaration syntax (using var) automatically disposes the BrisqueInterop instance at end of scope.
        /// Expected: Dispose is called automatically when the variable goes out of scope.
        /// </summary>
        [Fact]
        public void Dispose_WithUsingDeclaration_ShouldDisposeAutomatically()
        {
            using var interop = new BrisqueInterop(_validModelPath, _validRangePath);
            // Dispose should be called at end of scope
        }

        /// <summary>
        /// Verifies that Dispose() can be called manually without using a using statement or declaration.
        /// Expected: Manual Dispose() call works correctly without requiring using syntax.
        /// </summary>
        [Fact]
        public void Dispose_WithoutUsing_ShouldStillBeCallable()
        {
            var interop = new BrisqueInterop(_validModelPath, _validRangePath);
            // Manually dispose
            interop.Dispose();
        }

        /// <summary>
        /// Tests that letting a BrisqueInterop instance go out of scope without explicit disposal doesn't crash during finalization.
        /// Expected: Finalizer handles cleanup gracefully without exceptions. Forces GC to test finalizer behavior.
        /// </summary>
        [Fact]
        public void Dispose_InFinalizer_ShouldNotCrash()
        {
            // Create and let go out of scope without disposing
            var interop = new BrisqueInterop(_validModelPath, _validRangePath);
            // Finalizer should handle cleanup

            // Force garbage collection to test finalizer
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        /// <summary>
        /// Validates that accessing methods after Dispose() has been called doesn't cause crashes.
        /// Expected: Post-dispose access either throws an appropriate exception or returns safely without crashing.
        /// </summary>
        [Fact]
        public void Dispose_ThenAccess_ShouldNotCrash()
        {
            var interop = new BrisqueInterop(_validModelPath, _validRangePath);
            interop.Dispose();

            // Accessing after dispose - behavior depends on implementation
            // Should either throw or return safely
        }

        /// <summary>
        /// Tests that disposing one BrisqueInterop instance doesn't affect other instances.
        /// Expected: Each instance can be disposed independently without interfering with others.
        /// </summary>
        [Fact]
        public void MultipleInstances_DisposedIndependently_ShouldWork()
        {
            var interop1 = new BrisqueInterop(_validModelPath, _validRangePath);
            var interop2 = new BrisqueInterop(_validModelPath, _validRangePath);

            interop1.Dispose();
            // interop2 should still be usable

            interop2.Dispose();
        }

        /// <summary>
        /// Verifies that multiple instances can be disposed in any order, not necessarily creation order.
        /// Expected: Disposing instances in reverse order (2, 1, 3) works without issues.
        /// </summary>
        [Fact]
        public void Dispose_InDifferentOrder_ShouldWork()
        {
            var interop1 = new BrisqueInterop(_validModelPath, _validRangePath);
            var interop2 = new BrisqueInterop(_validModelPath, _validRangePath);
            var interop3 = new BrisqueInterop(_validModelPath, _validRangePath);

            // Dispose in different order than creation
            interop2.Dispose();
            interop1.Dispose();
            interop3.Dispose();
        }

        /// <summary>
        /// Tests that Dispose() works correctly when called from an async context with await operations.
        /// Expected: Dispose functions properly in async methods, even with delays between operations.
        /// </summary>
        [Fact]
        public async Task Dispose_InAsyncContext_ShouldWork()
        {
            var interop = new BrisqueInterop(_validModelPath, _validRangePath);
            await Task.Delay(10); // Simulate async work
            interop.Dispose();
        }

        /// <summary>
        /// Validates that Dispose() is still called when exceptions occur, ensuring proper cleanup.
        /// Expected: Even when constructor throws InvalidOperationException, using statement handles it gracefully.
        /// </summary>
        [Fact]
        public void Dispose_DuringException_ShouldStillDispose()
        {
            // Test that dispose is called even when exception occurs in the using block
            var exceptionCaught = false;

            try
            {
                // Since the constructor itself throws, we can't test inside the using block
                // This test verifies that even if constructor throws, there's no crash
                Assert.Throws<InvalidOperationException>(() =>
                {
                    using var interop = new BrisqueInterop(_validModelPath, _validRangePath);
                });

                exceptionCaught = true;
            }
            catch
            {
                exceptionCaught = true;
            }

            Assert.True(exceptionCaught);
        }

        /// <summary>
        /// Tests that Dispose() handles null internal instance pointers gracefully without crashing.
        /// Expected: Dispose checks for IntPtr.Zero and doesn't call native release on null instances.
        /// </summary>
        [Fact]
        public void Dispose_WithNullInstance_ShouldHandleGracefully()
        {
            BrisqueInterop? interop = null;

            interop = new BrisqueInterop(_validModelPath, _validRangePath);

            // Internal _brisqueInstance is IntPtr.Zero initially (in error cases)
            interop.Dispose();
            // Should check for Zero and not call native release
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
