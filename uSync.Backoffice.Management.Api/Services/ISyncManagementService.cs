using uSync.Backoffice.Management.Api.Models;
using uSync.BackOffice;
using uSync.BackOffice.Models;
using uSync.BackOffice.SyncHandlers.Models;

namespace uSync.Backoffice.Management.Api.Services;
public interface ISyncManagementService
{
    Stream CompressExportFolder();
    List<SyncActionGroup> GetActions();
    Func<SyncActionOptions, uSyncCallbacks, Task<SyncActionResult>> GetHandlerMethodAsync(HandlerActions action);
    Task<PerformActionResponse> PerformActionAsync(PerformActionRequest actionRequest);
    UploadImportResult UnpackStream(Stream stream);
}