using System.Reflection;
using System.Runtime.InteropServices;

namespace Luxoria.Algorithm.BrisqueScore
{
    /// <summary>
    /// Provides an interface for interacting with the native BRISQUE quality assessment algorithm.
    /// </summary>
    public class BrisqueInterop : IDisposable
    {
        private const string NativeLibraryName = "brisque_quality";
        private IntPtr _brisqueInstance;

        /// <summary>
        /// Static constructor to load the native library when the class is first accessed.
        /// </summary>
        static BrisqueInterop()
        {
            ExtractAndLoadNativeLibrary();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BrisqueInterop"/> class.
        /// </summary>
        /// <param name="modelPath">Path to the BRISQUE model file.</param>
        /// <param name="rangePath">Path to the BRISQUE range file.</param>
        /// <exception cref="FileNotFoundException">Thrown if the model or range file does not exist.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the BRISQUE algorithm instance cannot be created.</exception>
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

        /// <summary>
        /// Extracts and loads the native BRISQUE library from embedded resources.
        /// </summary>
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

            // Extract the DLL from embedded resources
            using (Stream? resourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
            {
                if (resourceStream == null)
                    throw new FileNotFoundException($"Embedded resource not found: {resourceName}");

                using (FileStream fileStream = new FileStream(dllPath, FileMode.Create, FileAccess.Write))
                {
                    resourceStream.CopyTo(fileStream);
                }
            }

            // Load the extracted native library
            NativeLibrary.Load(dllPath);
        }

        /// <summary>
        /// Computes the BRISQUE score for a given image.
        /// </summary>
        /// <param name="imagePath">Path to the image file.</param>
        /// <returns>The computed BRISQUE score.</returns>
        /// <exception cref="FileNotFoundException">Thrown if the image file does not exist.</exception>
        public double ComputeScore(string imagePath)
        {
            if (string.IsNullOrWhiteSpace(imagePath) || !File.Exists(imagePath))
                throw new FileNotFoundException($"Image file not found: {imagePath}");

            return ComputeBrisqueScore(_brisqueInstance, imagePath);
        }

        /// <summary>
        /// Releases resources used by the BRISQUE algorithm instance.
        /// </summary>
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
