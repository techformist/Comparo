# Product Requirements Document (PRD)

## Comparo - High-Performance File Comparison Tool

---

## 1. Executive Summary

**Product Name:** Comparo  
**Version:** 1.0  
**Status:** Draft  
**Platform:** .NET 8.0 / C# 12  
**Target Platforms:** Windows, macOS, Linux (via Avalonia) and Windows-only (via WPF)

Comparo is a high-performance file comparison tool designed for developers and content creators who need fast, accurate side-by-side comparison of text files. The tool supports plain text, Markdown, JSON, and XML formats with syntax-aware comparison capabilities. Key differentiators include sub-second diff computation on large files, 60fps scrolling performance, and intelligent diff algorithms tailored to each file format.

---

## 2. Problem Statement

### Current Pain Points

1. **Performance Issues**
   - Existing diff tools (WinMerge, Meld, Beyond Compare) struggle with files >10MB
   - Scrolling becomes laggy (30-100ms per frame) on large files
   - Diff computation blocks UI for >5 seconds on typical codebase files
   - Memory consumption grows linearly with file size

2. **Format Limitations**
   - Most tools treat JSON/XML as plain text, producing meaningless diffs on formatting changes
   - No semantic awareness for structured formats
   - Syntax highlighting is often basic or inaccurate
   - No intelligent handling of whitespace in code files

3. **UX Friction**
   - Complex keyboard shortcuts and navigation patterns
   - Inconsistent color schemes across tools
   - Poor visualization of inline changes
   - No real-time diff updates during editing

### Target Users

- **Software Developers:** Need fast code reviews, Git diff visualization, and merge conflict resolution
- **Data Engineers:** Compare large JSON/XML datasets, configuration files
- **Technical Writers:** Review Markdown documentation changes
- **DevOps Engineers:** Compare deployment manifests, infrastructure-as-code files

---

## 3. Solution Overview

Comparo provides a dual-mode interface (TUI and GUI) with:

- **Sub-500ms diff computation** on 10MB files
- **60fps scrolling** with synchronized panes
- **Format-aware comparison** (semantic diffs for JSON/XML)
- **Syntax highlighting** for all supported formats
- **Memory-efficient virtualization** for files up to 1GB

### Core Features

| Feature                   | TUI | GUI | Description                                  |
| ------------------------- | --- | --- | -------------------------------------------- |
| Side-by-side diff         | ✅  | ✅  | Left/right panes with synchronized scrolling |
| Format-aware comparison   | ✅  | ✅  | Semantic diffs for JSON/XML                  |
| Syntax highlighting       | ✅  | ✅  | txt, md, json, xml                           |
| Fast scrolling            | ✅  | ✅  | Virtual scrolling with 60fps                 |
| Inline diff visualization | ✅  | ✅  | Character-level changes highlighted          |
| Navigation shortcuts      | ✅  | ✅  | Jump to next/previous diff                   |
| Overview map              | ✅  | ✅  | Mini-map showing all diffs                   |
| Export diffs              | ✅  | ✅  | Unified diff format                          |

---

## 4. Technical Requirements

### 4.1 Framework Selection

#### Primary Choice: Avalonia UI (Cross-Platform)

**Rationale:**

- Native virtualization via `VirtualizingStackPanel`
- Skia-based GPU-accelerated rendering
- XAML-based (familiar to WPF developers)
- Active community (30k+ stars)
- Cross-platform support (Windows/macOS/Linux)
- AvaloniaEdit provides mature text editing components

#### Secondary Choice: WPF (Windows-Only)

**Rationale:**

- Best-in-class `VirtualizingStackPanel` with recycling mode
- Hardware-accelerated DirectX rendering
- Largest ecosystem and third-party controls
- Excellent documentation and community support
- `FlowDocumentScrollViewer` for rich text

#### TUI Choice: Terminal.Gui

**Rationale:**

- Native `TextView` component with scroll events
- Cross-platform (Windows/macOS/Linux)
- TrueColor support for syntax highlighting
- Built-in scroll synchronization via event system
- Active development despite Alpha status

### 4.2 Architecture

