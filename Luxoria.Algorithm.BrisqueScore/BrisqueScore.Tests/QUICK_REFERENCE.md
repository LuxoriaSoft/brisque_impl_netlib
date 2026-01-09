# Quick Test Reference

## Run Commands

```powershell
# Run all tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific category
dotnet test --filter "FullyQualifiedName~Constructor"
dotnet test --filter "FullyQualifiedName~ComputeScore"
dotnet test --filter "FullyQualifiedName~Dispose"
dotnet test --filter "FullyQualifiedName~Concurrency"
dotnet test --filter "FullyQualifiedName~EdgeCase"

# Run with detailed output
dotnet test --logger "console;verbosity=detailed"

# Run and watch for changes
dotnet watch test
```

## Test Statistics

- **Total Tests**: 86
- **Passing**: 78
- **Skipped**: 8 (integration tests)
- **Execution Time**: ~1 second

## Coverage by Component

| Component | Tests | Focus Areas |
|-----------|-------|-------------|
| Constructor | 17 | Path validation, file checks, edge cases |
| ComputeScore | 13 | Image handling, path formats, post-dispose |
| Dispose | 11 | Resource cleanup, multiple calls, async |
| Concurrency | 10 | Thread-safety, parallel execution, stress |
| Native Library | 13 | Architecture, loading, platform support |
| Edge Cases | 18 | Unusual scenarios, special files, limits |
| Integration | 8 | Real-world usage (skipped by default) |

## Common Test Patterns

### Testing for Expected Exceptions
```csharp
var ex = Assert.Throws<FileNotFoundException>(() =>
{
    using var interop = new BrisqueInterop(null!, validPath);
});
Assert.Contains("Model file not found", ex.Message);
```

### Testing Concurrency
```csharp
Parallel.For(0, 10, i =>
{
    using var interop = new BrisqueInterop(modelPath, rangePath);
    // Operations...
});
```

### Testing Resource Cleanup
```csharp
var interop = new BrisqueInterop(modelPath, rangePath);
interop.Dispose();
interop.Dispose(); // Should not throw
```

## Debugging Failed Tests

1. **Check file paths** - Ensure temp directories are accessible
2. **Verify permissions** - Some tests create read-only/hidden files
3. **Check native library** - Most tests expect InvalidOperationException when library isn't loaded
4. **Review cleanup** - Tests clean up temp files in Dispose()

## Adding New Tests

1. Choose appropriate test class
2. Follow naming: `MethodName_Scenario_ExpectedBehavior`
3. Clean up resources in Dispose()
4. Handle expected InvalidOperationException from constructor
5. Update this README with new test count

## Integration Test Setup

To enable integration tests:

1. Place model files:
   - `models/brisque_model_live.yml`
   - `models/brisque_range_live.yml`

2. Add test image:
   - `img.jpg`

3. Remove `Skip` attribute from tests

4. Ensure native library is properly loaded
