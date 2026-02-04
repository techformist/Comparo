using Comparo.Core.DiffModels;

namespace Comparo.Core.DiffAlgorithms;

/// <summary>
/// Interface for diff algorithms that can operate on large files using streaming/chunked loading
/// to minimize memory footprint.
/// </summary>
public interface IStreamingDiffAlgorithm
{
  /// <summary>
  /// Compute diff using chunked loaders for efficient memory usage on large files.
  /// </summary>
  /// <param name="oldLoader">Loader for the old/left file</param>
  /// <param name="newLoader">Loader for the new/right file</param>
  /// <param name="cancellationToken">Cancellation token</param>
  /// <returns>The computed diff hunk</returns>
  Task<DiffHunk> ComputeDiffAsync(
      ChunkedTextLoader oldLoader,
      ChunkedTextLoader newLoader,
      CancellationToken cancellationToken = default);

  /// <summary>
  /// Compute side-by-side diff using chunked loaders.
  /// </summary>
  /// <param name="oldLoader">Loader for the old/left file</param>
  /// <param name="newLoader">Loader for the new/right file</param>
  /// <param name="cancellationToken">Cancellation token</param>
  /// <returns>The computed side-by-side model</returns>
  Task<SideBySideModel> ComputeSideBySideDiffAsync(
      ChunkedTextLoader oldLoader,
      ChunkedTextLoader newLoader,
      CancellationToken cancellationToken = default);
}