```
Comparo
├── Comparo.Core (Diff Engine)
│   ├── DiffAlgorithms/
│   │   ├── MyersDiff.cs          // Optimal edit distance
│   │   ├── PatienceDiff.cs       // Code-friendly output
│   │   ├── HistogramDiff.cs      // Fast for repetitive content
│   │   └── HuntSzymanskiDiff.cs  // Line-based optimization
│   ├── StructuredComparators/
│   │   ├── IStructuredComparator.cs
│   │   ├── JsonSemanticComparator.cs    // JSON Path-based comparison
│   │   ├── XmlSemanticComparator.cs     // DOM tree comparison
│   │   └── ReorderingDetector.cs       // Array/element reordering
│   ├── FileParsers/
│   │   ├── IFileParser.cs
│   │   ├── TextParser.cs
│   │   ├── MarkdownParser.cs
│   │   ├── JsonParser.cs         // Semantic parsing to object model
│   │   └── XmlParser.cs          // Semantic parsing to DOM tree
│   ├── DiffModels/
│   │   ├── DiffHunk.cs
│   │   ├── DiffBlock.cs
│   │   ├── ChangeType.cs         // Inserted, Deleted, Unchanged, Reordered
│   │   ├── SideBySideModel.cs
│   │   ├── JsonPathChange.cs     // JSON Path-based change descriptor
│   │   └── XPathChange.cs        // XPath-based change descriptor
│   ├── Normalizers/
│   │   ├── JsonNormalizer.cs     // Sort properties, normalize whitespace
│   │   └── XmlNormalizer.cs      // Handle namespaces, normalize attributes
│   └── Caching/
│       ├── DiffResultCache.cs
│       ├── LineHashCache.cs
│       └── StructureCache.cs    // Cache parsed JSON/XML structures
│
├── Comparo.UI.Common (Shared UI Components)
│   ├── Rendering/
│   │   ├── VirtualTextView.cs
│   │   ├── LineRenderCache.cs
│   │   ├── DoubleBufferRenderer.cs
│   │   └── SemanticDiffRenderer.cs  // Render semantic diffs as text
│   ├── Highlighting/
│   │   ├── SyntaxHighlighter.cs
│   │   ├── HighlightCache.cs
│   │   └── ThemeManager.cs
│   └── Controls/
│       ├── DiffView.cs
│       ├── SynchronizedScrollView.cs
│       └── OverviewMap.cs
│
├── Comparo.UI.Avalonia (GUI Implementation)
│   ├── Views/
│   │   ├── MainWindow.axaml
│   │   ├── DiffPane.axaml
│   │   └── SettingsDialog.axaml
│   ├── ViewModels/
│   │   ├── MainViewModel.cs
│   │   └── DiffViewModel.cs
│   └── Controls/
│       └── AvaloniaDiffViewer.cs
│
├── Comparo.UI.WPF (Windows-Only GUI)
│   ├── Views/
│   │   ├── MainWindow.xaml
│   │   └── DiffPane.xaml
│   └── ViewModels/
│
├── Comparo.UI.TUI (Terminal UI)
│   ├── Views/
│   │   └── MainTuiView.cs
│   └── Controllers/
│       └── TuiScrollSync.cs
│
└── Comparo.Tests
    ├── DiffTests.cs
    ├── SemanticDiffTests.cs     // JSON/XML semantic comparison tests
    ├── PerformanceTests.cs
    └── EndToEndTests.cs
```

**Semantic Comparison Pipeline:**

```
File Input (JSON or XML)
    ↓
File Parser (JsonParser/XmlParser)
    ↓
Parse to Object Tree (JToken/IDocument)
    ↓
Normalizer (sort properties, handle whitespace)
    ↓
Structured Comparator
    ├─ Compare trees recursively
    ├─ Track changes via JSON Path/XPath
    └─ Detect reordering
    ↓
Semantic Diff Model
    ├─ JsonPathChange[] or XPathChange[]
    └─ DiffBlock[] (for UI rendering)
    ↓
Semantic Diff Renderer
    ├─ Map semantic changes to line numbers
    ├─ Generate side-by-side text view
    └─ Highlight reordering with distinct colors
    ↓
UI Display (TUI or GUI)
```

### 4.3 Diff Algorithm Selection

| Use Case             | Algorithm              | Rationale                                 |
| -------------------- | ---------------------- | ----------------------------------------- |
| General text diff    | Myers (optimized)      | Optimal edit distance, industry standard  |
| Source code diff     | Patience               | Better readability, less "chaff"          |
| Large files (>10MB)  | Histogram              | Faster for repetitive content             |
| Merge conflicts      | Three-way Myers        | Conflict detection and resolution         |
| JSON structural diff | Tree-based + JSON Path | Semantic comparison at object/array level |
| XML structural diff  | DOM tree comparison    | Tag-aware, preserves structure            |

### 4.4 Semantic Comparison for Structured Formats

**CRITICAL REQUIREMENT:** JSON and XML comparison MUST NOT be line-by-line. Instead, comparisons must be structure-aware and semantic.

#### 4.4.1 JSON Semantic Comparison

JSON files are parsed into their object model first, then compared at the semantic level:

**Key Principles:**

1. **Order Independence:**
   - Object properties: Order is irrelevant (JSON objects are unordered by spec)
   - Array elements: Order matters (arrays are ordered sequences)
   - Example: `{"name": "John", "age": 30}` is equivalent to `{"age": 30, "name": "John"}`

2. **Structure Awareness:**
   - Compare JSON nodes, not text lines
   - Detect value changes, not just line changes
   - Understand nesting depth and hierarchy
   - Track changes via JSON Path (RFC 6902 format)

3. **Type Detection:**
   - Detect type changes: `{"value": "123"}` → `{"value": 123}`
   - Track null vs missing properties
   - Distinguish between empty arrays and missing arrays

4. **Whitespace Handling:**
   - Ignore formatting whitespace by default
   - Preserve significant whitespace in string values
   - Configurable: "Ignore all whitespace" vs "Ignore only structural whitespace"

**Comparison Strategy:**

```csharp
// Example: Semantic JSON comparison
File A:
{
  "name": "John",
  "age": 30,
  "hobbies": ["reading", "coding"]
}

File B:
{
  "age": 30,
  "name": "John Doe",      // Property value changed
  "hobbies": ["coding", "reading"],  // Array order changed
  "active": true           // Property added
}

Diff Output (Semantic):
- $.name: "John" → "John Doe"
- $.hobbies: Array reordered (elements preserved)
+ $.active: true added
```

**JSON Path-Based Change Tracking:**

| Change Type           | JSON Path              | Description                   |
| --------------------- | ---------------------- | ----------------------------- |
| Value modified        | `$.users[0].name`      | Property value changed        |
| Property added        | `$.metadata.createdAt` | New property added to object  |
| Property removed      | `$.legacyField`        | Property deleted              |
| Type changed          | `$.count`              | String "5" → Number 5         |
| Array element added   | `$.items[2]`           | New element at index 2        |
| Array element removed | `$.items[1]`           | Element at index 1 deleted    |
| Array reordered       | `$.items[*]`           | Elements reordered (detected) |
| Nested object changed | `$.config.server.port` | Deep nested modification      |

