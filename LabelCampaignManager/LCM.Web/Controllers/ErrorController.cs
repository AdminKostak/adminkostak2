using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LCM.Web.Controllers;

[AllowAnonymous]
public class ErrorController : Controller
{
    [Route("Home/Hata/{kod?}")]
    public IActionResult Hata(int? kod)
    {
        if (kod == 404)
            return View("~/Views/Shared/404.cshtml");

        return View("~/Views/Shared/Error.cshtml");
    }
}