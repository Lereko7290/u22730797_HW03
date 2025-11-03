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
            int pageSize = 10; // Items per page

            var viewModel = new MaintainViewModel
            {
                StaffPage = staffPage,
                CustomerPage = customerPage,
                ProductPage = productPage,
                PageSize = pageSize
            };

            // Staff pagination - order by newest first
            var staffCount = await db.staffs.CountAsync();
            viewModel.StaffTotalPages = (int)Math.Ceiling(staffCount / (double)pageSize);

            viewModel.Staffs = await db.staffs
                .OrderByDescending(s => s.staff_id)  // Newest first
                .Skip((staffPage - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Customer pagination - order by newest first
            var customerCount = await db.customers.CountAsync();
            viewModel.CustomerTotalPages = (int)Math.Ceiling(customerCount / (double)pageSize);

            viewModel.Customers = await db.customers
                .OrderByDescending(c => c.customer_id)  // Newest first
                .Skip((customerPage - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Product pagination - order by newest first
            var productCount = await db.products.CountAsync();
            viewModel.ProductTotalPages = (int)Math.Ceiling(productCount / (double)pageSize);

            viewModel.Products = await db.products
                .Include(p => p.Brand)
                .Include(p => p.Category)
                .OrderByDescending(p => p.product_id)  // Newest first
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
            return PartialView("_EditStaffModal", staff);
        }

        [HttpPost]
        public async Task<ActionResult> EditStaff(Staff staff)
        {
            try
            {
                // Get the existing staff record to preserve any required fields that might not be in the form
                var existingStaff = await db.staffs.FindAsync(staff.staff_id);
                if (existingStaff == null)
                {
                    return Json(new { success = false, errors = "Staff not found" });
                }

                // Update only the fields that are in the form
                existingStaff.first_name = staff.first_name;
                existingStaff.last_name = staff.last_name;
                existingStaff.email = staff.email;
                existingStaff.phone = staff.phone;
                existingStaff.active = staff.active;

                if (ModelState.IsValid)
                {
                    await db.SaveChangesAsync();
                    return Json(new { success = true });
                }
                else
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                    return Json(new { success = false, errors = string.Join(", ", errors) });
                }
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Staff update error: {ex.Message}");
                return Json(new { success = false, errors = ex.Message });
            }
        }

        [HttpPost]
        public async Task<ActionResult> DeleteStaff(int id)
        {
            var staff = await db.staffs.FindAsync(id);
            if (staff != null)
            {
                db.staffs.Remove(staff);
                await db.SaveChangesAsync();
            }
            return Json(new { success = true });
        }

        // Customer CRUD
        public async Task<ActionResult> EditCustomer(int id)
        {
            var customer = await db.customers.FindAsync(id);
            if (customer == null)
            {
                return HttpNotFound();
            }
            return PartialView("_EditCustomerModal", customer);
        }

        [HttpPost]
        public async Task<ActionResult> EditCustomer(Customer customer)
        {
            if (ModelState.IsValid)
            {
                db.Entry(customer).State = EntityState.Modified;
                await db.SaveChangesAsync();
                return Json(new { success = true });
            }
            return PartialView("_EditCustomerModal", customer);
        }

        [HttpPost]
        public async Task<ActionResult> DeleteCustomer(int id)
        {
            var customer = await db.customers.FindAsync(id);
            if (customer != null)
            {
                db.customers.Remove(customer);
                await db.SaveChangesAsync();
            }
            return Json(new { success = true });
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
            return PartialView("_EditProductModal", product);
        }

        [HttpPost]
        public async Task<ActionResult> EditProduct(FormCollection form)
        {
            try
            {
                // Parse product ID
                if (!int.TryParse(form["product_id"], out int productId))
                {
                    return Json(new { success = false, errors = "Invalid product ID" });
                }

                var product = await db.products.FindAsync(productId);
                if (product == null)
                {
                    return Json(new { success = false, errors = "Product not found" });
                }

                // Update product name
                product.product_name = form["product_name"];

                // Parse brand_id with validation
                if (!int.TryParse(form["brand_id"], out int brandId))
                {
                    return Json(new { success = false, errors = "Please select a valid brand" });
                }
                product.brand_id = brandId;

                // Parse category_id with validation
                if (!int.TryParse(form["category_id"], out int categoryId))
                {
                    return Json(new { success = false, errors = "Please select a valid category" });
                }
                product.category_id = categoryId;

                // Parse list_price - handle both comma and dot decimal separators
                string priceString = form["list_price"];
                if (string.IsNullOrEmpty(priceString))
                {
                    return Json(new { success = false, errors = "Please enter a valid price" });
                }

                // Replace comma with dot for proper decimal parsing
                priceString = priceString.Replace(',', '.');

                if (!decimal.TryParse(priceString, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal listPrice))
                {
                    return Json(new { success = false, errors = "Please enter a valid price format (e.g., 749,99 or 749.99)" });
                }
                product.list_price = listPrice;

                // Parse model_year with validation
                if (!short.TryParse(form["model_year"], out short modelYear))
                {
                    return Json(new { success = false, errors = "Please enter a valid model year" });
                }
                product.model_year = modelYear;

                // Validate the model
                if (TryValidateModel(product))
                {
                    await db.SaveChangesAsync();
                    return Json(new { success = true, message = "Product updated successfully" });
                }
                else
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                    return Json(new { success = false, errors = string.Join(", ", errors) });
                }
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Product update error: {ex.Message}");
                return Json(new { success = false, errors = $"Database error: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<ActionResult> DeleteProduct(int id)
        {
            var product = await db.products.FindAsync(id);
            if (product != null)
            {
                db.products.Remove(product);
                await db.SaveChangesAsync();
            }
            return Json(new { success = true });
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