using System.Data.Entity;
using System.Threading.Tasks;
using System.Web.Mvc;
using u22730797_HW03.Models;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System;
using iTextSharp.text;
using iTextSharp.text.pdf;

namespace u22730797_HW03.Controllers
{
    public class ReportController : Controller
    {
        private BikeStoresContext db = new BikeStoresContext();

        public ActionResult Index()
        {
            return View();
        }

        // Current Sales Report
        public async Task<ActionResult> SalesReport()
        {
            var salesData = await db.orders
                .Join(db.order_items, o => o.order_id, oi => oi.order_id, (o, oi) => new { o, oi })
                .Join(db.products, x => x.oi.product_id, p => p.product_id, (x, p) => new { x.o, x.oi, p })
                .Join(db.customers, x => x.o.customer_id, c => c.customer_id, (x, c) => new { x.o, x.oi, x.p, c })
                .Join(db.staffs, x => x.o.staff_id, s => s.staff_id, (x, s) => new
                {
                    ProductName = x.p.product_name,
                    CustomerName = x.c.first_name + " " + x.c.last_name,
                    StaffName = s.first_name + " " + s.last_name,
                    Quantity = x.oi.quantity,
                    Price = x.oi.list_price,
                    OrderDate = x.o.order_date
                })
                .Take(100)
                .ToListAsync();

            return Json(salesData, JsonRequestBehavior.AllowGet);
        }

        // Popular Products Report
        public async Task<ActionResult> PopularProductsReport()
        {
            var popularProducts = await db.order_items
                .GroupBy(oi => oi.product_id)
                .Select(g => new
                {
                    ProductId = g.Key,
                    ProductName = db.products.Where(p => p.product_id == g.Key).Select(p => p.product_name).FirstOrDefault(),
                    TotalSold = g.Sum(oi => oi.quantity),
                    TotalRevenue = g.Sum(oi => oi.quantity * oi.list_price)
                })
                .OrderByDescending(x => x.TotalSold)
                .Take(15)
                .ToListAsync();

            return Json(popularProducts, JsonRequestBehavior.AllowGet);
        }

        // Stock Items Report
        public async Task<ActionResult> StockReport()
        {
            var stockData = await db.stocks
                .Join(db.products, s => s.product_id, p => p.product_id, (s, p) => new { s, p })
                .Join(db.brands, x => x.p.brand_id, b => b.brand_id, (x, b) => new { x.s, x.p, b })
                .Join(db.categories, x => x.p.category_id, c => c.category_id, (x, c) => new
                {
                    ProductName = x.p.product_name,
                    Brand = x.b.brand_name,
                    Category = c.category_name,
                    QuantityInStock = x.s.quantity,
                    ListPrice = x.p.list_price
                })
                .Where(x => x.QuantityInStock > 0)
                .Take(50)
                .ToListAsync();

            return Json(stockData, JsonRequestBehavior.AllowGet);
        }

        // Staff Performance Report
        public async Task<ActionResult> StaffPerformanceReport()
        {
            var staffPerformance = await db.orders
                .Join(db.order_items, o => o.order_id, oi => oi.order_id, (o, oi) => new { o, oi })
                .Join(db.staffs, x => x.o.staff_id, s => s.staff_id, (x, s) => new { x.o, x.oi, s })
                .GroupBy(x => new { x.s.staff_id, x.s.first_name, x.s.last_name })
                .Select(g => new
                {
                    StaffName = g.Key.first_name + " " + g.Key.last_name,
                    TotalSales = g.Sum(x => x.oi.quantity),
                    TotalRevenue = g.Sum(x => x.oi.quantity * x.oi.list_price),
                    OrderCount = g.Count()
                })
                .OrderByDescending(x => x.TotalRevenue)
                .Take(10)
                .ToListAsync();

            return Json(staffPerformance, JsonRequestBehavior.AllowGet);
        }

