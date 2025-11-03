using System.Data.Entity;
using System.Threading.Tasks;
using System.Web.Mvc;
using u22730797_HW03.Models;

namespace u22730797_HW03.Controllers
{
    public class CustomerController : Controller
    {
        private BikeStoresContext db = new BikeStoresContext();

        public async Task<ActionResult> Index()
        {
            var customers = await db.customers.ToListAsync();
            return View(customers);
        }

        public async Task<ActionResult> Edit(int id)
        {
            var customer = await db.customers.FindAsync(id);
            if (customer == null)
            {
                return HttpNotFound();
            }
            return PartialView("_EditCustomerForm", customer);
        }

        [HttpPost]
        public async Task<ActionResult> Edit(Customer customer)
        {
            if (ModelState.IsValid)
            {
                db.Entry(customer).State = EntityState.Modified;
                await db.SaveChangesAsync();
                return Json(new { success = true });
            }
            return PartialView("_EditCustomerForm", customer);
        }

        public async Task<ActionResult> Delete(int id)
        {
            var customer = await db.customers.FindAsync(id);
            if (customer == null)
            {
                return HttpNotFound();
            }
            return PartialView("_DeleteCustomerModal", customer);
        }

        [HttpPost, ActionName("Delete")]
        public async Task<ActionResult> DeleteConfirmed(int id)
        {
            var customer = await db.customers.FindAsync(id);
            db.customers.Remove(customer);
            await db.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}