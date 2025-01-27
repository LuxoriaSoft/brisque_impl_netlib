using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Luxoria.Algorithm.BrisqueScore
{
    public static class BrisqueInterop
    {
        private const string NativeLibraryName = "brisque_quality.dll";

        static BrisqueInterop()
        {
            LoadNativeLibrary();
        }

        private static void LoadNativeLibrary()
        {
            string architecture = RuntimeInformation.ProcessArchitecture switch
            {
                Architecture.X86 => "x86",
                Architecture.X64 => "x64",
                Architecture.Arm64 => "arm64",
                _ => throw new NotSupportedException("Unsupported architecture")
            };

            string nativeLibraryPath = Path.Combine(AppContext.BaseDirectory, "NativeLibraries", architecture, NativeLibraryName);

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

        [DllImport(NativeLibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern double ComputeBrisqueScore(string imagePath);

        [DllImport(NativeLibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr CreateBrisqueAlgorithm(string modelPath, string rangePath);

        [DllImport(NativeLibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void ReleaseBrisqueAlgorithm(IntPtr instance);
    }
}
