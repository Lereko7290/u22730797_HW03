using System.Data.Entity;
using System.Threading.Tasks;
using System.Web.Mvc;
using u22730797_HW03.Models;

namespace u22730797_HW03.Controllers
{
    public class StaffController : Controller
    {
        private BikeStoresContext db = new BikeStoresContext();

        public async Task<ActionResult> Index()
        {
            var staffs = await db.staffs.ToListAsync();
            return View(staffs);
        }

        public async Task<ActionResult> Edit(int id)
        {
            var staff = await db.staffs.FindAsync(id);
            if (staff == null)
            {
                return HttpNotFound();
            }
            return View("_EditStaffForm", staff);
        }

        [HttpPost]
        public async Task<ActionResult> Edit(Staff staff)
        {
            if (ModelState.IsValid)
            {
                db.Entry(staff).State = EntityState.Modified;
                await db.SaveChangesAsync();
                return Json(new { success = true });
            }
            return View("_EditStaffForm", staff);
        }

        public async Task<ActionResult> Delete(int id)
        {
            var staff = await db.staffs.FindAsync(id);
            if (staff == null)
            {
                return HttpNotFound();
            }
            return PartialView("_DeleteStaffModal", staff);
        }

        [HttpPost, ActionName("Delete")]
        public async Task<ActionResult> DeleteConfirmed(int id)
        {
            var staff = await db.staffs.FindAsync(id);
            db.staffs.Remove(staff);
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