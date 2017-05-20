using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace Transact.Api.Controllers
{
    public class ErrorController : ApiController
    {
        [HttpGet]
        public IHttpActionResult Get()
        {
            return NotFound();
        }
    }
}
