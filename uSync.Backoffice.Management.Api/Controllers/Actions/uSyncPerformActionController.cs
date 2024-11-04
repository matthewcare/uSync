using Asp.Versioning;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

using System.Net.Mime;
using System.Text;

using uSync.Backoffice.Management.Api.Models;
using uSync.Backoffice.Management.Api.Services;

namespace uSync.Backoffice.Management.Api.Controllers.Actions;

[ApiVersion("1.0")]
[ApiExplorerSettings(GroupName = "Actions")]
public class uSyncPerformActionController : uSyncControllerBase
{
    private readonly ISyncManagementService _managementService;

    public uSyncPerformActionController(ISyncManagementService managementService)
    {
        _managementService = managementService;
    }

    [HttpPost("Perform")]
    [ProducesResponseType(typeof(PerformActionResponse), 200)]
    public async Task<PerformActionResponse> PerformAction(PerformActionRequest model)
        => await _managementService.PerformActionAsync(model);


    [HttpPost("Download")]
    [ProducesResponseType<FileContentResult>(StatusCodes.Status200OK)]
    public ActionResult Download(string requestId)
    {
        var filename = "uSync.zip";

        var stream = _managementService.CompressExportFolder();
        if (stream is null) return NotFound();

        var mediaType = new MediaTypeHeaderValue(MediaTypeNames.Application.Zip)
        {
            Charset = Encoding.UTF8.WebName
        };

        var contentDisposition = new ContentDisposition
        {
            FileName = filename,
            DispositionType = DispositionTypeNames.Attachment
        };

        Response.Headers[HeaderNames.ContentDisposition] = contentDisposition.ToString();

        return new FileStreamResult(stream, mediaType)
        {
            FileDownloadName = filename
        };
    }
}
