# Comparo Setup Guide

## Quick Start

```bash
# Clone the repository
git clone https://github.com/techformist/Comparo.git
cd Comparo

# Initialize and update submodules (test data)
git submodule update --init --recursive

# Restore NuGet packages
dotnet restore

# Build the solution
dotnet build

# Run tests
dotnet test
```

## Test Data Submodule

This project uses a separate repository for test data to keep the main codebase lean. The test data is managed as a git submodule.

### Initial Setup

When cloning the repository for the first time, the TestData folder will be empty. Initialize it with:

```bash
git submodule update --init --recursive
```

### Updating Test Data

To pull the latest test data:

```bash
git submodule update --remote Comparo.Tests/TestData
```

### Working with Test Data

The test data repository is located at: https://github.com/techformist/Comparo.TestData

If you need to modify test data:

1. Navigate to the submodule directory:

   ```bash
   cd Comparo.Tests/TestData
   ```

2. Create a branch and make your changes:

   ```bash
   git checkout -b my-test-data-changes
   # Make your changes
   git add .
   git commit -m "Add new test scenario"
   git push origin my-test-data-changes
   ```

3. Create a pull request in the TestData repository

4. After merging, update the main Comparo repository to reference the new commit:
   ```bash
   cd ../..  # Back to Comparo root
   git add Comparo.Tests/TestData
   git commit -m "Update test data submodule"
   git push
   ```

## CI/CD Configuration

### GitHub Actions

Ensure your workflow includes submodule initialization:

```yaml
- name: Checkout code
  uses: actions/checkout@v3
  with:
    submodules: recursive
```

### Azure DevOps

Add this to your pipeline YAML:

```yaml
- checkout: self
  submodules: true
```

## Troubleshooting

### TestData folder is empty

Run: `git submodule update --init --recursive`

### Submodule shows as modified

This means the TestData commit doesn't match what the main repo expects. Either:

- Update to the correct version: `git submodule update`
- Or commit the new version: `git add Comparo.Tests/TestData && git commit`

### Permission issues with TestData repo

Ensure you have access to https://github.com/techformist/Comparo.TestData
