using System.Runtime.InteropServices;

namespace Luxoria.Algorithm.BrisqueScore;

public static class BrisqueInterop
{
    static BrisqueInterop()
    {
        // Dynamically resolve the correct native library path based on the architecture.
        string architecture = RuntimeInformation.ProcessArchitecture switch
        {
            Architecture.X86 => "x86",
            Architecture.X64 => "x64",
            Architecture.Arm64 => "arm64",
            _ => throw new NotSupportedException("Unsupported architecture, checkout at https://github.com/LuxoriaSoft/brisque_impl_netlib")
        };

        string nativePath = Path.Combine(AppContext.BaseDirectory, "lib", architecture, "BRISQUE.dll");

        if (!File.Exists(nativePath))
            throw new FileNotFoundException($"Native BRISQUE library not found: {nativePath}");

        // Load the native library dynamically
        NativeLibrary.SetDllImportResolver(typeof(BrisqueInterop).Assembly, (libraryName, assembly, searchPath) =>
        {
            if (libraryName == "BRISQUE.dll")
                return NativeLibrary.Load(nativePath);

            return IntPtr.Zero;
        });
    }

    // Import the native method using P/Invoke
    [DllImport("BRISQUE.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern double ComputeBrisqueScore(string imagePath);

    /// <summary>
    /// Calculates the BRISQUE score for a given image file.
    /// </summary>
    /// <param name="imagePath">The full path to the image file.</param>
    /// <returns>The BRISQUE score.</returns>
    public static double CalculateScore(string imagePath)
    {
        if (string.IsNullOrEmpty(imagePath) || !File.Exists(imagePath))
            throw new FileNotFoundException("Image file not found.", imagePath);

        return ComputeBrisqueScore(imagePath);
    }
}