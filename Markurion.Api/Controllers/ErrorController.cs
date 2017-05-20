using Microsoft.AspNetCore.Mvc;

namespace Markurion.WebApi.Controllers
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
