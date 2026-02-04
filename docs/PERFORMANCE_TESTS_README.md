# Comparo Performance Test Suite

This directory contains comprehensive performance tests for the Comparo Core library using BenchmarkDotNet.

## Test Files

### Core Performance Test Files

1. **TestDataGenerator.cs** - Helper class for generating test data
   - Generates text, markdown, JSON, and XML files of various sizes
   - Creates files with different change percentages
   - Supports reordering tests

2. **FileComparisonPerformanceTests.cs** - Single file comparison benchmarks
   - Tests file sizes: 10KB, 100KB, 1MB, 10MB
   - Tests formats: txt, md, json, xml
   - Measures: Time to compare, Memory usage, CPU usage

3. **MultiFileComparisonPerformanceTests.cs** - Multi-file comparison benchmarks
   - Tests file counts: 10, 50, 100, 1000
   - Tests formats: txt, md, json, xml
   - Measures: Total time, Time per file, Memory usage

4. **AlgorithmPerformanceTests.cs** - Algorithm-specific benchmarks
   - Compares Myers, Patience, Histogram algorithms
   - Tests with identical files, small changes, large changes
   - Tests with reordered content

5. **SemanticComparisonPerformanceTests.cs** - JSON/XML semantic benchmarks
   - JSON semantic comparison with various nesting levels
   - XML semantic comparison with various tag depths
   - Compares semantic vs line-based diff performance

6. **MemoryUsagePerformanceTests.cs** - Memory usage validation
   - Tests memory usage for large files (10MB, 100MB)
   - Verifies <5x file size target
   - Memory leak detection tests

7. **CachingPerformanceTests.cs** - Cache efficiency tests
   - Cache hit vs cache miss performance
   - LRU eviction performance
   - Repeated comparison performance
   - Concurrent access tests

## Running the Benchmarks

### Running All Benchmarks

To run all performance benchmarks, use the BenchmarkDotNet CLI:

```bash
cd C:\dev\1p\dotnet\comparo-core
dotnet run --project Comparo.Tests --configuration Release
```

Or using BenchmarkDotNet directly:

```bash
cd C:\dev\1p\dotnet\comparo-core\Comparo.Tests\bin\Release\net8.0
dotnet Comparo.Tests.dll
```

### Running Specific Benchmarks

To run specific benchmark classes, you can modify the Program.cs or create a custom runner:

```csharp
using BenchmarkDotNet.Running;
using Comparo.Tests.Performance;

class Program
{
    static void Main(string[] args)
    {
        BenchmarkRunner.Run<FileComparisonPerformanceTests>();
        BenchmarkRunner.Run<AlgorithmPerformanceTests>();
        // Add more benchmark classes as needed
    }
}
```

### Running Benchmarks with xUnit

Note: These are **BenchmarkDotNet** benchmarks, not xUnit tests. They require:

1. Release build configuration (not Debug)
2. Separate console application execution
3. BenchmarkDotNet runtime instrumentation

## Performance Targets

From the PRD, the key performance targets are:

| Metric | Target |
|--------|--------|
| Diff computation (10MB files) | <500ms |
| Memory efficiency | <5x file size |
| Scroll latency (UI) | <16ms (N/A for core tests) |
| Startup time (UI) | <500ms (N/A for core tests) |

## Test Data Generation

The `TestDataGenerator` class provides:

- **Text files**: Lines with random words
- **Markdown files**: Various markdown elements (headings, lists, code blocks, etc.)
- **JSON files**: Nested objects and arrays with configurable depth
- **XML files**: Nested elements with configurable depth

## Benchmark Categories

### 1. File Size Benchmarks

Tests performance across file sizes:
- 10KB (~200 lines)
- 100KB (~2,000 lines)
- 1MB (~20,000 lines)
- 10MB (~200,000 lines)

### 2. Algorithm Benchmarks

Compares three diff algorithms:
- **MyersDiff**: Classic O(ND) difference algorithm
- **PatienceDiff**: Optimized for code changes
- **HistogramDiff**: Fast histogram-based algorithm

### 3. Format-Specific Benchmarks

Tests performance for different file formats:
- Plain text (.txt)
- Markdown (.md)
- JSON (.json)
- XML (.xml)

### 4. Change Percentage Benchmarks

Tests behavior with different amounts of changes:
- Identical files (0% changes)
- Small changes (1%, 5%, 10%)
- Large changes (25%, 50%)
- Reordered content

### 5. Memory Benchmarks

Measures:
- Peak memory usage
- Memory efficiency ratio
- Memory leak detection
- Process memory monitoring

### 6. Caching Benchmarks

Tests cache performance:
- Cache hit vs miss
- LRU eviction
- Repeated comparisons
- Concurrent access

## Performance Report

After running benchmarks, results are saved to:
`C:\dev\1p\dotnet\comparo-core\Comparo.Tests\BenchmarkDotNet.Artifacts\results\`

To generate the performance report, run the benchmarks and copy the results to:
`C:\dev\1p\dotnet\comparo-core\PERFORMANCE_TEST_RESULTS.md`

## Best Practices

1. **Always run in Release mode** - Debug mode adds overhead
2. **Close other applications** - Ensure consistent environment
3. **Run multiple iterations** - Allow warm-up and stable measurements
4. **Use consistent hardware** - Results vary across systems
5. **Document environment** - CPU, RAM, OS version

## Interpreting Results

### Key Metrics

- **Mean**: Average execution time
- **StdDev**: Standard deviation (consistency)
- **Allocated**: Memory allocated during benchmark
- **Gen0/Gen1/Gen2**: Garbage collection generations

### What to Look For

1. **Performance regressions**: Compare with previous runs
2. **Memory leaks**: Increasing memory over iterations
3. **Algorithm selection**: Choose fastest algorithm for use case
4. **Format differences**: Some formats may be slower than others

## Troubleshooting

### Common Issues

1. **Build fails in Debug mode** - Always use Release configuration
2. **Benchmarks take too long** - Reduce iteration count or file sizes
3. **Out of memory errors** - Reduce file sizes in benchmarks
4. **Inconsistent results** - Ensure system is not under load

## Additional Resources

- [BenchmarkDotNet Documentation](https://benchmarkdotnet.org/)
- [Comparo Performance Research](../PERFORMANCE_BENCHMARK_RESEARCH.md)
- [Comparo PRD](../PRD.md)
