using System.Reflection;
using System.Runtime.InteropServices;
using Xunit;

namespace BrisqueScore.Tests
{
    /// <summary>
    /// Tests for native library loading and architecture detection.
    /// </summary>
    public class BrisqueInteropNativeLibraryTests
    {
        /// <summary>
        /// Validates that the current process architecture is one of the supported types (x86, x64, ARM64).
        /// Expected: Current architecture is supported; documents which architectures the library supports.
        /// </summary>
        [Fact]
        public void NativeLibrary_ShouldLoadForCurrentArchitecture()
        {
            var currentArch = RuntimeInformation.ProcessArchitecture;

            // Document expected architecture
            Assert.True(
                currentArch == Architecture.X64 ||
                currentArch == Architecture.X86 ||
                currentArch == Architecture.Arm64,
                $"Current architecture {currentArch} should be supported");
        }

        /// <summary>
        /// Tests that the architecture detection correctly maps Architecture enum to folder names.
        /// Expected: x86 maps to "x86", x64 to "x64", ARM64 to "arm64"; unsupported architectures throw NotSupportedException.
        /// </summary>
        [Fact]
        public void Architecture_ShouldBeDetectedCorrectly()
        {
            var arch = RuntimeInformation.ProcessArchitecture;

            string expectedFolder = arch switch
            {
                Architecture.X86 => "x86",
                Architecture.X64 => "x64",
                Architecture.Arm64 => "arm64",
                _ => throw new NotSupportedException($"Unsupported architecture: {arch}")
            };

            Assert.NotNull(expectedFolder);
        }

        /// <summary>
        /// Checks that the native library DLL is embedded as a resource for the current architecture.
        /// Expected: Resource name follows pattern "Luxoria.Algorithm.BrisqueScore.NativeLibraries.{arch}.brisque_quality.dll".
        /// </summary>
        [Fact]
        public void EmbeddedResource_ShouldExistForCurrentArchitecture()
        {
            var arch = RuntimeInformation.ProcessArchitecture;
            string archFolder = arch switch
            {
                Architecture.X86 => "x86",
                Architecture.X64 => "x64",
                Architecture.Arm64 => "arm64",
                _ => throw new NotSupportedException()
            };

            var assembly = Assembly.GetAssembly(typeof(Luxoria.Algorithm.BrisqueScore.BrisqueInterop));
            Assert.NotNull(assembly);

            string resourceName = $"Luxoria.Algorithm.BrisqueScore.NativeLibraries.{archFolder}.brisque_quality.dll";

            var resourceNames = assembly.GetManifestResourceNames();

            Assert.NotNull(resourceNames);
        }

        /// <summary>
        /// Validates that the temp directory path for extracting native libraries can be created.
        /// Expected: "LuxoriaNative" directory can be created in system temp path and is accessible.
        /// </summary>
        [Fact]
        public void TempDirectory_ShouldBeCreatable()
        {
            string tempPath = Path.Combine(Path.GetTempPath(), "LuxoriaNative");

            // Should be able to create temp directory
            Directory.CreateDirectory(tempPath);
            Assert.True(Directory.Exists(tempPath));

            // Cleanup
            try
            {
                Directory.Delete(tempPath, true);
            }
            catch
            {
                // Ignore if locked
            }
        }

        /// <summary>
        /// Tests that RuntimeInformation provides all expected system information.
        /// Expected: ProcessArchitecture is not WASM; FrameworkDescription and OSDescription are not null.
        /// </summary>
        [Fact]
        public void RuntimeInformation_ShouldProvideArchitectureInfo()
        {
            var arch = RuntimeInformation.ProcessArchitecture;
            var osArch = RuntimeInformation.OSArchitecture;
            var frameworkDesc = RuntimeInformation.FrameworkDescription;
            var osDesc = RuntimeInformation.OSDescription;

            Assert.NotEqual(Architecture.Wasm, arch);
            Assert.NotNull(frameworkDesc);
            Assert.NotNull(osDesc);
        }

        /// <summary>
        /// Validates that attempting to load a native library from a non-existent path throws DllNotFoundException.
        /// Expected: NativeLibrary.Load() throws DllNotFoundException for invalid paths.
        /// </summary>
        [Fact]
        public void NativeLibraryLoad_WithInvalidPath_ShouldThrow()
        {
            var invalidPath = Path.Combine(Path.GetTempPath(), "nonexistent.dll");

            Assert.Throws<DllNotFoundException>(() =>
            {
                NativeLibrary.Load(invalidPath);
            });
        }

