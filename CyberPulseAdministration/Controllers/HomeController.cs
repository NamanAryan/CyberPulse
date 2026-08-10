using System.Web.Mvc;

namespace CyberPulseAdministration.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        // GET: Home/Index
        public ActionResult Index()
        {
            return View();
        }
    }
}