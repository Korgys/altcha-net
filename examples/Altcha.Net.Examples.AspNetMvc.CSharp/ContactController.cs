using System.Web.Mvc;

namespace Altcha.Net.Examples.AspNetMvc.CSharp
{
    public sealed class ContactController : Controller
    {
        [HttpGet]
        public ActionResult Index()
        {
            return View(new ContactForm());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Submit(ContactForm model)
        {
            var result = AltchaProvider.Service.ValidateResponse(Request.Form["altcha"]);
            if (!result.IsValid)
            {
                ModelState.AddModelError("", "Validation ALTCHA invalide.");
                return View("Index", model);
            }

            return RedirectToAction("Thanks");
        }

        [HttpGet]
        public ActionResult Thanks()
        {
            return Content("Message envoye.");
        }
    }
}
