using Microsoft.AspNetCore.Mvc;
using OAuth2.Services;

namespace OAuth2.Controllers;

public abstract class BackendProxyControllerBase : ControllerBase
{
    protected IActionResult FromBackend(BackendResponse response)
    {
        if (string.IsNullOrEmpty(response.Content))
        {
            return StatusCode((int)response.StatusCode);
        }

        return new ContentResult
        {
            Content = response.Content,
            ContentType = response.ContentType ?? "application/json; charset=utf-8",
            StatusCode = (int)response.StatusCode
        };
    }
}
