using System.Data.Entity;
using System.Threading.Tasks;
using System.Web.Mvc;
using u22730797_HW03.Models;
using System.Linq;
using System;

namespace u22730797_HW03.Controllers
{
    public class MaintainController : Controller
    {
        private BikeStoresContext db = new BikeStoresContext();

        public async Task<ActionResult> Index(int staffPage = 1, int customerPage = 1, int productPage = 1)
        {
            int pageSize = 10;

            var viewModel = new MaintainViewModel
            {
                StaffPage = staffPage,
                CustomerPage = customerPage,
                ProductPage = productPage,
                PageSize = pageSize
            };

            // Staff pagination
            var staffCount = await db.staffs.CountAsync();
            viewModel.StaffTotalPages = (int)Math.Ceiling(staffCount / (double)pageSize);

            viewModel.Staffs = await db.staffs
                .OrderByDescending(s => s.staff_id)
                .Skip((staffPage - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Customer pagination
            var customerCount = await db.customers.CountAsync();
            viewModel.CustomerTotalPages = (int)Math.Ceiling(customerCount / (double)pageSize);

            viewModel.Customers = await db.customers
                .OrderByDescending(c => c.customer_id)
                .Skip((customerPage - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Product pagination
            var productCount = await db.products.CountAsync();
            viewModel.ProductTotalPages = (int)Math.Ceiling(productCount / (double)pageSize);

            viewModel.Products = await db.products
                .Include(p => p.Brand)
                .Include(p => p.Category)
                .OrderByDescending(p => p.product_id)
                .Skip((productPage - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return View(viewModel);
        }

        // Staff CRUD
        public async Task<ActionResult> EditStaff(int id)
        {
            var staff = await db.staffs.FindAsync(id);
            if (staff == null)
            {
                return HttpNotFound();
            }
            return View("EditStaff", staff); // Changed to full view
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> EditStaff(Staff staff)
        {
            try
            {
                var existingStaff = await db.staffs.FindAsync(staff.staff_id);
                if (existingStaff == null)
                {
                    ModelState.AddModelError("", "Staff not found");
                    return View("EditStaff", staff);
                }

                existingStaff.first_name = staff.first_name;
                existingStaff.last_name = staff.last_name;
                existingStaff.email = staff.email;
                existingStaff.phone = staff.phone;
                existingStaff.active = staff.active;

                if (ModelState.IsValid)
                {
                    await db.SaveChangesAsync();
                    return RedirectToAction("Index"); // Changed to redirect
                }
                else
                {
                    return View("EditStaff", staff);
                }
            }
            catch (System.Exception ex)
            {
                ModelState.AddModelError("", $"Error updating staff: {ex.Message}");
                return View("EditStaff", staff);
            }
        }

        public async Task<ActionResult> DeleteStaff(int id)
        {
            var staff = await db.staffs.FindAsync(id);
            if (staff == null)
            {
                return HttpNotFound();
            }
            return View("DeleteStaff", staff); // Changed to full view
        }

        [HttpPost, ActionName("DeleteStaff")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteStaffConfirmed(int id)
        {
            var staff = await db.staffs.FindAsync(id);
            if (staff != null)
            {
                db.staffs.Remove(staff);
                await db.SaveChangesAsync();
            }
            return RedirectToAction("Index"); // Changed to redirect
        }

        // Customer CRUD
        public async Task<ActionResult> EditCustomer(int id)
        {
            var customer = await db.customers.FindAsync(id);
            if (customer == null)
            {
                return HttpNotFound();
            }
            return View("EditCustomer", customer); // Changed to full view
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> EditCustomer(Customer customer)
        {
            if (ModelState.IsValid)
            {
                db.Entry(customer).State = EntityState.Modified;
                await db.SaveChangesAsync();
                return RedirectToAction("Index"); // Changed to redirect
            }
            return View("EditCustomer", customer); // Changed to full view
        }

        public async Task<ActionResult> DeleteCustomer(int id)
        {
            var customer = await db.customers.FindAsync(id);
            if (customer == null)
            {
                return HttpNotFound();
            }
            return View("DeleteCustomer", customer); // Changed to full view
        }

        [HttpPost, ActionName("DeleteCustomer")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteCustomerConfirmed(int id)
        {
            var customer = await db.customers.FindAsync(id);
            if (customer != null)
            {
                db.customers.Remove(customer);
                await db.SaveChangesAsync();
            }
            return RedirectToAction("Index"); // Changed to redirect
        }

        // Product CRUD
        public async Task<ActionResult> EditProduct(int id)
        {
            var product = await db.products.FindAsync(id);
            if (product == null)
            {
                return HttpNotFound();
            }
            ViewBag.Brands = await db.brands.ToListAsync();
            ViewBag.Categories = await db.categories.ToListAsync();
            return View("EditProduct", product); // Changed to full view
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> EditProduct(Product product)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    db.Entry(product).State = EntityState.Modified;
                    await db.SaveChangesAsync();
                    return RedirectToAction("Index"); // Changed to redirect
                }

                ViewBag.Brands = await db.brands.ToListAsync();
                ViewBag.Categories = await db.categories.ToListAsync();
                return View("EditProduct", product);
            }
            catch (System.Exception ex)
            {
                ModelState.AddModelError("", $"Error updating product: {ex.Message}");
                ViewBag.Brands = await db.brands.ToListAsync();
                ViewBag.Categories = await db.categories.ToListAsync();
                return View("EditProduct", product);
            }
        }

        public async Task<ActionResult> DeleteProduct(int id)
        {
            var product = await db.products.FindAsync(id);
            if (product == null)
            {
                return HttpNotFound();
            }
            return View("DeleteProduct", product); // Changed to full view
        }

        [HttpPost, ActionName("DeleteProduct")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteProductConfirmed(int id)
        {
            var product = await db.products.FindAsync(id);
            if (product != null)
            {
                db.products.Remove(product);
                await db.SaveChangesAsync();
            }
            return RedirectToAction("Index"); // Changed to redirect
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