**Implementation:**

- Use `JsonDiffPatch.Net` for semantic diff
- Parse JSON to `JToken` (Newtonsoft.Json) or `JsonNode` (System.Text.Json)
- Normalize objects (sort properties alphabetically)
- Compare tree structure using recursive depth-first traversal
- Generate RFC 6902 JSON Patch format for export

#### 4.4.2 XML Semantic Comparison

XML files are parsed into DOM trees, then compared at the node level:

**Key Principles:**

1. **Tag Awareness:**
   - Compare XML elements as nodes, not text lines
   - Preserve tag hierarchy and nesting
   - Never break tags (won't produce invalid XML like `<div`text</div>`)

2. **Attribute Handling:**
   - Attribute order: Irrelevant (configurable to respect order)
   - Attribute names: Case-sensitive (XML spec)
   - Attribute values: Whitespace matters (unless CDATA)
   - Detect added/removed/changed attributes

3. **Namespace Awareness:**
   - Respect XML namespace declarations
   - Compare elements in correct namespace context
   - Track namespace prefix changes vs actual namespace changes

4. **Content vs Structure:**
   - Element content: Compared as text (whitespace configurable)
   - Mixed content: Preserve text node positions between elements
   - CDATA sections: Treated as opaque text content

5. **Order Sensitivity:**
   - Element order: Matters (XML is ordered)
   - Attribute order: Doesn't matter (configurable)
   - Comments/processing instructions: Order preserved

**Comparison Strategy:**

```xml
<!-- Example: Semantic XML comparison -->
File A:
<config>
  <server port="8080" host="localhost">
    <timeout>30</timeout>
  </server>
</config>

File B:
<config>
  <server host="localhost" port="8081">  <!-- Attribute changed, order swapped -->
    <timeout>30</timeout>
    <debug>true</debug>                   <!-- Element added -->
  </server>
</config>

Diff Output (Semantic):
- /config/server/@port: "8080" → "8081"
+ /config/server/debug: Element added
```

**XPath-Based Change Tracking:**

| Change Type              | XPath                                    | Description               |
| ------------------------ | ---------------------------------------- | ------------------------- |
| Attribute value changed  | `/config/server/@port`                   | Attribute value modified  |
| Attribute added          | `/config/server/@ssl`                    | New attribute added       |
| Attribute removed        | `/config/server/@legacy`                 | Attribute deleted         |
| Element content changed  | `/config/server/timeout`                 | Text content modified     |
| Element added            | `/config/server/debug`                   | New child element         |
| Element removed          | `/config/server/logging`                 | Child element deleted     |
| Element reordered        | `/config/server/*[2]`                    | Element moved in sequence |
| Namespace prefix changed | `/config/*[namespace-uri()='http://ns']` | Namespace change          |

**Implementation:**

- Use `AngleSharp.Diffing` for DOM tree comparison
- Parse XML to `IDocument` (AngleSharp) or `XmlDocument` (System.Xml)
- Compare trees using tree-edit distance algorithms
- Generate XPath-based change descriptions
- Support configurable options (attribute order sensitivity, whitespace handling)

#### 4.4.3 Reordering Detection

Both JSON and XML comparison must detect and report reordering:

**JSON Array Reordering:**

```json
// File A
{"items": ["apple", "banana", "cherry"]}

// File B
{"items": ["banana", "apple", "cherry"]}

// Diff: Array reordered at $.items
// Shows: Elements [0] and [1] swapped positions
```

**XML Element Reordering:**

```xml
<!-- File A -->
<fruits>
  <fruit>apple</fruit>
  <fruit>banana</fruit>
</fruits>

<!-- File B -->
<fruits>
  <fruit>banana</fruit>
  <fruit>apple</fruit>
</fruits>

<!-- Diff: Elements reordered at /fruits/fruit[0] and /fruits/fruit[1] -->
```

**Reordering Visualization:**

- Highlight reordered elements with a distinct color (e.g., orange)
- Show original and new positions in the diff
- Provide "Ignore order" option for cases where order doesn't matter
- Detect element-level reordering (smart matching, not naive index comparison)

#### 4.4.4 Comparison Settings (User Configurable)

| Setting                       | JSON | XML | Default               |
| ----------------------------- | ---- | --- | --------------------- |
| Ignore property/element order | ✅   | ❌  | JSON: Yes, XML: No    |
| Ignore whitespace             | ✅   | ✅  | Yes (structural only) |
| Respect attribute order       | N/A  | ✅  | No                    |
| Detect type changes           | ✅   | N/A | Yes                   |
| Ignore comments               | N/A  | ✅  | No                    |
| Ignore namespaces             | N/A  | ✅  | No                    |
| Detect array reordering       | ✅   | N/A | Yes                   |

### 4.5 Semantic vs Line-by-Line Comparison: Detailed Examples

#### 4.5.1 Why Line-by-Line Comparison Fails for JSON/XML

**Problem with Line-by-Line Comparison:**

When treating JSON/XML as plain text, diff tools produce meaningless results on formatting changes:

```json
// File A (left)
{
  "name": "John",
  "age": 30
}

// File B (right) - Same content, different formatting
{
  "name": "John",
  "age": 30
}

// Line-by-line diff shows:
- Line 2: { "name": "John",
+ Line 2: { "name": "John",
- Line 3:   "age": 30
+ Line 3:   "age": 30

// Result: Shows 2 changes when there are 0 semantic changes!
```

```xml
<!-- File A (left) -->
<server>
  <host>localhost</host>
  <port>8080</port>
</server>

<!-- File B (right) - Same content, attribute order swapped -->
<server>
  <port port="8080">
    <host>localhost</host>
  </port>
</server>

<!-- Line-by-line diff shows:
- Line 2: <host>localhost</host>
+ Line 2: <port port="8080">
+ Line 3:   <host>localhost</host>
+ Line 4: </port>
- Line 3: <port>8080</port>

Result: Shows complete structural change when only attribute order changed!
-->
```

#### 4.5.2 Semantic Comparison: The Right Approach

**JSON Semantic Comparison Example:**

```json
// File A
{
  "firstName": "John",
  "lastName": "Doe",
  "age": 30,
  "hobbies": ["reading", "coding", "gaming"],
  "address": {
    "street": "123 Main St",
    "city": "NYC",
    "zip": "10001"
  }
}

// File B - Multiple changes, some reordered
{
  "lastName": "Doe",
  "firstName": "Johnathan",  // Changed
  "age": 31,                 // Changed
  "hobbies": ["coding", "reading", "hiking"],  // Reordered + one new
  "address": {
    "street": "123 Main St",
    "city": "San Francisco",  // Changed
    "zip": "94105",           // Changed
    "country": "USA"           // Added
  }
}

// SEMANTIC DIFF (NOT line-by-line):
CHANGED: $.firstName: "John" → "Johnathan"
CHANGED: $.age: 30 → 31
REORDERED: $.hobbies[*] (indices [0,1] swapped)
  - "reading", "coding" → "coding", "reading"
CHANGED: $.hobbies[2]: "gaming" → "hiking"
CHANGED: $.address.city: "NYC" → "San Francisco"
CHANGED: $.address.zip: "10001" → "94105"
ADDED: $.address.country: "USA"

// Total changes: 7 semantic changes (not line-based)
// Property order between "firstName" and "lastName" ignored
```

**XML Semantic Comparison Example:**

```xml
<!-- File A -->
<configuration>
  <server host="localhost" port="8080" ssl="false">
    <timeout>30</timeout>
  </server>
  <database name="appdb" poolSize="10">
    <connectionString>server=localhost;database=appdb</connectionString>
  </database>
</configuration>

<!-- File B - Multiple structural changes -->
<configuration>
  <database name="appdb" poolSize="20">    <!-- Moved up, poolSize changed -->
    <connectionString>server=localhost;database=appdb;timeout=30</connectionString>
  </database>
  <server port="8081" host="localhost">   <!-- Moved down, port changed, order swapped -->
    <timeout>60</timeout>                  <!-- Changed -->
    <ssl>true</ssl>                        <!-- Added -->
  </server>
</configuration>

<!-- SEMANTIC DIFF (NOT line-by-line):
MOVED: /configuration/database (position 2 → 1)
CHANGED: /configuration/database/@poolSize: "10" → "20"
CHANGED: /configuration/database/connectionString: "server=localhost;database=appdb" → "server=localhost;database=appdb;timeout=30"
MOVED: /configuration/server (position 1 → 2)
CHANGED: /configuration/server/@port: "8080" → "8081"
CHANGED: /configuration/server/timeout: "30" → "60"
ADDED: /configuration/server/ssl: "true"

Total changes: 7 semantic changes (not line-based)
Attribute order in <server> tag ignored: port/host vs host/port
-->

// Line-by-line would show:
// -15 lines removed, +15 lines added (even though it's the same structure!)
```

#### 4.5.3 Reordering Detection Examples

**JSON Array Reordering:**

```json
// File A
{"items": ["apple", "banana", "cherry", "date"]}

// File B
{"items": ["banana", "date", "apple", "cherry"]}

// Semantic diff shows:
REORDERED: $.items[*]
  - apple moved from [0] to [2]
  - banana moved from [1] to [0]
  - cherry moved from [2] to [3]
  - date moved from [3] to [1]

// UI visualization:
// Show reordered items in orange color
// Draw arrows connecting original positions to new positions
```

**XML Element Reordering:**

```xml
<!-- File A -->
<menu>
  <item id="1">File</item>
  <item id="2">Edit</item>
  <item id="3">View</item>
  <item id="4">Help</item>
</menu>

<!-- File B -->
<menu>
  <item id="1">File</item>
  <item id="3">View</item>
  <item id="2">Edit</item>
  <item id="4">Help</item>
</menu>

<!-- Semantic diff:
MOVED: /menu/item[2] (id="3") moved from [2] to [1]
MOVED: /menu/item[1] (id="2") moved from [1] to [2]

UI shows View and Edit highlighted in orange with swap indicators
-->
```

#### 4.5.4 UI Visualization of Semantic Diffs

**How Semantic Diffs are Displayed:**

1. **Side-by-Side View:**
   - Left pane: Original file (preserving original formatting)
   - Right pane: Modified file (preserving original formatting)
   - Diff markers aligned at semantic level, not line level

2. **Semantic Change Indicators:**
   - Added: Green highlight (new property/element)
   - Removed: Red highlight (deleted property/element)
   - Changed: Yellow highlight (modified value)
   - Reordered: Orange highlight (moved position)

3. **Path-Based Navigation:**
   - JSON: Clicking on change shows JSON Path (e.g., `$.users[0].address.city`)
   - XML: Clicking on change shows XPath (e.g., `/config/server/@port`)
   - Navigate by path in overview map

4. **Reordering Visualization:**
   - Arrows connecting original position to new position
   - "Moved from line X to line Y" tooltip
   - Diff summary shows "3 items reordered"

**Example UI Display for JSON:**

```
┌─────────────────────────┬─────────────────────────┐
│ Line  │ File A (original) │ Line  │ File B (modified) │
├───────┼──────────────────┼───────┼──────────────────┤
│ 1     │ {                 │ 1     │ {                 │
│ 2     │   "name": "John", │ 2     │   "age": 31,      │ YELLOW (changed)
│ 3     │   "age": 30,      │ 3     │   "name": "John", │ GREEN (moved)
│ 4     │   "tags": [       │ 4     │   "tags": [       │
│ 5     │     "dev",        │ 5     │     "qa",         │ ORANGE (reordered)
│ 6     │     "qa"          │ 6     │     "dev"         │ ORANGE (reordered)
│ 7     │   ]               │ 7     │   ],              │
│ 8     │ }                 │ 8     │   "active": true  │ GREEN (added)
│       │                   │ 9     │ }                 │
└─────────────────────────┴─────────────────────────┘

Summary: 3 changes (2 moved, 1 changed, 1 added)
```

### 4.6 NuGet Dependencies

```xml
<!-- Core Diff Engine -->
<PackageReference Include="DiffPlex" Version="1.9.0" />

<!-- JSON Semantic Diff -->
<PackageReference Include="JsonDiffPatch.Net" Version="2.5.0" />

<!-- XML Semantic Diff -->
<PackageReference Include="AngleSharp.Diffing" Version="1.1.1" />

<!-- Avalonia UI -->
<PackageReference Include="Avalonia" Version="11.0.7" />
<PackageReference Include="AvaloniaEdit" Version="11.0.2" />
<PackageReference Include="Avalonia.Diagnostics" Version="11.0.7" />

<!-- WPF (Windows-only) -->
<PackageReference Include="ModernWpf" Version="0.9.4" />

<!-- TUI -->
<PackageReference Include="Terminal.Gui" Version="2.0.0-alpha" />

<!-- Performance -->
<PackageReference Include="System.Threading.Channels" Version="8.0.0" />
<PackageReference Include="System.Collections.Immutable" Version="8.0.0" />
```

---

## 5. Non-Functional Requirements

### 5.1 Performance Targets

| Metric                       | Target         | Measurement Method                           |
| ---------------------------- | -------------- | -------------------------------------------- |
| Startup time (empty)         | < 500ms        | Stopwatch from process start to window shown |
| File load (10MB)             | < 1s           | Time from file dialog OK to diff displayed   |
| Scroll latency               | < 16ms (60fps) | Time between scroll event and visual update  |
| Diff computation (10MB)      | < 500ms        | Time to compute full diff on 2x10MB files    |
| Memory usage (100MB file)    | < 200MB        | Process.PrivateMemorySize64                  |
| Memory usage (1GB file)      | < 500MB        | With chunked loading and virtualization      |
| Syntax highlight (10K lines) | < 100ms        | Time to highlight visible viewport           |

### 5.2 Scalability

- **File Size:** Support files up to 1GB via chunked loading and memory-mapped files
- **Line Count:** Support up to 10 million lines via virtualization
- **Diff Size:** Handle diffs with 100K+ changes via efficient rendering
- **Concurrent Files:** Support 10+ simultaneous comparisons (memory permitting)

### 5.3 Reliability

- **Crash Rate:** < 0.1% of sessions
- **Data Loss:** Zero risk - read-only comparison, no modification of source files
- **Recovery:** Auto-save comparison state every 30 seconds
- **Error Handling:** Graceful degradation (fallback to simpler algorithms on errors)

### 5.4 Security

- **No Telemetry:** No data sent to external servers
- **File Access:** Respect OS file permissions
- **Memory Safety:** No unsafe code blocks
- **Dependency Scanning:** All dependencies vetted for vulnerabilities

### 5.5 Usability

- **Learning Curve:** < 5 minutes to perform first comparison
- **Keyboard Navigation:** All actions accessible via keyboard
- **Response Time:** < 50ms to all user inputs
- **Accessibility:** WCAG 2.1 AA compliance (screen reader support)

---

## 6. User Requirements

### 6.1 Functional Requirements

#### FR-1: File Selection

**Priority:** P0 (Critical)  
**Description:** Users can select two files to compare via file picker or drag-and-drop.

**Acceptance Criteria:**

- File picker dialog remembers last directory
- Drag-and-drop works for individual files or file pairs
- Error message shown if files are identical
- File extensions auto-detected for format selection

#### FR-2: Side-by-Side Display

**Priority:** P0 (Critical)  
**Description:** Display two files side-by-side with synchronized scrolling.

**Acceptance Criteria:**

- Left and right panes show respective file contents
- Scrolling one pane automatically scrolls the other
- Horizontal scrolling is also synchronized
- Line numbers displayed in both panes
- Pane width can be adjusted via drag handle

#### FR-3: Diff Visualization

**Priority:** P0 (Critical)  
**Description:** Visualize differences with color coding and inline highlights.

**Acceptance Criteria:**

- Added lines: Green background
- Deleted lines: Red background
- Modified lines: Yellow background
- Inline character changes shown within modified lines
- Empty diff shown with "No differences found" message

#### FR-4: Format-Specific Comparison

**Priority:** P1 (High)  
**Description:** Provide semantic comparison for JSON and XML files. **NOT line-by-line comparison.**

**CRITICAL REQUIREMENT:** JSON and XML must be compared structurally, not as text. The comparison must:

1. Parse files into their object/tree models
2. Compare at the semantic level (objects, arrays, elements, attributes)
3. Respect structure and ignore formatting differences
4. Detect reordering intelligently
5. Track changes via JSON Path (JSON) or XPath (XML)

**Acceptance Criteria - JSON:**

- [ ] **Property Order Independence:** Objects compared regardless of property order
  ```
  {"name": "John", "age": 30} == {"age": 30, "name": "John"}
  ```
- [ ] **Array Order Preservation:** Arrays compared with order awareness
  ```
  ["a", "b"] != ["b", "a"]  // Different order detected
  ```
- [ ] **Whitespace Ignored:** Structural whitespace ignored by default
  ```
  {"a":1} == { "a" : 1 }  // Formatting ignored
  ```
- [ ] **Type Change Detection:** Detect value type changes
  ```
  {"count": "5"} → {"count": 5}  // String to number detected
  ```
- [ ] **Deep Nested Comparison:** Compare nested structures recursively
  ```
  {"user": {"profile": {"age": 30}}} compared at each level
  ```
- [ ] **Missing vs Null Detection:** Distinguish between missing and null values
  ```
  {} != {"value": null}  // Different: missing vs explicitly null
  ```
- [ ] **Array Element Tracking:** Track individual array element changes
  ```
  Items[0] changed, Items[1] added, Items[2] removed
  ```
- [ ] **JSON Path Output:** Changes reported in RFC 6902 JSON Path format
  ```
  Changed at: $.users[0].name
  Added at: $.metadata.createdAt
  ```

**Acceptance Criteria - XML:**

- [ ] **Tag Awareness:** Compare as XML nodes, never break tags
  - Diff will NOT produce invalid XML like `<div`text</div>`
  - Tags always remain intact and properly nested
- [ ] **Element Order Preservation:** Element sequence order matters
  ```xml
  <a><b/><c/></a> != <a><c/><b/></a>  // Different order
  ```
- [ ] **Attribute Order Independence:** Attribute order ignored (configurable)
  ```xml
  <tag a="1" b="2"/> == <tag b="2" a="1"/>  // Order ignored
  ```
- [ ] **Attribute Value Comparison:** Detect attribute changes
  ```xml
  <tag id="1"/> → <tag id="2"/>  // Attribute value changed
  ```
- [ ] **Namespace Awareness:** Respect XML namespaces in comparison
  ```xml
  <ns:tag xmlns:ns="http://example.com"/>
  ```
- [ ] **Text Content Comparison:** Compare element text content
  ```xml
  <p>Hello</p> → <p>World</p>  // Content changed
  ```
- [ ] **Mixed Content Support:** Handle text between elements
  ```xml
  <p>Hello <b>world</b></p>  // Preserves text/element interleaving
  ```
- [ ] **XPath Output:** Changes reported in XPath format
  ```
  Changed at: /config/server/@port
  Added at: /config/server/debug
  ```
- [ ] **Comment Handling:** Respect or ignore XML comments (configurable)
- [ ] **CDATA Section Support:** Treat CDATA as opaque content

**Examples:**

**JSON Comparison Example:**

```json
// File A (left)
{
  "name": "John",
  "age": 30,
  "active": true,
  "tags": ["developer", "csharp"],
  "address": {
    "city": "NYC",
    "zip": "10001"
  }
}

// File B (right)
{
  "age": 30,
  "name": "John Doe",        // CHANGED
  "active": true,
  "tags": ["csharp", "developer"],  // REORDERED
  "address": {
    "city": "San Francisco",  // CHANGED
    "zip": "94105",
    "country": "USA"          // ADDED
  }
}

// Diff Output (Semantic, NOT line-by-line):
CHANGED: $.name: "John" → "John Doe"
REORDERED: $.tags[*] (elements at [0] and [1] swapped)
CHANGED: $.address.city: "NYC" → "San Francisco"
CHANGED: $.address.zip: "10001" → "94105"
ADDED: $.address.country: "USA"
```

**XML Comparison Example:**

```xml
<!-- File A (left) -->
<config version="1.0">
  <server host="localhost" port="8080">
    <timeout>30</timeout>
  </server>
  <database>
    <name>appdb</name>
    <poolSize>10</poolSize>
  </database>
</config>

<!-- File B (right) -->
<config version="1.0">
  <database>                           <!-- MOVED UP -->
    <name>appdb</name>
    <poolSize>20</poolSize>            <!-- CHANGED -->
    <schema>public</schema>             <!-- ADDED -->
  </database>
  <server port="8081" host="localhost">  <!-- CHANGED, order swapped -->
    <timeout>30</timeout>
  </server>
</config>

<!-- Diff Output (Semantic, NOT line-by-line):
MOVED: /config/database (moved from position 2 to 1)
CHANGED: /config/database/poolSize: "10" → "20"
ADDED: /config/database/schema: "public"
CHANGED: /config/server/@port: "8080" → "8081"
MOVED: /config/server (moved from position 1 to 2)
-->
```

**Reordering Detection:**

JSON arrays and XML element sequences must show reordering distinctly:

```json
// Array Reordering Example
["item1", "item2", "item3"] → ["item2", "item1", "item3"]

// Diff shows:
REORDERED: $[*] at indices [0,1]
- Previous order: ["item1", "item2", "item3"]
+ New order: ["item2", "item1", "item3"]
```

#### FR-5: Syntax Highlighting

**Priority:** P1 (High)  
**Description:** Provide syntax highlighting for all supported formats.

**Acceptance Criteria:**

- txt: Plain text with consistent font
- md: Markdown-specific highlighting (headers, bold, code blocks)
- json: JSON syntax highlighting (keys, strings, numbers, booleans)
- xml: XML syntax highlighting (tags, attributes, content)
- Highlighting supports dark and light themes

#### FR-6: Navigation

**Priority:** P1 (High)  
**Description:** Enable quick navigation between differences.

**Acceptance Criteria:**

- Keyboard shortcuts: Ctrl+N (next), Ctrl+P (previous)
- Toolbar buttons for next/previous diff
- Overview map shows all diff locations
- Clicking overview map jumps to diff
- Diff count displayed (e.g., "42 changes found")

#### FR-7: Export Diffs

**Priority:** P2 (Medium)  
**Description:** Export diffs in standard formats.

**Acceptance Criteria:**

- Unified diff format (compatible with Git)
- Side-by-side HTML format
- JSON patch format (RFC 6902) for JSON files
- Copy to clipboard option

#### FR-8: Keyboard Shortcuts

**Priority:** P1 (High)  
**Description:** Comprehensive keyboard shortcuts for all actions.

**Acceptance Criteria:**
| Shortcut | Action |
|----------|--------|
| Ctrl+O | Open file comparison dialog |
| Ctrl+S | Save comparison state |
| Ctrl+Q | Quit |
| F5 | Refresh comparison |
| Ctrl+N | Next diff |
| Ctrl+P | Previous diff |
| Ctrl+G | Go to line |
| Ctrl+F | Find in files |
| Ctrl+H | Replace in files |

---

## 7. Implementation Plan

### 7.1 Phases

#### Phase 1: Core Diff Engine (Weeks 1-4)

**Deliverables:**

- DiffPlex integration for line-based comparison (text/Markdown)
- Custom Myers implementation with optimizations
- JSON parser with semantic comparison
- XML parser with DOM tree comparison
- Reordering detection for JSON arrays and XML elements
- JSON Path-based change tracking (RFC 6902)
- XPath-based change tracking for XML
- Caching layer for diff results

**Success Criteria:**

- Unit tests for all diff algorithms
- Unit tests for semantic JSON comparison (15+ test cases covering order independence, type changes, reordering)
- Unit tests for semantic XML comparison (15+ test cases covering tag awareness, attribute handling, reordering)
- Performance benchmarks (target: <500ms for 10MB files)
- Format-aware comparison working for JSON/XML
- Test cases verify: property order ignored, array order preserved, whitespace ignored

**Example Test Cases for Phase 1:**

```
JSON Semantic Tests:
✓ Property order independence
✓ Whitespace independence
✓ Type change detection (string → number)
✓ Nested object comparison
✓ Array element tracking
✓ Array reordering detection
✓ Missing vs null detection
✓ Deep nested structure comparison (10+ levels)
✓ Large arrays (10K+ elements)

XML Semantic Tests:
✓ Tag awareness (never break tags)
✓ Element order preservation
✓ Attribute order independence
✓ Attribute value comparison
✓ Namespace awareness
✓ Text content comparison
✓ Mixed content support
✓ XPath change tracking
✓ Comment handling (ignore/respect)
✓ CDATA section handling
✓ Deep nested element comparison (10+ levels)
```

#### Phase 2: TUI Implementation (Weeks 5-8)

**Deliverables:**

- Terminal.Gui application structure
- Side-by-side view with synchronized scrolling
- Basic diff visualization
- Syntax highlighting (simplified)
- Keyboard navigation

**Success Criteria:**

- Functional TUI application
- 60fps scrolling on 100K line files
- Keyboard shortcuts implemented

#### Phase 3: Avalonia GUI - Basic (Weeks 9-12)

**Deliverables:**

- Avalonia application structure
- Virtual scrolling implementation
- Side-by-side view with sync
- Basic diff visualization
- Overview map component

**Success Criteria:**

- Functional GUI application
- Virtualization working (only rendering visible lines)
- Synchronized scrolling smooth at 60fps

#### Phase 4: Advanced Features (Weeks 13-16)

**Deliverables:**

- Full syntax highlighting
- Inline diff visualization
- Format-specific comparison UI
- Export functionality
- Settings dialog

**Success Criteria:**

- All syntax highlighting accurate
- Export formats working
- User settings persist

#### Phase 5: Performance Optimization (Weeks 17-20)

**Deliverables:**

- Chunked file loading for >10MB files
- Memory-mapped file support
- Object pooling for UI elements
- Double-buffered rendering
- Background diff computation

**Success Criteria:**

- 1GB files load in <2s
- Memory usage <500MB for 1GB files
- Scroll latency <16ms maintained

#### Phase 6: WPF Implementation (Weeks 21-24)

**Deliverables:**

- WPF application structure
- WPF-specific optimizations

**Success Criteria:**

- Feature parity with Avalonia
- WPF-specific performance benchmarks met

#### Phase 7: Polish and Testing (Weeks 25-28)

**Deliverables:**

- Comprehensive test suite
- Performance regression tests
- User documentation
- Installer packages (MSI, AppImage, DMG)
- Release preparation

**Success Criteria:**

- 90%+ code coverage
- All performance targets met
- Documentation complete

### 7.2 Technical Milestones

| Milestone                     | Target Date | Dependencies |
| ----------------------------- | ----------- | ------------ |
| M1: Core diff engine complete | Week 4      | None         |
| M2: TUI MVP ready             | Week 8      | M1           |
| M3: Avalonia MVP ready        | Week 12     | M1           |
| M4: Feature-complete Avalonia | Week 16     | M3           |
| M5: Performance optimized     | Week 20     | M4           |
| M6: WPF MVP ready             | Week 24     | M1           |
| M7: Release candidate         | Week 28     | M5, M6       |

---

## 8. Risk Assessment

### 8.1 Technical Risks

| Risk                                 | Probability | Impact | Mitigation                                 |
| ------------------------------------ | ----------- | ------ | ------------------------------------------ |
| Virtualization performance issues    | Medium      | High   | Early prototyping, benchmark at 100K lines |
| Memory leaks with large files        | Medium      | High   | Memory profiling, weak reference caches    |
| Terminal.Gui breaking changes        | Low         | Medium | Pin specific version, monitor releases     |
| JSON semantic diff complexity        | High        | Medium | Start with simple text diff, iterate       |
| Cross-platform rendering differences | Medium      | Medium | Continuous integration testing             |

### 8.2 Schedule Risks

| Risk                                  | Probability | Impact | Mitigation                                    |
| ------------------------------------- | ----------- | ------ | --------------------------------------------- |
| Avalonia learning curve               | Low         | Medium | Training time allocated                       |
| Performance optimization takes longer | Medium      | High   | Early performance benchmarks, parallel tracks |
| WPF implementation delayed            | Low         | Low    | WPF is nice-to-have, not critical for MVP     |

### 8.3 User Adoption Risks

| Risk                        | Probability | Impact | Mitigation                           |
| --------------------------- | ----------- | ------ | ------------------------------------ |
| Users prefer existing tools | Medium      | Medium | Focus on performance differentiation |
| Poor UX design              | Low         | High   | User testing throughout development  |
| Lack of file format support | Low         | Low    | Extensible parser architecture       |

---

## 9. Success Metrics

### 9.1 Product Metrics

| Metric                   | Target                       | Measurement                |
| ------------------------ | ---------------------------- | -------------------------- |
| Monthly active users     | 1,000 (6 months post-launch) | Application telemetry      |
| Average session duration | 10+ minutes                  | Application telemetry      |
| Comparison success rate  | 99%                          | Error tracking             |
| User satisfaction        | 4.5/5 stars                  | GitHub Stars, user surveys |

### 9.2 Technical Metrics

| Metric            | Target         | Measurement          |
| ----------------- | -------------- | -------------------- |
| Startup time      | < 500ms        | Automated benchmarks |
| Scroll latency    | < 16ms         | Automated benchmarks |
| Crash rate        | < 0.1%         | Crash reporting      |
| Memory efficiency | < 5x file size | Memory profiling     |

---

## 10. Future Enhancements (Post-MVP)

### 10.1 File Formats

- YAML support
- CSV/TSV comparison
- Image comparison
- PDF comparison
- Binary file hex diff

### 10.2 Integration

- Git integration (staging, commit from diff view)
- VS Code extension
- Command-line interface for CI/CD
- API for programmatic access

### 10.3 Collaboration

- Real-time collaborative diffing
- Diff sharing via URLs
- Comment threads on diffs
- Review workflow integration

### 10.4 Advanced Features

- Fuzzy matching (ignore minor changes)
- Ignore patterns (regex-based)
- Diff history tracking
- Machine learning-assisted diff prioritization
- Custom themes and color schemes

---

## 11. Appendix A: Glossary

- **Diff Algorithm:** Algorithm for computing the differences between two sequences
- **Hunk:** A contiguous block of changes in a diff
- **Myers Algorithm:** Graph-based diff algorithm producing optimal edit scripts
- **Patience Diff:** Diff algorithm optimized for human-readable output
- **Virtual Scrolling:** Rendering only visible content for performance
- **Synchronized Scrolling:** Coordinated scrolling between multiple views
- **Semantic Diff:** Comparison that understands structure (e.g., JSON objects vs text)
  - Compares parsed objects/trees, not raw text
  - Ignores formatting, respects structure
  - Detects changes at semantic level (properties, elements, attributes)
- **JSON Path:** String syntax for identifying specific values in JSON documents (RFC 6902)
  - Example: `$.users[0].name` selects "name" property of first user
- **XPath:** XML Path Language for selecting nodes in XML documents
  - Example: `/config/server/@port` selects port attribute of server element
- **RFC 6902:** JSON Patch format standard for describing changes to JSON documents
- **Unified Diff:** Standard diff format used by Git and other version control systems
- **Tree-Based Diff:** Comparison of hierarchical structures (XML DOM, JSON object tree)
  - Uses tree-edit distance algorithms
  - Tracks insertions, deletions, and modifications of nodes
- **Order-Independent Comparison:** Comparison where sequence order is ignored
  - JSON object properties: Order doesn't matter (per JSON spec)
  - XML attributes: Order doesn't matter (configurable)
  - JSON arrays: Order matters (arrays are ordered sequences)
  - XML elements: Order matters (element sequence is significant)
- **Line-by-Line Diff:** Text-based comparison comparing individual text lines
  - Comparo uses this ONLY for plain text and Markdown
  - Comparo uses semantic comparison for JSON and XML

---

## 12. Appendix B: References

### Research Sources

1. **Terminal.Gui Documentation:** https://gui-cs.github.io/Terminal.Gui
2. **Avalonia Documentation:** https://docs.avaloniaui.net
3. **DiffPlex GitHub:** https://github.com/mmanela/diffplex
4. **JsonDiffPatch.NET GitHub:** https://github.com/wbish/jsondiffpatch.net
5. **WinMerge Source Code:** https://github.com/WinMerge/winmerge
6. **KDiff3 Source Code:** https://github.com/KDE/kdiff3
7. **Delta (Rust diff tool):** https://github.com/dandavison/delta
8. **Myers Diff Algorithm Paper:** Myers, E. (1986). "An O(ND) Difference Algorithm and Its Variations"

### Performance Benchmarks

- WinMerge: ~2-3s to diff 10MB files
- Meld: ~5s to diff 10MB files
- Beyond Compare: ~1s to diff 10MB files (commercial)
- **Comparo Target:** <500ms to diff 10MB files

---

**Document Status:** Draft  
**Last Updated:** 2026-02-02  
**Next Review:** After Phase 1 completion (Week 4)
