# BRISQUE Refactoring Journal: C++ to C#

## Document Information

| Field | Value |
|-------|-------|
| **Project** | Luxoria BRISQUE Implementation |
| **Original Language** | C++ (OpenCV 4.10.0) with contrib modules |
| **Target Language** | C# (.NET 8.0) |
| **Architecture** | Native Interop via P/Invoke |
| **License** | Apache 2.0 |

---

## Table of Contents

1. [Executive Summary](#1-executive-summary)
2. [Project Context](#2-project-context)
3. [Architecture Comparison](#3-architecture-comparison)
4. [Technical Trade-offs Analysis](#4-technical-trade-offs-analysis)
5. [Design Decisions](#5-design-decisions)
6. [Memory Management](#6-memory-management)
7. [Platform Strategy](#7-platform-strategy)
8. [Distribution Strategy](#8-distribution-strategy)
9. [Error Handling](#9-error-handling)
10. [API Design](#10-api-design)
11. [Build System](#11-build-system)
12. [Lessons Learned](#12-lessons-learned)
13. [Conclusion](#13-conclusion)

---

## 1. Executive Summary

This document records the technical decisions and architectural choices made during the port of the BRISQUE (Blind/Referenceless Image Spatial Quality Evaluator) algorithm from a native C++ implementation to a .NET C# library.

### Project Goal

Create a NuGet package for seamless integration into **Luxoria**, our C# WinUI desktop application, while preserving the performance characteristics of the native OpenCV implementation.

### Key Achievements

- **Zero-configuration deployment**: Single `dotnet add package` command
- **Native performance preserved**: P/Invoke wrapper around optimized C++ code
- **Multi-architecture support**: x86, x64, and ARM64 Windows
- **Minimal footprint**: Strict as possible, one goal, one task to perform

### Trade-off Summary

| Aspect | Decision | Outcome |
|--------|----------|---------|
| Performance | Native code via P/Invoke | CPU-intensive operations remain optimized |
| Distribution | Embedded native libraries | Zero external dependencies |
| API Surface | Simplified wrapper | Easy adoption for .NET developers |
| Platform | Windows-focused | Optimal Visual Studio/NuGet integration |

---

## 2. Project Context

### 2.1 What is BRISQUE?

BRISQUE (Blind/Referenceless Image Spatial Quality Evaluator) is a no-reference image quality assessment algorithm that:

- Requires no reference image for comparison
- Uses natural scene statistics (NSS) in the spatial domain
- Extracts 36 features from locally normalized luminance coefficients
- Employs SVM regression to predict perceptual quality scores (0-100, lower is better)

### 2.2 Why This Refactoring?

The Luxoria application needed a reliable image quality assessment solution. Rather than reimplementing a complex algorithm from scratch, we chose to wrap the battle-tested OpenCV implementation, gaining:

1. **Proven accuracy**: OpenCV's BRISQUE is validated against academic benchmarks
2. **Optimized performance**: Years of C++ optimization work
3. **Reduced risk**: No algorithmic implementation bugs
4. **Faster delivery**: Wrapper approach vs full reimplementation

### 2.3 Original Implementation (C++)

```
brisquecpp/
├── includes/
│   └── BrisqueAlgorithm.hpp    # 41 lines - Wrapper class
├── main.cpp                     # 60 lines - CLI entry point
├── CMakeLists.txt              # Build configuration
└── models/
    ├── brisque_model_live.yml  # Pre-trained SVM model (555 KB)
    └── brisque_range_live.yml  # Feature normalization (1.3 KB)
```

### 2.4 Target Implementation (C#)

```
brisquecsharp/
├── Luxoria.Algorithm.BrisqueScore/
│   ├── BrisqueScore.cs                    # 113 lines - P/Invoke wrapper
│   ├── NativeLibraries/
│   │   ├── x86/brisque_quality.dll       # 4.2 MB
│   │   ├── x64/brisque_quality.dll       # 5.6 MB
│   │   └── arm64/brisque_quality.dll     # 3.9 MB
│   └── Luxoria.Algorithm.BrisqueScore.csproj
└── BrisqueScore.TestMain/                 # Reference application
└── BrisqueScore.Tests/                    # Testing enforcement policy (testing codebase)
```

---

## 3. Architecture Comparison

### 3.1 C++ Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                      main.cpp (CLI)                         │
│  - Argument parsing                                         │
│  - Image loading (cv::imread)                               │
│  - Grayscale conversion (cv::cvtColor)                      │
└─────────────────────────┬───────────────────────────────────┘
                          │
                          ▼
┌─────────────────────────────────────────────────────────────┐
│              BrisqueAlgorithm (Facade)                      │
│  namespace: luxoria::filter::algorithms                     │
│  - cv::Ptr<cv::quality::QualityBRISQUE> _model              │
│  - compute(cv::Mat) -> double                               │
└─────────────────────────┬───────────────────────────────────┘
                          │
                          ▼
┌─────────────────────────────────────────────────────────────┐
│              OpenCV Quality Module                          │
│  - Feature extraction (36 NSS features)                     │
│  - SVM prediction                                           │
└─────────────────────────────────────────────────────────────┘
```

### 3.2 C# Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                Luxoria WinUI Application                    │
│  using Luxoria.Algorithm.BrisqueScore;                      │
└─────────────────────────┬───────────────────────────────────┘
                          │
                          ▼
┌─────────────────────────────────────────────────────────────┐
│              BrisqueInterop : IDisposable                   │
│  - Static constructor: ExtractAndLoadNativeLibrary()        │
│  - Architecture detection (x86/x64/arm64)                   │
│  - Resource extraction to temp directory                    │
│  - Validation layer (file existence checks)                 │
│  - P/Invoke declarations                                    │
└─────────────────────────┬───────────────────────────────────┘
                          │ P/Invoke (Cdecl)
                          ▼
┌─────────────────────────────────────────────────────────────┐
│              brisque_quality.dll (Native)                   │
│  - CreateBrisqueAlgorithm()                                 │
│  - ComputeBrisqueScore()                                    │
│  - ReleaseBrisqueAlgorithm()                                │
│  + OpenCV (statically linked)                               │
└─────────────────────────────────────────────────────────────┘
```

### 3.3 Key Architectural Differences

| Aspect | C++ | C# |
|--------|-----|-----|
| **Purpose** | CLI tool | Library for Luxoria |
| **OpenCV Access** | Direct linking | Indirect via native DLL |
| **Image Input** | `cv::Mat` objects | File path strings |
| **Memory Model** | RAII with smart pointers | Managed + IDisposable |
| **Distribution** | Executable | NuGet package |

---

## 4. Technical Trade-offs Analysis

### 4.1 Interop Strategy: P/Invoke vs Pure Managed

We evaluated two approaches for bringing BRISQUE to .NET:

#### Option A: Pure C# Reimplementation

| Pros | Cons |
|------|------|
| No native dependencies | Requires reimplementing complex algorithms |
| True cross-platform | Performance penalty on CPU math |
| Simpler deployment | ~2000+ lines of code to maintain |
| | Risk of algorithmic discrepancies |

#### Option B: P/Invoke Wrapper (Selected)

| Pros | Cons |
|------|------|
| Leverages optimized OpenCV code | Platform-specific binaries |
| Only ~113 lines to maintain | Larger package size |
| Guaranteed algorithmic consistency | Native memory management |
| Proven implementation | |

**Decision**: P/Invoke was the clear winner. BRISQUE involves complex mathematical operations (DCT, SVD, SVM) that OpenCV has spent years optimizing. The wrapper approach reduced development effort by approximately 90% while preserving performance.

### 4.2 Library Embedding vs External Dependencies

#### Option A: External DLL Reference

```csharp
// Would require users to install OpenCV separately
[DllImport("opencv_quality4100.dll")]
```

| Pros | Cons |
|------|------|
| Smaller package | Complex installation |
| Shared across apps | Version compatibility issues |
| | PATH configuration required |
| | Support burden |

#### Option B: Embedded Resources (Selected)

```xml
<EmbeddedResource Include="NativeLibraries\x64\brisque_quality.dll" />
```

| Pros | Cons |
|------|------|
| Zero-config deployment | ~14MB total package size |
| Version-locked | Cannot share OpenCV |
| Works everywhere | Temp directory usage |
| NuGet "just works" | |

**Decision**: Developer experience trumps package size. When someone adds our package, it should work immediately without additional setup steps.

### 4.3 Static vs Dynamic Linking (Native Side)

The native DLLs statically link OpenCV:

```cmake
set(CMAKE_MSVC_RUNTIME_LIBRARY "MultiThreaded$<$<CONFIG:Debug>:Debug>")
```

| Architecture | DLL Size | Contents |
|--------------|----------|----------|
| x86 | 4.2 MB | OpenCV core + quality (static) |
| x64 | 5.6 MB | OpenCV core + quality (static) |
| arm64 | 3.9 MB | OpenCV core + quality (static) |

**Trade-off**: Larger binaries but guaranteed compatibility. No DLL hell.

---

## 5. Design Decisions

### 5.1 API Surface Design

#### C++ Original API

```cpp
class BrisqueAlgorithm {
public:
    BrisqueAlgorithm(std::string modelPath, std::string rangePath);
    double compute(cv::Mat img);
    ~BrisqueAlgorithm();
};
```

#### C# Wrapper API

```csharp
public class BrisqueInterop : IDisposable {
    public BrisqueInterop(string modelPath, string rangePath);
    public double ComputeScore(string imagePath);
    public void Dispose();
}
```

#### Design Changes Explained

| Change | Rationale |
|--------|-----------|
| `cv::Mat` → `string imagePath` | Avoids exposing OpenCV types to managed code |
| `compute()` → `ComputeScore()` | .NET naming conventions (PascalCase, descriptive) |
| Destructor → `IDisposable` | Standard .NET pattern for unmanaged resources |

### 5.2 Native Function Signatures

Three clean P/Invoke entry points:

```csharp
[DllImport("brisque_quality", CallingConvention = CallingConvention.Cdecl)]
private static extern IntPtr CreateBrisqueAlgorithm(string modelPath, string rangePath);

[DllImport("brisque_quality", CallingConvention = CallingConvention.Cdecl)]
private static extern double ComputeBrisqueScore(IntPtr brisqueInstance, string imagePath);

[DllImport("brisque_quality", CallingConvention = CallingConvention.Cdecl)]
private static extern void ReleaseBrisqueAlgorithm(IntPtr instance);
```

**Design Rationale:**
- **Cdecl convention**: Standard for C++ exports
- **IntPtr for instances**: Opaque handle hides implementation details
- **Explicit release**: Required because GC doesn't manage native memory

### 5.3 Namespace Design

| Language | Namespace |
|----------|-----------|
| C++ | `luxoria::filter::algorithms` |
| C# | `Luxoria.Algorithm.BrisqueScore` |

The C# namespace was simplified while maintaining consistency with Luxoria's naming conventions.

---

## 6. Memory Management

### 6.1 C++ Memory Model

```cpp
class BrisqueAlgorithm {
private:
    cv::Ptr<cv::quality::QualityBRISQUE> _model;  // Reference-counted smart pointer

public:
    ~BrisqueAlgorithm() {
        _model.release();  // Explicit release (optional due to smart pointer)
    }
};
```

### 6.2 C# Memory Model

```csharp
public class BrisqueInterop : IDisposable {
    private IntPtr _brisqueInstance;  // Opaque handle to native object

    public void Dispose() {
        if (_brisqueInstance != IntPtr.Zero) {
            ReleaseBrisqueAlgorithm(_brisqueInstance);
            _brisqueInstance = IntPtr.Zero;  // Prevent double-free
        }
    }
}
```

### 6.3 Memory Ownership

```
┌───────────────────────────────────────────────────┐
│              Managed Heap (.NET)                  │
│  ┌───────────────────────────────────────────┐    │
│  │  BrisqueInterop instance                  │    │
│  │  - _brisqueInstance: IntPtr (8 bytes)     │    │
│  └──────────────────┬────────────────────────┘    │
└─────────────────────┼─────────────────────────────┘
                      │ Points to
                      ▼
┌───────────────────────────────────────────────────┐
│              Native Heap (C++)                    │
│  ┌───────────────────────────────────────────┐    │
│  │  BrisqueAlgorithm instance                │    │
│  │  └─> QualityBRISQUE (SVM model ~555KB)    │    │
│  └───────────────────────────────────────────┘    │
└───────────────────────────────────────────────────┘
```

### 6.4 Safety Measures

| Issue | Mitigation |
|-------|------------|
| Forgetting Dispose() | Recommend `using` statement |
| Double disposal | `IntPtr.Zero` check prevents double-free |
| Invalid instance | Constructor throws if creation fails |

---

## 7. Platform Strategy

### 7.1 Windows-Focused Design

The library targets Windows exclusively because:

1. **Luxoria Integration**: Our main application (Luxoria) is a C# WinUI desktop app targeting Windows
2. **Visual Studio Ecosystem**: Primary development environment for the team
3. **NuGet Distribution**: Seamless package management in Visual Studio
4. **Enterprise Target**: Most enterprise deployments are Windows-based

### 7.2 Architecture Detection

```csharp
string architecture = RuntimeInformation.ProcessArchitecture switch {
    Architecture.X86 => "x86",
    Architecture.X64 => "x64",
    Architecture.Arm64 => "arm64",
    _ => throw new NotSupportedException("Unsupported architecture")
};
```

### 7.3 Supported Configurations

| Architecture | Platform | Status |
|--------------|----------|--------|
| x86 | Windows 32-bit | Supported |
| x64 | Windows 64-bit | Supported |
| ARM64 | Windows on ARM | Supported |

### 7.4 Native Library Loading

```csharp
private static void ExtractAndLoadNativeLibrary() {
    // 1. Detect architecture
    string architecture = RuntimeInformation.ProcessArchitecture switch { ... };

    // 2. Build resource name
    string resourceName = $"Luxoria.Algorithm.BrisqueScore.NativeLibraries.{architecture}.brisque_quality.dll";

    // 3. Extract to temp directory
    string tempPath = Path.Combine(Path.GetTempPath(), "LuxoriaNative");
    Directory.CreateDirectory(tempPath);

    // 4. Write resource to disk and load
    NativeLibrary.Load(dllPath);
}
```

This wrapper automatically selects and loads the correct native DLL (`x86`, `x64`, or `arm64`) based on the current process architecture.

In the original version, this selection was manual, meaning that callers had to choose which executable to run for each architecture, which partially negated the simplification goal because additional operations were required and not forgetting not-supported architecture.

---

## 8. Distribution Strategy

### 8.1 NuGet Package Structure

```
Luxoria.Algorithm.BrisqueScore.3.0.3.4100.nupkg
├── lib/net8.0/
│   └── Luxoria.Algorithm.BrisqueScore.dll
├── logo128x128.png
├── README.md
└── [embedded in assembly]
    ├── NativeLibraries.x86.brisque_quality.dll
    ├── NativeLibraries.x64.brisque_quality.dll
    └── NativeLibraries.arm64.brisque_quality.dll
```

### 8.2 Installation

```bash
# Via dotnet CLI
dotnet add package Luxoria.Algorithm.BrisqueScore

# Via Package Manager Console
Install-Package Luxoria.Algorithm.BrisqueScore
```

### 8.3 Usage in Luxoria

```csharp
using Luxoria.Algorithm.BrisqueScore;

// In your image processing service
public class ImageQualityService {
    public double AssessQuality(string imagePath) {
        using var brisque = new BrisqueInterop(
            "models/brisque_model_live.yml",
            "models/brisque_range_live.yml"
        );
        return brisque.ComputeScore(imagePath);
    }
}
```

### 8.4 Model Files

Model files are distributed separately (not in the NuGet package):

| File | Size | Purpose |
|------|------|---------|
| `brisque_model_live.yml` | 555 KB | Pre-trained SVM model |
| `brisque_range_live.yml` | 1.3 KB | Feature normalization ranges |

**Rationale**: Users may want custom-trained models. Keeping models separate follows separation of concerns.

---

## 9. Error Handling

### 9.1 Validation Layer

The C# wrapper provides comprehensive input validation before calling native code:

```csharp
public BrisqueInterop(string modelPath, string rangePath) {
    if (string.IsNullOrWhiteSpace(modelPath) || !File.Exists(modelPath))
        throw new FileNotFoundException($"Model file not found: {modelPath}");

    if (string.IsNullOrWhiteSpace(rangePath) || !File.Exists(rangePath))
        throw new FileNotFoundException($"Range file not found: {rangePath}");

    _brisqueInstance = CreateBrisqueAlgorithm(modelPath, rangePath);
    if (_brisqueInstance == IntPtr.Zero)
        throw new InvalidOperationException("Failed to create BRISQUE algorithm instance.");
}

public double ComputeScore(string imagePath) {
    if (string.IsNullOrWhiteSpace(imagePath) || !File.Exists(imagePath))
        throw new FileNotFoundException($"Image file not found: {imagePath}");

    return ComputeBrisqueScore(_brisqueInstance, imagePath);
}
```

### 9.2 Exception Types

| Scenario | Exception | Message Pattern |
|----------|-----------|-----------------|
| Model not found | `FileNotFoundException` | "Model file not found: {path}" |
| Range not found | `FileNotFoundException` | "Range file not found: {path}" |
| Image not found | `FileNotFoundException` | "Image file not found: {path}" |
| Native creation failure | `InvalidOperationException` | "Failed to create BRISQUE algorithm instance." |
| Unsupported architecture | `NotSupportedException` | "Unsupported architecture" |

### 9.3 Comparison with C++

| Error Type | C++ Behavior | C# Behavior |
|------------|--------------|-------------|
| Missing file | stderr + exit code | Structured exception |
| Invalid image | OpenCV exception | Pre-validated in managed code |
| Out of memory | std::bad_alloc | OutOfMemoryException |

---

## 10. API Design

### 10.1 Side-by-Side Comparison

**C++ Usage:**

```cpp
#include "BrisqueAlgorithm.hpp"

cv::Mat img = cv::imread("image.jpg", cv::IMREAD_COLOR);
cv::Mat gray;
cv::cvtColor(img, gray, cv::COLOR_BGR2GRAY);

luxoria::filter::algorithms::BrisqueAlgorithm brisque(
    "models/brisque_model_live.yml",
    "models/brisque_range_live.yml"
);

double score = brisque.compute(gray);
std::cout << "BRISQUE Score: " << score << std::endl;
```

**C# Usage:**

```csharp
using Luxoria.Algorithm.BrisqueScore;

using var brisque = new BrisqueInterop(
    "models/brisque_model_live.yml",
    "models/brisque_range_live.yml"
);

double score = brisque.ComputeScore("image.jpg");
Console.WriteLine($"BRISQUE Score: {score}");
```

### 10.2 API Improvements in C#

| Improvement | Description |
|-------------|-------------|
| Simpler input | File path instead of cv::Mat handling |
| Automatic grayscale | Conversion happens in native layer |
| Using statement | Resource cleanup via IDisposable |
| Strong typing | No need to handle OpenCV types |

---

## 11. Build System

### 11.1 C++ Build (CMake + vcpkg)

```cmake
cmake_minimum_required(VERSION 3.23)
project(BRISQUEQuality)

set(CMAKE_CXX_STANDARD 23)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

# Static runtime linking
set(CMAKE_MSVC_RUNTIME_LIBRARY "MultiThreaded$<$<CONFIG:Debug>:Debug>")

find_package(OpenCV REQUIRED)

add_executable(brisque_quality main.cpp)
target_include_directories(brisque_quality PUBLIC ${CMAKE_SOURCE_DIR}/includes)
target_link_libraries(brisque_quality ${OpenCV_LIBS})
```

### 11.2 C# Build (MSBuild)

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <GeneratePackageOnBuild>true</GeneratePackageOnBuild>
    <PackageId>Luxoria.Algorithm.BrisqueScore</PackageId>
    <Version>3.0.3.4100</Version>
  </PropertyGroup>

  <ItemGroup>
    <EmbeddedResource Include="NativeLibraries\arm64\brisque_quality.dll" />
    <EmbeddedResource Include="NativeLibraries\x64\brisque_quality.dll" />
    <EmbeddedResource Include="NativeLibraries\x86\brisque_quality.dll" />
  </ItemGroup>
</Project>
```

### 11.3 CI/CD Pipeline

**C++ Build Matrix** (GitHub Actions):
- Builds for x86, x64, arm64 on Windows
- Uses vcpkg for OpenCV dependency
- Outputs architecture-specific DLLs

**C# Build**:
- .NET 8.0 SDK
- Packages native DLLs as embedded resources
- Produces NuGet package

---

## 12. Lessons Learned

### 12.1 What Worked Well

1. **P/Invoke Approach**
   - Minimal code to maintain (~113 lines)
   - Leveraged battle-tested OpenCV implementation
   - Fast time to production

2. **Embedded Resources**
   - Seamless NuGet installation experience
   - No external dependency issues
   - Version consistency guaranteed

3. **Static Linking**
   - Single DLL per architecture
   - No dependency conflicts
   - Predictable deployment

4. **Comprehensive Validation**
   - Pre-flight checks catch errors early
   - Clear, actionable error messages
   - Prevents crashes in native code

### 12.2 Challenges Overcome

1. **vcpkg Integration**
   - Initial path configuration required iteration
   - Cross-architecture builds needed separate CI jobs
   - **Solution**: Documented workflow in CI/CD

2. **Resource Naming**
   - Assembly resource names must match exactly
   - **Solution**: Explicit LogicalName in .csproj

3. **Model File Distribution**
   - Initially confusing for users
   - **Solution**: Clear documentation and examples

### 12.3 TODO / Technical Debt

| Item | Priority | Description |
|------|----------|-------------|
| Async API | Low | Add ComputeScoreAsync for UI responsiveness |
| Batch processing | Low | Process multiple images efficiently |

---

## 13. Conclusion

### 13.1 Project Success Metrics

| Goal | Status |
|------|--------|
| NuGet package for Visual Studio | Achieved |
| Zero-configuration deployment | Achieved |
| Integration with Luxoria | Achieved |
| Native performance preserved | Achieved |

### 13.2 Final Architecture

The refactoring successfully transformed a C++ CLI tool into a polished .NET library:

```
┌────────────────────────────────────────────────────────────────┐
│                    Luxoria Application                         │
│                    (C# WinUI Desktop)                          │
└────────────────────────────┬───────────────────────────────────┘
                             │ NuGet Reference
                             ▼
┌────────────────────────────────────────────────────────────────┐
│           Luxoria.Algorithm.BrisqueScore (NuGet)               │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │  BrisqueInterop.cs                                       │  │
│  │  - Input validation                                      │  │
│  │  - Architecture detection                                │  │
│  │  - Resource extraction                                   │  │
│  │  - P/Invoke wrapper                                      │  │
│  │  - IDisposable pattern                                   │  │
│  └──────────────────────────────────────────────────────────┘  │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │  Embedded Native Libraries                               │  │
│  │  - x86/brisque_quality.dll (4.2 MB)                      │  │
│  │  - x64/brisque_quality.dll (5.6 MB)                      │  │
│  │  - arm64/brisque_quality.dll (3.9 MB)                    │  │
│  └──────────────────────────────────────────────────────────┘  │
└────────────────────────────────────────────────────────────────┘
```

### 13.3 Key Takeaways

1. **Wrap, don't rewrite**: When mature native libraries exist, wrapping is often the better choice
2. **Prioritize developer experience**: Zero-config installation pays dividends
3. **Validate early**: Catch errors in managed code before they reach native code
4. **Document decisions**: This journal captures the "why" for future maintainers

---

## Appendix A: File Reference

| File | Lines | Purpose |
|------|-------|---------|
| `BrisqueAlgorithm.hpp` | 41 | C++ wrapper class |
| `main.cpp` | 60 | C++ CLI entry point |
| `CMakeLists.txt` | 37 | C++ build config |
| `BrisqueScore.cs` | 113 | C# P/Invoke wrapper |
| `.csproj` | 43 | C# project config |

**Total Implementation**: ~250 lines of production code

---

## Appendix B: BRISQUE Algorithm Overview

### Algorithm Pipeline

1. **Preprocessing**: Convert to grayscale, compute local mean/variance
2. **MSCN Coefficients**: Mean Subtracted Contrast Normalized values
3. **Feature Extraction**: 36 features from GGD fitting
4. **Normalization**: Scale using pre-computed ranges
5. **Prediction**: SVM regression outputs quality score

### Model Details

| Parameter | Value |
|-----------|-------|
| SVM Type | EPS_SVR |
| Kernel | RBF (Radial Basis Function) |
| C | 1024 |
| gamma | 0.05 |
| epsilon | 0.001 |
| Support Vectors | 774 |
| Features | 36 |

---

## Appendix C: Glossary

| Term | Definition |
|------|------------|
| **BRISQUE** | Blind/Referenceless Image Spatial Quality Evaluator |
| **P/Invoke** | Platform Invocation Services - .NET native interop |
| **RAII** | Resource Acquisition Is Initialization (C++ pattern) |
| **IQA** | Image Quality Assessment |
| **NSS** | Natural Scene Statistics |
| **MSCN** | Mean Subtracted Contrast Normalized |
| **SVM** | Support Vector Machine |
| **vcpkg** | Microsoft C++ package manager |
| **WinUI** | Windows UI Library for modern Windows apps |

---

*Document Version: 1.0*
*Last Updated: January 2026*
*Repository: LuxoriaSoft/brisque_impl_netlib*
