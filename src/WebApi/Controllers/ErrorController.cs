using Microsoft.AspNetCore.Mvc;

namespace Daemos.WebApi.Controllers
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
