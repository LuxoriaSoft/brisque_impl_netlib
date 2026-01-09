# BRISQUE Interop Comprehensive Tests

This test suite provides extensive coverage of the `BrisqueInterop` class, covering various scenarios and edge cases.

## Test Organization

### 1. **BrisqueInteropConstructorTests** (17 tests)
Tests for constructor validation and initialization:
- Null, empty, and whitespace path handling
- Non-existent file detection
- Special characters and long paths
- Relative paths
- Path validation for both model and range files

### 2. **BrisqueInteropComputeScoreTests** (13 tests)
Tests for the `ComputeScore` method:
- Null, empty, and whitespace image path handling
- Non-existent image detection
- Invalid image formats
- Zero-byte images
- Special characters and Unicode in paths
- Multiple sequential score computations
- Post-dispose behavior
- Long paths

### 3. **BrisqueInteropDisposeTests** (11 tests)
Tests for resource cleanup and disposal:
- Single and multiple dispose calls
- Using statements (both syntax forms)
- Dispose without using
- Finalizer behavior
- Post-dispose access
- Multiple instance disposal
- Async context disposal
- Exception handling during disposal

### 4. **BrisqueInteropConcurrencyTests** (10 tests)
Tests for thread-safety and concurrent usage:
- Concurrent instance creation
- Asynchronous instance creation
- Concurrent score computation on single instance
- Multi-threaded usage
- Rapid create/dispose cycles
- Concurrent dispose and compute operations
- Stress testing with many operations
- Thread-safe initialization

### 5. **BrisqueInteropNativeLibraryTests** (13 tests)
Tests for native library loading and architecture detection:
- Architecture detection (x86, x64, ARM64)
- Embedded resource validation
- Temp directory creation
- Runtime information verification
- DllImport calling convention
- Static constructor behavior
- Architecture mapping
- Unsupported architecture handling

### 6. **BrisqueInteropEdgeCaseTests** (18 tests)
Tests for unusual edge cases and scenarios:
- Read-only and hidden files
- Symbolic links
- Very large files (models and images)
- Empty files
- Binary garbage data
- Network paths (UNC)
- Same file for model and range
- Directories instead of files
- Mixed path separators
- Trailing slashes
- Null characters in paths
- Reserved Windows filenames (CON, PRN, etc.)
- Low memory scenarios

### 7. **BrisqueInteropIntegrationTests** (9 tests - skipped by default)
Integration tests that require real model files and native library:
- Real model file initialization
- Real image score computation
- Consistency checks
- Quality comparison tests
- Multiple instance management
- Dispose and recreate scenarios
- Performance benchmarks
- YAML validation

## Running the Tests

### Run all tests:
```powershell
dotnet test
```

### Run specific test class:
```powershell
dotnet test --filter "FullyQualifiedName~BrisqueInteropConstructorTests"
```

### Run with detailed output:
```powershell
dotnet test --logger "console;verbosity=detailed"
```

### Run integration tests (requires setup):
```powershell
# First, ensure model files and native library are available
dotnet test --filter "FullyQualifiedName~IntegrationTests"
```

## Test Expectations

**Important Notes:**
1. Most tests expect `InvalidOperationException` because the native library may not be properly loaded in the test environment
2. Constructor validation (null checks, file existence) is tested and should work regardless of native library status
3. Integration tests are skipped by default and require:
   - Real model files (`brisque_model_live.yml`, `brisque_range_live.yml`)
   - Properly loaded native library (`brisque_quality.dll`)
   - Test images for score computation

## Coverage Summary

Total: **91 tests** covering:
- ✅ Constructor validation (input validation, path handling)
- ✅ Score computation (all edge cases)
- ✅ Resource cleanup (disposal patterns)
- ✅ Concurrency (thread-safety, async operations)
- ✅ Native library (architecture detection, loading)
- ✅ Edge cases (file attributes, special paths, error conditions)
- ✅ Integration scenarios (requires real setup)

## Test Data Requirements

For integration tests, place test files in one of these locations:
- `./models/brisque_model_live.yml`
- `./models/brisque_range_live.yml`
- `./img.jpg` (test image)

## Continuous Integration

These tests are designed to run in CI/CD environments:
- Unit tests pass without external dependencies
- Integration tests are skipped automatically if requirements aren't met
- No manual setup required for basic test execution

## Contributing

When adding new tests:
1. Follow the naming convention: `MethodName_Scenario_ExpectedBehavior`
2. Add appropriate test category (Constructor, ComputeScore, Dispose, etc.)
3. Document expected exceptions for native library failures
4. Use `Skip.IfNot()` for conditional integration tests
5. Clean up resources in `Dispose()` method