        // Sales Frequency Report (Monthly)
        public async Task<ActionResult> SalesFrequencyReport()
        {
            var salesFrequency = await db.orders
                .Join(db.order_items, o => o.order_id, oi => oi.order_id, (o, oi) => new { o, oi })
                .GroupBy(x => new { Year = x.o.order_date.Year, Month = x.o.order_date.Month })
                .Select(g => new
                {
                    Period = g.Key.Year + "-" + g.Key.Month.ToString("00"),
                    TotalSales = g.Sum(x => x.oi.quantity),
                    TotalOrders = g.Count(),
                    TotalRevenue = g.Sum(x => x.oi.quantity * x.oi.list_price)
                })
                .OrderBy(x => x.Period)
                .Take(12)
                .ToListAsync();

            return Json(salesFrequency, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult SaveReportWithDescription(string reportData, string fileName, string fileType, string description)
        {
            try
            {
                var appDataPath = Server.MapPath("~/App_Data/");
                if (!Directory.Exists(appDataPath))
                    Directory.CreateDirectory(appDataPath);

                var reportsPath = Path.Combine(appDataPath, "Reports");
                if (!Directory.Exists(reportsPath))
                    Directory.CreateDirectory(reportsPath);

                var fullFileName = fileName + "." + fileType;
                var filePath = Path.Combine(reportsPath, fullFileName);

                // Handle different file types
                switch (fileType.ToLower())
                {
                    case "pdf":
                        GeneratePdfReport(reportData, filePath);
                        break;
                    case "docx":
                        GenerateWordReport(reportData, filePath);
                        break;
                    default: // txt, csv, json
                        System.IO.File.WriteAllText(filePath, reportData);
                        break;
                }

                var metaFilePath = filePath + ".meta";
                System.IO.File.WriteAllText(metaFilePath, description ?? "");

                return Json(new { success = true, message = "Report saved successfully!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        private void GeneratePdfReport(string reportData, string filePath)
        {
            using (FileStream fs = new FileStream(filePath, FileMode.Create))
            {
                Document document = new Document(PageSize.A4, 50, 50, 25, 25);
                PdfWriter writer = PdfWriter.GetInstance(document, fs);

                document.Open();

                // Add title
                Font titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 18);
                Paragraph title = new Paragraph("Bike Stores Business Report", titleFont);
                title.Alignment = Element.ALIGN_CENTER;
                title.SpacingAfter = 20f;
                document.Add(title);

                // Add generation date
                Font dateFont = FontFactory.GetFont(FontFactory.HELVETICA, 12);
                Paragraph date = new Paragraph($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}", dateFont);
                date.SpacingAfter = 10f;
                document.Add(date);

                // Add report content
                Font contentFont = FontFactory.GetFont(FontFactory.HELVETICA, 10);
                string[] lines = reportData.Split('\n');
                foreach (string line in lines)
                {
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        Paragraph paragraph = new Paragraph(line, contentFont);
                        document.Add(paragraph);
                    }
                }

                document.Close();
            }
        }

        private void GenerateWordReport(string reportData, string filePath)
        {
            // Create HTML content that can be opened in Word
            string htmlContent = $@"
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset='UTF-8'>
                <title>Bike Stores Report</title>
                <style>
                    body {{ font-family: Arial, sans-serif; margin: 40px; }}
                    .header {{ text-align: center; font-size: 24px; font-weight: bold; margin-bottom: 30px; color: #2c3e50; }}
                    .date {{ text-align: right; font-size: 12px; color: #666; margin-bottom: 20px; }}
                    .content {{ font-size: 11px; line-height: 1.4; }}
                    table {{ border-collapse: collapse; width: 100%; margin: 10px 0; }}
                    th, td {{ border: 1px solid #ddd; padding: 8px; text-align: left; }}
                    th {{ background-color: #f2f2f2; font-weight: bold; }}
                </style>
            </head>
            <body>
                <div class='header'>🚴 Bike Stores Business Report</div>
                <div class='date'>Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}</div>
                <div class='content'>{reportData.Replace("\n", "<br>").Replace(",", ", ")}</div>
            </body>
            </html>";

            System.IO.File.WriteAllText(filePath, htmlContent);
        }

        public ActionResult GetSavedReportsWithDetails()
        {
            var reportFiles = new List<object>();
            var reportsPath = Server.MapPath("~/App_Data/Reports/");

            if (Directory.Exists(reportsPath))
            {
                var files = Directory.GetFiles(reportsPath)
                    .Where(f => !f.EndsWith(".meta"))
                    .ToArray();

                foreach (var file in files)
                {
                    var info = new FileInfo(file);
                    var metaFile = file + ".meta";

                    string description = "No description provided";
                    if (System.IO.File.Exists(metaFile))
                    {
                        description = System.IO.File.ReadAllText(metaFile);
                    }

                    reportFiles.Add(new
                    {
                        FileName = info.Name,
                        Description = description,
                        CreatedDate = info.CreationTime,
                        FileType = info.Extension.TrimStart('.'),
                        FileSize = info.Length
                    });
                }
            }

            return Json(reportFiles, JsonRequestBehavior.AllowGet);
        }

        public ActionResult DownloadFile(string fileName)
        {
            var filePath = Server.MapPath("~/App_Data/Reports/" + fileName);
            if (System.IO.File.Exists(filePath))
            {
                string contentType = "application/octet-stream";

                // Set proper content types
                if (fileName.EndsWith(".pdf"))
                    contentType = "application/pdf";
                else if (fileName.EndsWith(".docx"))
                    contentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
                else if (fileName.EndsWith(".txt"))
                    contentType = "text/plain";
                else if (fileName.EndsWith(".csv"))
                    contentType = "text/csv";
                else if (fileName.EndsWith(".json"))
                    contentType = "application/json";

                return File(filePath, contentType, fileName);
            }
            return HttpNotFound();
        }

        [HttpPost]
        public ActionResult DeleteFile(string fileName)
        {
            try
            {
                var filePath = Server.MapPath("~/App_Data/Reports/" + fileName);
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);

                    var metaFilePath = filePath + ".meta";
                    if (System.IO.File.Exists(metaFilePath))
                    {
                        System.IO.File.Delete(metaFilePath);
                    }

                    return Json(new { success = true });
                }
                return Json(new { success = false, error = "File not found" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }
    }
}