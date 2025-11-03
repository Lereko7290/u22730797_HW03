using System.Data.Entity;
using System.Threading.Tasks;
using System.Web.Mvc;
using u22730797_HW03.Models;

namespace u22730797_HW03.Controllers
{
    public class ProductController : Controller
    {
        private BikeStoresContext db = new BikeStoresContext();

        public async Task<ActionResult> Index()
        {
            var products = await db.products
                .Include(p => p.Brand)
                .Include(p => p.Category)
                .ToListAsync();
            return View(products);
        }

        public async Task<ActionResult> Edit(int id)
        {
            var product = await db.products.FindAsync(id);
            if (product == null)
            {
                return HttpNotFound();
            }
            ViewBag.Brands = await db.brands.ToListAsync();
            ViewBag.Categories = await db.categories.ToListAsync();
            return PartialView("_EditProductForm", product);
        }

        [HttpPost]
        public async Task<ActionResult> Edit(Product product)
        {
            if (ModelState.IsValid)
            {
                db.Entry(product).State = EntityState.Modified;
                await db.SaveChangesAsync();
                return Json(new { success = true });
            }
            ViewBag.Brands = await db.brands.ToListAsync();
            ViewBag.Categories = await db.categories.ToListAsync();
            return PartialView("_EditProductForm", product);
        }

        public async Task<ActionResult> Delete(int id)
        {
            var product = await db.products.FindAsync(id);
            if (product == null)
            {
                return HttpNotFound();
            }
            return PartialView("_DeleteProductModal", product);
        }

        [HttpPost, ActionName("Delete")]
        public async Task<ActionResult> DeleteConfirmed(int id)
        {
            var product = await db.products.FindAsync(id);
            db.products.Remove(product);
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