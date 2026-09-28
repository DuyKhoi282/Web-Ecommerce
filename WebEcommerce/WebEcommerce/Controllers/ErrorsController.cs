using System.Web.Mvc;

namespace WebEcommerce.Controllers
{
    public class ErrorsController : Controller
    {
        // GET: Errors/Forbidden (HTTP 403)
        [AllowAnonymous]
        public ActionResult Forbidden()
        {
            Response.StatusCode = 403;
            Response.TrySkipIisCustomErrors = true;
            return View();
        }

        // GET: Errors/NotFound (HTTP 404)
        [AllowAnonymous]
        public ActionResult NotFound()
        {
            Response.StatusCode = 404;
            Response.TrySkipIisCustomErrors = true;
            return View();
        }
    }
}
