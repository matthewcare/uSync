using System.Collections.Generic;

namespace uSync.BackOffice.Models;

/// <summary>
///  results of a file upload and validate
/// </summary>
public class UploadImportResult
{
    public required bool Success { get; set; }
    public IEnumerable<string> Errors { get; set; } = [];
}
