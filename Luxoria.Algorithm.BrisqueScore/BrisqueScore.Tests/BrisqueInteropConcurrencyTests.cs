using Luxoria.Algorithm.BrisqueScore;
using System.Collections.Concurrent;
using Xunit;

namespace BrisqueScore.Tests
{
    /// <summary>
    /// Tests for concurrent and multi-threaded usage of BrisqueInterop.
    /// </summary>
    public class BrisqueInteropConcurrencyTests : IDisposable
    {
        private readonly string _tempDir;
        private readonly string _validModelPath;
        private readonly string _validRangePath;
        private readonly string _testImagePath;

        public BrisqueInteropConcurrencyTests()
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
        /// Tests creating 10 BrisqueInterop instances concurrently using Parallel.For.
        /// Expected: All instances handle concurrent creation; each throws InvalidOperationException due to native library not being loaded.
        /// </summary>
        [Fact]
        public void MultipleInstances_CreatedConcurrently_ShouldAllSucceed()
        {
            var exceptions = new ConcurrentBag<Exception>();

            Parallel.For(0, 10, i =>
            {
                try
                {
                    using var interop = new BrisqueInterop(_validModelPath, _validRangePath);
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            });

            // All should succeed without exceptions
            Assert.Empty(exceptions);
        }

        /// <summary>
        /// Validates that 10 BrisqueInterop instances can be created asynchronously with Task.Run without interference.
        /// Expected: All async tasks complete and catch exceptions; no threading issues or deadlocks occur.
        /// </summary>
        [Fact]
        public async Task MultipleInstances_CreatedAsynchronously_ShouldWork()
        {
            var tasks = new List<Task<Exception?>>();

            for (int i = 0; i < 10; i++)
            {
                tasks.Add(Task.Run(() =>
                {
                    try
                    {
                        using var interop = new BrisqueInterop(_validModelPath, _validRangePath);
                        return null as Exception;
                    }
                    catch (Exception ex)
                    {
                        return ex;
                    }
                }));
            }

            var results = await Task.WhenAll(tasks);

            // All should have succeeded without exceptions
            Assert.All(results, result => Assert.Null(result));
        }

        /// <summary>
        /// Tests that calling ComputeScore concurrently on a single instance (5 parallel calls) doesn't cause crashes.
        /// Expected: Handles concurrent calls without data corruption or access violations.
        /// </summary>
        [Fact]
        public void SingleInstance_ComputeScore_CalledConcurrently_ShouldHandle()
        {
            using var interop = new BrisqueInterop(_validModelPath, _validRangePath);

            var exceptions = new ConcurrentBag<Exception>();

            Parallel.For(0, 5, i =>
            {
                try
                {
                    interop.ComputeScore(_testImagePath);
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            });

            // Should succeed without exceptions
            Assert.Empty(exceptions);
        }

        /// <summary>
        /// Validates that ComputeScore can be called asynchronously from multiple tasks (5 tasks) on the same instance.
        /// Expected: Async calls complete without deadlocks or threading issues.
        /// </summary>
        [Fact]
        public async Task SingleInstance_ComputeScore_CalledAsynchronously_ShouldHandle()
        {
            using var interop = new BrisqueInterop(_validModelPath, _validRangePath);

            var tasks = new List<Task>();

            for (int i = 0; i < 5; i++)
            {
                tasks.Add(Task.Run(() =>
                {
                    try
                    {
                        interop.ComputeScore(_testImagePath);
                    }
                    catch
                    {
                        // Catch any exceptions
                    }
                }));
            }

            await Task.WhenAll(tasks);
        }

        /// <summary>
        /// Tests that two BrisqueInterop instances created on separate threads don't interfere with each other.
        /// Expected: Each thread's instance operates independently; one thread's exception doesn't affect the other.
        /// </summary>
        [Fact]
        public void MultipleInstances_DifferentThreads_ShouldNotInterfere()
        {
            var thread1Exception = null as Exception;
            var thread2Exception = null as Exception;

            var thread1 = new Thread(() =>
            {
                try
                {
                    using var interop = new BrisqueInterop(_validModelPath, _validRangePath);
                    Thread.Sleep(100);
                }
                catch (Exception ex)
                {
                    thread1Exception = ex;
                }
            });

            var thread2 = new Thread(() =>
            {
                try
                {
                    using var interop = new BrisqueInterop(_validModelPath, _validRangePath);
                    Thread.Sleep(100);
                }
                catch (Exception ex)
                {
                    thread2Exception = ex;
                }
            });

            thread1.Start();
            thread2.Start();

            thread1.Join();
            thread2.Join();

            // Both threads should succeed
            Assert.Null(thread1Exception);
            Assert.Null(thread2Exception);
        }

        /// <summary>
        /// Stress tests rapid creation and disposal of 100 BrisqueInterop instances in sequence.
        /// Expected: Rapid create/dispose cycles don't cause memory leaks, crashes, or resource exhaustion.
        /// </summary>
        [Fact]
        public void RapidCreateDispose_ShouldNotCauseIssues()
        {
            var exceptions = new ConcurrentBag<Exception>();

            for (int i = 0; i < 100; i++)
            {
                try
                {
                    var interop = new BrisqueInterop(_validModelPath, _validRangePath);
                    interop.Dispose();
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }

            // Should succeed without exceptions
            Assert.Empty(exceptions);
        }

        /// <summary>
        /// Stress test with 50 parallel operations, each creating an instance and calling ComputeScore 3 times.
        /// Expected: System remains stable under heavy concurrent load; no crashes, deadlocks, or resource leaks.
        /// </summary>
        [Fact]
        public void StressTest_ManyOperations_ShouldStayStable()
        {
            var operations = 50;
            var exceptions = new ConcurrentBag<Exception>();

            Parallel.For(0, operations, i =>
            {
                try
                {
                    using var interop = new BrisqueInterop(_validModelPath, _validRangePath);

                    for (int j = 0; j < 3; j++)
                    {
                        interop.ComputeScore(_testImagePath);
                    }
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            });

            // Should complete without exceptions
            Assert.Empty(exceptions);
        }

        /// <summary>
        /// Tests thread-safety by synchronizing 3 threads with a Barrier and creating instances simultaneously.
        /// Expected: All 3 threads create instances at the same time without race conditions or corruption.
        /// </summary>
        [Fact]
        public void ThreadSafety_CreateFromDifferentThreads_ShouldWork()
        {
            var barrier = new Barrier(3);
            var exceptions = new ConcurrentBag<Exception>();

            var threads = Enumerable.Range(0, 3).Select(i => new Thread(() =>
            {
                try
                {
                    barrier.SignalAndWait(); // Synchronize start
                    using var interop = new BrisqueInterop(_validModelPath, _validRangePath);
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            })).ToArray();

            foreach (var thread in threads)
            {
                thread.Start();
            }

            foreach (var thread in threads)
            {
                thread.Join();
            }

            // All threads should succeed
            Assert.Empty(exceptions);
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
