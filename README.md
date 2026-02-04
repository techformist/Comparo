# Comparo - High-Performance File Comparison Tool

## Status

**Phase 1, 2, 4, 6, 7, 8 Complete** ✅ - Core engine, UI components, and performance optimizations implemented.

## Quick Start

### Clone and Setup

```bash
# Clone the repository with submodules
git clone --recurse-submodules https://github.com/techformist/Comparo.git
cd Comparo

# If already cloned, initialize submodules
git submodule update --init --recursive
```

For detailed setup instructions, see [SETUP.md](SETUP.md).

### Build the Solution

```bash
# Build Comparo Core
cd ../Comparo
dotnet build Comparo.Core/Comparo.Core.csproj

# Build Comparo UI (references ../Comparo)
cd ../Comparo.UI
dotnet build

# Run Avalonia UI application
dotnet run --project Comparo.UI.Avalonia
```

### Project Structure

```
c:\\dev\\1p\\dotnet\\
├── Comparo/               # Core diff engine, tests, docs
│   ├── Comparo.Core/       # Diff algorithms, parsers, semantic comparators
│   └── Comparo.Tests/      # Test suite
├── Comparo.UI/            # Avalonia UI application (includes shared controls/renderers)
│   └── Comparo.UI.Avalonia/  # Main Avalonia GUI
├── MetaComp.SF/           # Salesforce metadata comparison tool
│   ├── MetaComp.Core/      # SF CLI integration, metadata comparison
│   ├── MetaComp.UI.Avalonia/  # SF GUI application
│   └── MetaComp.Tests/     # SF test suite
└── Comparo (this repo)    # Documentation + solution for core/tests
```

## Documentation

- **PRD.md** - Full Product Requirements Document
- **EXECUTION_PLAN.md** - Detailed execution plan with 10 phases
- **PRD_METACOMP_SF.md** - Product Requirements for MetaComp-SF (Salesforce metadata comparison)

## Project Split Information

Comparo has been split into three independent projects for better reusability:

### Comparo Core

- **Location:** `../Comparo`
- **Contains:** Diff engine, semantic comparators, parsers, caching, performance tests
- **Can be used independently** by other applications

### Comparo UI

- **Location:** `../Comparo.UI`
- **Contains:** Avalonia GUI application (shared controls now live here)
- **Depends on:** Comparo Core

### MetaComp-SF

- **Location:** `../metacomp-sf`
- **Contains:** Salesforce metadata comparison tool
- **Uses:** Comparo Core for diff engine
- **Features:** SF CLI integration, metadata-type-aware comparison

## Key Features (Per PRD)

- ✅ Cross-platform UI (Avalonia)
- ✅ Semantic comparison for JSON/XML (NOT line-by-line)
- ✅ Line-based comparison for text/Markdown
- ✅ 60fps scrolling with virtualization
- ✅ Sub-500ms diff computation on 10MB files
- ✅ Support for txt, md, json, xml formats
- ✅ JSON Path and XPath-based change tracking
- ✅ Reordering detection for arrays and elements
- ✅ Inline diff visualization with character-level changes
- ✅ Memory-mapped file support for files >100MB
- ✅ Chunked loading for large files
- ✅ Comprehensive performance testing suite

## Implementation Status

| Phase    | Status         | Description                   |
| -------- | -------------- | ----------------------------- |
| Phase 1  | ✅ Complete    | Solution Setup                |
| Phase 2  | ✅ Complete    | Core Diff Engine              |
| Phase 3  | ✅ Complete    | Avalonia UI Setup             |
| Phase 4  | ✅ Complete    | UI Rendering & Virtualization |
| Phase 5  | ✅ Complete    | Diff Visualization            |
| Phase 6  | ✅ Complete    | Syntax Highlighting           |
| Phase 7  | ✅ Complete    | Navigation & UX               |
| Phase 8  | ✅ Complete    | Performance Optimization      |
| Phase 9  | ✅ Complete    | Testing                       |
| Phase 10 | ⏳ In Progress | Documentation & Polish        |

## Performance

Comparo is designed for high performance with quantified targets and real-world testing.

### Performance Targets

| Metric | Target | Status |
|--------|--------|--------|
| Diff computation (10MB files) | <500ms | ✓ On track |
| Memory efficiency | <5x file size | ✓ On track |
| Large file support | Up to 100MB | ✓ Tested |

### Test Data

Real-world test data is maintained in a separate repository:
- **Repository:** https://github.com/techformist/Comparo.TestData
- **Includes:** 20+ test scenarios with real files (JSON, XML, Salesforce Apex, etc.)
- **Access:** Automatically available via git submodules

### Running Performance Tests

```bash
# Run all performance tests
dotnet test --filter "FullyQualifiedName~Performance"

# Run real-world data tests (requires TestData submodule)
dotnet test --filter "FullyQualifiedName~RealWorldDataTests"
```

📊 **[Complete Performance Documentation](docs/PERFORMANCE.md)** - Detailed benchmarks, test data info, and competitive analysis

## Development Commands

```bash
# Run tests (from Comparo)
cd ../Comparo
dotnet test

# Clean build artifacts
dotnet clean

# Format code
dotnet format

# Add new NuGet package
dotnet add Comparo.Core/Comparo.Core.csproj package <PackageName>

# Add project reference
dotnet add Comparo.UI.Avalonia/Comparo.UI.Avalonia.csproj reference ../Comparo/Comparo.Core/Comparo.Core.csproj
```

## Current Application

The current implementation (Phases 1-8 complete) includes a fully functional Avalonia application with:

- Menu bar (File, View, Tools, Help)
- Side-by-side diff panes with synchronized scrolling
- File selection via dialogs and drag-and-drop
- Diff visualization with semantic awareness (JSON/XML)
- Syntax highlighting for txt, md, json, xml formats
- Navigation (next/previous diff, jump to line)
- Settings dialog for comparison options
- High-performance rendering with virtualization (60fps scrolling)
- Inline diff visualization with character-level changes
- Reordering detection and visualization
- Overview map showing all diffs

**To run:** `cd ../comparo-ui && dotnet run --project Comparo.UI.Avalonia`

## Semantic Comparison Notes

**JSON Comparison:**

- ✅ Property order ignored (per JSON spec)
- ✅ Array order preserved
- ✅ Type changes detected (string vs number)
- ✅ JSON Path-based change tracking

**XML Comparison:**

- ✅ Tag awareness (never breaks tags)
- ✅ Element order preserved
- ✅ Attribute order ignored (configurable)
- ✅ XPath-based change tracking

## License

TBD

## Contributing

TBD

---

**Note:** This project follows the architecture and requirements defined in PRD.md. Refer to PRD.md for detailed specifications.
