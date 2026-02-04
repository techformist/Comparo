using Comparo.Core.DiffModels;

namespace Comparo.Core.DiffAlgorithms;

public interface IDiffAlgorithm
{
    DiffHunk ComputeDiff(string[] oldLines, string[] newLines);
    SideBySideModel ComputeSideBySideDiff(string[] oldLines, string[] newLines);
}
