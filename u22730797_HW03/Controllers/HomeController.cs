using System.Data.Entity;
using System.Threading.Tasks;
using System.Web.Mvc;
using u22730797_HW03.Models;
using System.Linq;
using System;

namespace u22730797_HW03.Controllers
{
    public class HomeController : Controller
    {
        private BikeStoresContext db = new BikeStoresContext();

        public async Task<ActionResult> Index(int staffPage = 1, int customerPage = 1)
        {
            int pageSize = 5;

            var viewModel = new HomeViewModel
            {
                StaffPage = staffPage,
                CustomerPage = customerPage,
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

            // Products
            viewModel.Products = await db.products
                .Include(p => p.Brand)
                .Include(p => p.Category)
                .Take(5)
                .ToListAsync();

            viewModel.Brands = await db.brands.ToListAsync();
            viewModel.Categories = await db.categories.ToListAsync();

            ViewBag.StaffCount = staffCount;
            ViewBag.CustomerCount = customerCount;
            ViewBag.ProductCount = await db.products.CountAsync();

            return View(viewModel);
        }

        public ActionResult CreateStaff()
        {
            return View(); 
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CreateStaff(Staff staff)
        {
            try
            {
                // Validate required fields
                if (string.IsNullOrEmpty(staff.first_name))
                {
                    ModelState.AddModelError("first_name", "First name is required");
                }

                if (string.IsNullOrEmpty(staff.last_name))
                {
                    ModelState.AddModelError("last_name", "Last name is required");
                }

                if (string.IsNullOrEmpty(staff.email))
                {
                    ModelState.AddModelError("email", "Email is required");
                }

                if (ModelState.IsValid)
                {
                    staff.active = 1;
                    staff.store_id = 1;
                    staff.manager_id = null;

                    db.staffs.Add(staff);
                    await db.SaveChangesAsync();
                    return RedirectToAction("Index"); 
                }

                return View(staff);
            }
            catch (System.Exception ex)
            {
                ModelState.AddModelError("", $"Error creating staff: {ex.Message}");
                return View(staff);
            }
        }

        public ActionResult CreateCustomer()
        {
            return View(); 
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CreateCustomer(Customer customer)
        {
            try
            {
                // Validate required fields
                if (string.IsNullOrEmpty(customer.first_name))
                {
                    ModelState.AddModelError("first_name", "First name is required");
                }

                if (string.IsNullOrEmpty(customer.last_name))
                {
                    ModelState.AddModelError("last_name", "Last name is required");
                }

                if (ModelState.IsValid)
                {
                    customer.street = "";
                    customer.zip_code = "";

                    db.customers.Add(customer);
                    await db.SaveChangesAsync();
                    return RedirectToAction("Index"); 
                }

                return View(customer);
            }
            catch (System.Exception ex)
            {
                ModelState.AddModelError("", $"Error creating customer: {ex.Message}");
                return View(customer);
            }
        }

        public async Task<ActionResult> FilterProducts(int? brandId, int? categoryId)
        {
            var products = db.products.Include(p => p.Brand).Include(p => p.Category).AsQueryable();

            if (brandId.HasValue)
                products = products.Where(p => p.brand_id == brandId.Value);

            if (categoryId.HasValue)
                products = products.Where(p => p.category_id == categoryId.Value);

            var filteredProducts = await products.Take(10).ToListAsync();

            
            return View("ProductList", filteredProducts);
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