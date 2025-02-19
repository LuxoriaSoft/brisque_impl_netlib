using System.Reflection;
using System.Runtime.InteropServices;

namespace Luxoria.Algorithm.BrisqueScore
{
    public class BrisqueInterop : IDisposable
    {
        private const string NativeLibraryName = "brisque_quality";
        private IntPtr _brisqueInstance;

        static BrisqueInterop()
        {
            ExtractAndLoadNativeLibrary();
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

        private static void ExtractAndLoadNativeLibrary()
        {
            string architecture = RuntimeInformation.ProcessArchitecture switch
            {
                Architecture.X86 => "x86",
                Architecture.X64 => "x64",
                Architecture.Arm64 => "arm64",
                _ => throw new NotSupportedException("Unsupported architecture")
            };

            string resourceName = $"Luxoria.Algorithm.BrisqueScore.NativeLibraries.{architecture}.brisque_quality.dll";

            string tempPath = Path.Combine(Path.GetTempPath(), "LuxoriaNative");
            Directory.CreateDirectory(tempPath);

            string dllPath = Path.Combine(tempPath, "brisque_quality.dll");

            // Extract DLL from embedded resources
            using (Stream? resourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
            {
                if (resourceStream == null)
                    throw new FileNotFoundException($"Embedded resource not found: {resourceName}");

                using (FileStream fileStream = new FileStream(dllPath, FileMode.Create, FileAccess.Write))
                {
                    resourceStream.CopyTo(fileStream);
                }
            }

            // Load the native library
            NativeLibrary.Load(dllPath);
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
