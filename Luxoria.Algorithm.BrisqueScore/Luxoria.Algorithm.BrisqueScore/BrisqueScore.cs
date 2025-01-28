using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Luxoria.Algorithm.BrisqueScore
{
    public class BrisqueInterop : IDisposable
    {
        private const string NativeLibraryName = "brisque_quality.dll";
        private IntPtr _brisqueInstance;

        static BrisqueInterop()
        {
            LoadNativeLibrary();
        }

        public BrisqueInterop(string modelPath, string rangePath)
        {
            if (string.IsNullOrWhiteSpace(modelPath) || !File.Exists(modelPath))
                throw new FileNotFoundException($"Model file not found: {modelPath}");
            if (string.IsNullOrWhiteSpace(rangePath) || !File.Exists(rangePath))
                throw new FileNotFoundException($"Range file not found: {rangePath}");

            _brisqueInstance = CreateBrisqueAlgorithm(modelPath, rangePath);
            if (_brisqueInstance == IntPtr.Zero)
                throw new InvalidOperationException("Failed to create BRISQUE algorithm instance.");
        }

        private static void LoadNativeLibrary()
        {
            string architecture = RuntimeInformation.ProcessArchitecture switch
            {
                Architecture.X86 => "win-x86",
                Architecture.X64 => "win-x64",
                Architecture.Arm64 => "win-arm64",
                _ => throw new NotSupportedException("Unsupported architecture")
            };

            string nativeLibraryPath = Path.Combine(AppContext.BaseDirectory, "runtimes", architecture, NativeLibraryName);

            if (!File.Exists(nativeLibraryPath))
            {
                throw new FileNotFoundException($"The required native library was not found: {nativeLibraryPath}");
            }

            // Dynamically load the native library
            NativeLibrary.SetDllImportResolver(typeof(BrisqueInterop).Assembly, (libraryName, assembly, searchPath) =>
            {
                if (libraryName == NativeLibraryName)
                {
                    return NativeLibrary.Load(nativeLibraryPath);
                }
                return IntPtr.Zero;
            });
        }

        public double ComputeScore(string imagePath)
        {
            if (string.IsNullOrWhiteSpace(imagePath) || !File.Exists(imagePath))
                throw new FileNotFoundException($"Image file not found: {imagePath}");

            return ComputeBrisqueScore(_brisqueInstance, imagePath);
        }

        public void Dispose()
        {
            if (_brisqueInstance != IntPtr.Zero)
            {
                ReleaseBrisqueAlgorithm(_brisqueInstance);
                _brisqueInstance = IntPtr.Zero;
            }
        }

        // Native function declarations
        [DllImport(NativeLibraryName, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr CreateBrisqueAlgorithm(string modelPath, string rangePath);

        [DllImport(NativeLibraryName, CallingConvention = CallingConvention.Cdecl)]
        private static extern double ComputeBrisqueScore(IntPtr brisqueInstance, string imagePath);

        [DllImport(NativeLibraryName, CallingConvention = CallingConvention.Cdecl)]
        private static extern void ReleaseBrisqueAlgorithm(IntPtr instance);
    }
}
