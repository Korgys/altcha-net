using System.Web.Mvc;

namespace Altcha.Net.Examples.AspNetMvc.CSharp
{
    public sealed class AltchaController : Controller
    {
        [HttpGet]
        public ActionResult Challenge()
        {
            return Content(AltchaProvider.Service.GenerateChallenge().ToJson(), "application/json");
        }
    }
}
