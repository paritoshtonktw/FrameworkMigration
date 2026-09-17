using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CryptoTrading.Core.Controllers;

[Route("")]
[AllowAnonymous]
[ApiExplorerSettings(IgnoreApi = true)] // Hides the redirect route from Swagger documentation
public class SwaggerRedirectController : ControllerBase
{
    [HttpGet]
    public IActionResult Index()
    {
        return RedirectPermanent("~/swagger");
    }
}