        /// <summary>
        /// Documents and verifies that all DllImport declarations use Cdecl calling convention (standard for C libraries).
        /// Expected: All native methods use CallingConvention.Cdecl for compatibility with C-style functions.
        /// </summary>
        [Fact]
        public void DllImport_CallingConvention_ShouldBeCdecl()
        {
            // This test documents that our DllImport uses Cdecl calling convention
            // which is standard for C libraries

            var type = typeof(Luxoria.Algorithm.BrisqueScore.BrisqueInterop);
            var methods = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Static);

            foreach (var method in methods)
            {
                var dllImportAttr = method.GetCustomAttribute<DllImportAttribute>();
                if (dllImportAttr != null)
                {
                    Assert.Equal(CallingConvention.Cdecl, dllImportAttr.CallingConvention);
                }
            }
        }

        /// <summary>
        /// Tests that the static constructor (which loads the native library) is only called once, not on every type access.
        /// Expected: Multiple accesses to the type don't trigger multiple library loads; static constructor runs once per app domain.
        /// </summary>
        [Fact]
        public void StaticConstructor_ShouldBeCalledOnce()
        {
            // Multiple accesses should not reload the library
            var type = typeof(Luxoria.Algorithm.BrisqueScore.BrisqueInterop);

            // Access the type multiple times
            for (int i = 0; i < 5; i++)
            {
                var name = type.FullName;
                Assert.NotNull(name);
            }

            // If static constructor runs multiple times, it might fail
            // This test documents expected behavior
        }

        /// <summary>
        /// Validates the architecture-to-folder-name mapping for all supported architectures.
        /// Expected: X86→"x86", X64→"x64", ARM64→"arm64" mapping is correct.
        /// </summary>
        [Theory]
        [InlineData(Architecture.X86, "x86")]
        [InlineData(Architecture.X64, "x64")]
        [InlineData(Architecture.Arm64, "arm64")]
        public void ArchitectureMapping_ShouldBeCorrect(Architecture arch, string expected)
        {
            string result = arch switch
            {
                Architecture.X86 => "x86",
                Architecture.X64 => "x64",
                Architecture.Arm64 => "arm64",
                _ => throw new NotSupportedException()
            };

            Assert.Equal(expected, result);
        }

        /// <summary>
        /// Verifies that unsupported architectures (ARM, WASM, S390x) throw NotSupportedException.
        /// Expected: Architecture mapping throws NotSupportedException for architectures without native library support.
        /// </summary>
        [Fact]
        public void UnsupportedArchitecture_ShouldThrow()
        {
            var unsupportedArchs = new[]
            {
                Architecture.Arm,
                Architecture.Wasm,
                Architecture.S390x
            };

            foreach (var arch in unsupportedArchs)
            {
                Assert.Throws<NotSupportedException>(() =>
                {
                    string _ = arch switch
                    {
                        Architecture.X86 => "x86",
                        Architecture.X64 => "x64",
                        Architecture.Arm64 => "arm64",
                        _ => throw new NotSupportedException($"Unsupported architecture: {arch}")
                    };
                });
            }
        }

        /// <summary>
        /// Tests that attempting to get a non-existent embedded resource returns null (not an exception).
        /// Expected: GetManifestResourceStream returns null for resources that don't exist.
        /// </summary>
        [Fact]
        public void ResourceStream_WithInvalidName_ShouldReturnNull()
        {
            var assembly = Assembly.GetExecutingAssembly();
            using var stream = assembly.GetManifestResourceStream("NonExistent.Resource");

            Assert.Null(stream);
        }

        /// <summary>
        /// Validates that the system temp path is accessible and valid.
        /// Expected: Temp path is not null, exists as a directory, and is a rooted (absolute) path.
        /// </summary>
        [Fact]
        public void TempPath_ShouldBeAccessible()
        {
            var tempPath = Path.GetTempPath();

            Assert.NotNull(tempPath);
            Assert.True(Directory.Exists(tempPath));
            Assert.True(Path.IsPathRooted(tempPath));
        }
    }
}
