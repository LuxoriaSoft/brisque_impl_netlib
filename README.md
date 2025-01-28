# Luxoria.Algorithm.BrisqueScore

This NuGet package provides a .NET wrapper for the **BRISQUE (Blind/Referenceless Image Spatial Quality Evaluator)** algorithm implemented in native C++ with OpenCV. It allows .NET developers to compute the perceptual quality of images using the BRISQUE algorithm without requiring a reference image.

## Features
- **Easy-to-Use .NET API**: Access BRISQUE functionality directly in .NET applications.
- **Cross-Platform Support**: Includes native libraries for the following architectures:
  - `x86` (32-bit Windows)
  - `x64` (64-bit Windows)
  - `arm64` (ARM-based 64-bit Windows)
- **Precompiled Native Libraries**: The package includes precompiled `brisque_quality.dll` for all supported architectures.

## Requirements
- **.NET Version**: `net8.0` or compatible.
- **Native Dependencies**: OpenCV libraries are embedded within the native implementation.

## Source Code
The precompiled native libraries are built from the source code available at [LuxoriaSoft/brisque_impl](https://github.com/LuxoriaSoft/brisque_impl)

## Installation
You can install the package via NuGet Package Manager or the `.NET CLI`:

### Using NuGet Package Manager
Search for `Luxoria.Algorithm.BrisqueScore` in the NuGet Package Manager and install it.

### Using .NET CLI
Run the following command:
```bash
dotnet add package Luxoria.Algorithm.BrisqueScore --version 2.0.0.4100
```

### Usage
```csharp	
using Luxoria.Algorithm.BrisqueScore;

class Program
{
    static void Main()
    {
        string modelPath = @"path\to\brisque_model_live.yml";
        string rangePath = @"path\to\brisque_range_live.yml";
        string imagePath = @"path\to\image.jpg";

        try
        {
            using var brisque = new BrisqueInterop(modelPath, rangePath);
            double score = brisque.ComputeScore(imagePath);
            Console.WriteLine($"BRISQUE Score: {score}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}
```

### License
Luxoria.Algorithm.BrisqueScore is licensed under the Apache 2.0 License. See [LICENSE](LICENSE) for more information.

LuxoriaSoft
