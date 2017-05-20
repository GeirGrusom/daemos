using Microsoft.AspNetCore.Mvc;

namespace Transact.Api.Controllers
{
    public class ErrorController : Controller
    {
        [HttpGet]
        public IActionResult Get()
        {
            return NotFound();
        }
    }
}
