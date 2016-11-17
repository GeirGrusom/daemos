using Microsoft.AspNetCore.Mvc;

namespace Markurion.Api.Controllers
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
