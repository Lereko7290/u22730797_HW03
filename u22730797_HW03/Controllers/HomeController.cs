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
            int pageSize = 5; // Items per page

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

            // Products (keep as is, or add pagination if needed)
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
        [HttpPost]
        public async Task<ActionResult> CreateStaff(FormCollection form)
        {
            try
            {
                var staff = new Staff
                {
                    first_name = form["first_name"],
                    last_name = form["last_name"],
                    email = form["email"],
                    phone = form["phone"] ?? "",
                    active = 1,
                    store_id = 1,
                    manager_id = null
                };

                // Validate required fields
                if (string.IsNullOrEmpty(staff.first_name))
                {
                    return Json(new { success = false, errors = "First name is required" });
                }

                if (string.IsNullOrEmpty(staff.last_name))
                {
                    return Json(new { success = false, errors = "Last name is required" });
                }

                if (string.IsNullOrEmpty(staff.email))
                {
                    return Json(new { success = false, errors = "Email is required" });
                }

                db.staffs.Add(staff);
                await db.SaveChangesAsync();

                return Json(new { success = true, message = "Staff created successfully", redirectToPage = true });
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating staff: {ex.Message}");
                return Json(new { success = false, errors = $"Database error: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<ActionResult> CreateCustomer(FormCollection form)
        {
            try
            {
                var customer = new Customer
                {
                    first_name = form["first_name"],
                    last_name = form["last_name"],
                    email = form["email"] ?? "",
                    phone = form["phone"] ?? "",
                    city = form["city"] ?? "",
                    state = form["state"] ?? "",
                    street = "",
                    zip_code = ""
                };

                // Validate required fields
                if (string.IsNullOrEmpty(customer.first_name))
                {
                    return Json(new { success = false, errors = "First name is required" });
                }

                if (string.IsNullOrEmpty(customer.last_name))
                {
                    return Json(new { success = false, errors = "Last name is required" });
                }

                db.customers.Add(customer);
                await db.SaveChangesAsync();

                return Json(new { success = true, message = "Customer created successfully", redirectToPage = true });
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating customer: {ex.Message}");
                return Json(new { success = false, errors = $"Database error: {ex.Message}" });
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
            return PartialView("_ProductList", filteredProducts);
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