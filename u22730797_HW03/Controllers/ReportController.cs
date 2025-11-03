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
using System.Web.Script.Serialization;
using System.Drawing;
using System.Drawing.Imaging;

namespace u22730797_HW03.Controllers
{
    public class ReportViewModel
    {
        public string ReportTitle { get; set; }
        public string ReportDescription { get; set; }
        public string ReportType { get; set; }
        public string ChartType { get; set; }
        public string ChartConfig { get; set; }
        public string HtmlTable { get; set; }
        public string ReportData { get; set; }
    }

    public class ReportSaveModel
    {
        public string ReportType { get; set; }
        public string FileName { get; set; }
        public string FileType { get; set; }
        public string Description { get; set; }
        public string ReportData { get; set; }
        public string ChartType { get; set; }
        public string ChartImageData { get; set; }
    }

    public class ArchiveFileModel
    {
        public string FileName { get; set; }
        public string Description { get; set; }
        public DateTime CreatedDate { get; set; }
        public string FileType { get; set; }
        public long FileSize { get; set; }
    }

    public class SalesReportData
    {
        public string ProductName { get; set; }
        public string CustomerName { get; set; }
        public string StaffName { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public DateTime OrderDate { get; set; }
    }

    public class PopularProductsData
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public int TotalSold { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    public class StockReportData
    {
        public string ProductName { get; set; }
        public string Brand { get; set; }
        public string Category { get; set; }
        public int QuantityInStock { get; set; }
        public decimal ListPrice { get; set; }
    }

    public class StaffPerformanceData
    {
        public string StaffName { get; set; }
        public int TotalSales { get; set; }
        public decimal TotalRevenue { get; set; }
        public int OrderCount { get; set; }
    }

    public class SalesFrequencyData
    {
        public string Period { get; set; }
        public int TotalSales { get; set; }
        public int TotalOrders { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    public class CustomerPerformanceData
    {
        public string CustomerName { get; set; }
        public int TotalPurchases { get; set; }
        public decimal TotalSpent { get; set; }
        public int OrderCount { get; set; }
    }

    public class ReportController : Controller
    {
        private BikeStoresContext db = new BikeStoresContext();

        public async Task<ActionResult> Index()
        {
            try
            {
                ViewBag.TotalProducts = await db.products.CountAsync();
                ViewBag.TotalCustomers = await db.customers.CountAsync();
                ViewBag.TotalStaff = await db.staffs.CountAsync();
                ViewBag.TotalSales = await db.order_items.SumAsync(oi => oi.quantity);
            }
            catch (Exception ex)
            {
                ViewBag.TotalProducts = 0;
                ViewBag.TotalCustomers = 0;
                ViewBag.TotalStaff = 0;
                ViewBag.TotalSales = 0;
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> GenerateReport(string ReportType, string ChartType)
        {
            if (string.IsNullOrEmpty(ReportType))
            {
                TempData["ErrorMessage"] = "Please select a report type.";
                return RedirectToAction("Index");
            }

            switch (ReportType)
            {
                case "sales":
                    return await SalesReportWithChart(ChartType);
                case "popular":
                    return await PopularProductsReportWithChart(ChartType);
                case "stock":
                    return await StockReportWithChart(ChartType);
                case "staff":
                    return await StaffPerformanceReportWithChart(ChartType);
                case "frequency":
                    return await SalesFrequencyReportWithChart(ChartType);
                case "customer":
                    return await CustomerPerformanceReportWithChart(ChartType);
                default:
                    TempData["ErrorMessage"] = "Invalid report type selected.";
                    return RedirectToAction("Index");
            }
        }

        private async Task<ActionResult> SalesReportWithChart(string chartType)
        {
            var salesData = await db.orders
                .Join(db.order_items, o => o.order_id, oi => oi.order_id, (o, oi) => new { o, oi })
                .Join(db.products, x => x.oi.product_id, p => p.product_id, (x, p) => new { x.o, x.oi, p })
                .Join(db.customers, x => x.o.customer_id, c => c.customer_id, (x, c) => new { x.o, x.oi, x.p, c })
                .Join(db.staffs, x => x.o.staff_id, s => s.staff_id, (x, s) => new SalesReportData
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

            var topProducts = salesData.GroupBy(x => x.ProductName)
                                      .Select(g => new { Product = g.Key, TotalSales = g.Sum(x => x.Quantity) })
                                      .OrderByDescending(x => x.TotalSales)
                                      .Take(10)
                                      .ToList();

            var chartConfig = GenerateChartConfig(
                chartType,
                topProducts.Select(x => x.Product).ToArray(),
                topProducts.Select(x => x.TotalSales).ToArray(),
                "Top 10 Products by Sales Volume",
                "Products",
                "Units Sold"
            );

            var htmlTable = GenerateSalesHtmlTable(salesData);

            var model = new ReportViewModel
            {
                ReportTitle = "Current Sales Report",
                ReportDescription = "Analysis of recent sales transactions including customer, product, and staff details",
                ReportType = "sales",
                ChartType = chartType,
                ChartConfig = chartConfig,
                HtmlTable = htmlTable,
                ReportData = SerializeToJson(salesData)
            };

            return View("ReportView", model);
        }

        private async Task<ActionResult> PopularProductsReportWithChart(string chartType)
        {
            var popularProducts = await db.order_items
                .GroupBy(oi => oi.product_id)
                .Select(g => new PopularProductsData
                {
                    ProductId = g.Key,
                    ProductName = db.products.Where(p => p.product_id == g.Key).Select(p => p.product_name).FirstOrDefault(),
                    TotalSold = g.Sum(oi => oi.quantity),
                    TotalRevenue = g.Sum(oi => oi.quantity * oi.list_price)
                })
                .OrderByDescending(x => x.TotalSold)
                .Take(15)
                .ToListAsync();

            var chartConfig = GenerateChartConfig(
                chartType,
                popularProducts.Select(x => x.ProductName).ToArray(),
                popularProducts.Select(x => x.TotalSold).ToArray(),
                "Most Popular Products by Sales Volume",
                "Products",
                "Units Sold"
            );

            var htmlTable = GeneratePopularProductsHtmlTable(popularProducts);

            var model = new ReportViewModel
            {
                ReportTitle = "Popular Products Report",
                ReportDescription = "Analysis of top-selling products to identify inventory and marketing opportunities",
                ReportType = "popular",
                ChartType = chartType,
                ChartConfig = chartConfig,
                HtmlTable = htmlTable,
                ReportData = SerializeToJson(popularProducts)
            };

            return View("ReportView", model);
        }

        private async Task<ActionResult> StockReportWithChart(string chartType)
        {
            var stockData = await db.stocks
                .Join(db.products, s => s.product_id, p => p.product_id, (s, p) => new { s, p })
                .Join(db.brands, x => x.p.brand_id, b => b.brand_id, (x, b) => new { x.s, x.p, b })
                .Join(db.categories, x => x.p.category_id, c => c.category_id, (x, c) => new StockReportData
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

            var brandStock = stockData.GroupBy(x => x.Brand)
                                     .Select(g => new { Brand = g.Key, TotalStock = g.Sum(x => x.QuantityInStock) })
                                     .OrderByDescending(x => x.TotalStock)
                                     .ToList();

            var chartConfig = GenerateChartConfig(
                chartType,
                brandStock.Select(x => x.Brand).ToArray(),
                brandStock.Select(x => x.TotalStock).ToArray(),
                "Stock Levels by Brand",
                "Brands",
                "Units in Stock"
            );

            var htmlTable = GenerateStockHtmlTable(stockData);

            var model = new ReportViewModel
            {
                ReportTitle = "Stock Items Report",
                ReportDescription = "Analysis of current inventory levels across brands and categories",
                ReportType = "stock",
                ChartType = chartType,
                ChartConfig = chartConfig,
                HtmlTable = htmlTable,
                ReportData = SerializeToJson(stockData)
            };

            return View("ReportView", model);
        }

        private async Task<ActionResult> StaffPerformanceReportWithChart(string chartType)
        {
            var staffPerformance = await db.orders
                .Join(db.order_items, o => o.order_id, oi => oi.order_id, (o, oi) => new { o, oi })
                .Join(db.staffs, x => x.o.staff_id, s => s.staff_id, (x, s) => new { x.o, x.oi, s })
                .GroupBy(x => new { x.s.staff_id, x.s.first_name, x.s.last_name })
                .Select(g => new StaffPerformanceData
                {
                    StaffName = g.Key.first_name + " " + g.Key.last_name,
                    TotalSales = g.Sum(x => x.oi.quantity),
                    TotalRevenue = g.Sum(x => x.oi.quantity * x.oi.list_price),
                    OrderCount = g.Count()
                })
                .OrderByDescending(x => x.TotalRevenue)
                .Take(10)
                .ToListAsync();

            var chartConfig = GenerateChartConfig(
                chartType,
                staffPerformance.Select(x => x.StaffName).ToArray(),
                staffPerformance.Select(x => (int)x.TotalRevenue).ToArray(),
                "Staff Performance by Revenue Generated",
                "Staff Members",
                "Revenue ($)"
            );

            var htmlTable = GenerateStaffPerformanceHtmlTable(staffPerformance);

            var model = new ReportViewModel
            {
                ReportTitle = "Staff Performance Ranking",
                ReportDescription = "Performance analysis of sales staff based on revenue and sales volume",
                ReportType = "staff",
                ChartType = chartType,
                ChartConfig = chartConfig,
                HtmlTable = htmlTable,
                ReportData = SerializeToJson(staffPerformance)
            };

            return View("ReportView", model);
        }

        private async Task<ActionResult> SalesFrequencyReportWithChart(string chartType)
        {
            var salesFrequency = await db.orders
                .Join(db.order_items, o => o.order_id, oi => oi.order_id, (o, oi) => new { o, oi })
                .GroupBy(x => new { Year = x.o.order_date.Year, Month = x.o.order_date.Month })
                .Select(g => new SalesFrequencyData
                {
                    Period = g.Key.Year + "-" + g.Key.Month.ToString("00"),
                    TotalSales = g.Sum(x => x.oi.quantity),
                    TotalOrders = g.Count(),
                    TotalRevenue = g.Sum(x => x.oi.quantity * x.oi.list_price)
                })
                .OrderBy(x => x.Period)
                .Take(12)
                .ToListAsync();

            var effectiveChartType = "line";

            var chartConfig = GenerateChartConfig(
                effectiveChartType,
                salesFrequency.Select(x => x.Period).ToArray(),
                salesFrequency.Select(x => x.TotalSales).ToArray(),
                "Monthly Sales Trends",
                "Time Period",
                "Units Sold"
            );

            var htmlTable = GenerateSalesFrequencyHtmlTable(salesFrequency);

            var model = new ReportViewModel
            {
                ReportTitle = "Sales Frequency Report",
                ReportDescription = "Analysis of sales patterns and trends over time",
                ReportType = "frequency",
                ChartType = effectiveChartType,
                ChartConfig = chartConfig,
                HtmlTable = htmlTable,
                ReportData = SerializeToJson(salesFrequency)
            };

            return View("ReportView", model);
        }

        private async Task<ActionResult> CustomerPerformanceReportWithChart(string chartType)
        {
            var customerPerformance = await db.orders
                .Join(db.order_items, o => o.order_id, oi => oi.order_id, (o, oi) => new { o, oi })
                .Join(db.customers, x => x.o.customer_id, c => c.customer_id, (x, c) => new { x.o, x.oi, c })
                .GroupBy(x => new { x.c.customer_id, x.c.first_name, x.c.last_name })
                .Select(g => new CustomerPerformanceData
                {
                    CustomerName = g.Key.first_name + " " + g.Key.last_name,
                    TotalPurchases = g.Sum(x => x.oi.quantity),
                    TotalSpent = g.Sum(x => x.oi.quantity * x.oi.list_price),
                    OrderCount = g.Count()
                })
                .OrderByDescending(x => x.TotalSpent)
                .Take(10)
                .ToListAsync();

            var chartConfig = GenerateChartConfig(
                chartType,
                customerPerformance.Select(x => x.CustomerName).ToArray(),
                customerPerformance.Select(x => (int)x.TotalSpent).ToArray(),
                "Top Customers by Total Spending",
                "Customers",
                "Amount Spent ($)"
            );

            var htmlTable = GenerateCustomerPerformanceHtmlTable(customerPerformance);

            var model = new ReportViewModel
            {
                ReportTitle = "Customer Performance Ranking",
                ReportDescription = "Analysis of customer purchasing behavior and loyalty",
                ReportType = "customer",
                ChartType = chartType,
                ChartConfig = chartConfig,
                HtmlTable = htmlTable,
                ReportData = SerializeToJson(customerPerformance)
            };

            return View("ReportView", model);
        }

        private string GenerateChartConfig(string chartType, string[] labels, int[] data, string title, string xLabel, string yLabel)
        {
            var backgroundColor = new string[] {
                "rgba(255, 99, 132, 0.7)", "rgba(54, 162, 235, 0.7)", "rgba(255, 205, 86, 0.7)",
                "rgba(75, 192, 192, 0.7)", "rgba(153, 102, 255, 0.7)", "rgba(255, 159, 64, 0.7)",
                "rgba(201, 203, 207, 0.7)", "rgba(255, 99, 132, 0.7)", "rgba(54, 162, 235, 0.7)",
                "rgba(255, 205, 86, 0.7)", "rgba(75, 192, 192, 0.7)", "rgba(153, 102, 255, 0.7)"
            };

            return $@"{{
        type: '{chartType}',
        data: {{
            labels: {SerializeToJson(labels)},
            datasets: [{{
                label: '{yLabel}',
                data: {SerializeToJson(data)},
                backgroundColor: {SerializeToJson(backgroundColor)},
                borderColor: {SerializeToJson(backgroundColor.Select(c => c.Replace("0.7", "1")).ToArray())},
                borderWidth: 2
            }}]
        }},
        options: {{
            responsive: true,
            maintainAspectRatio: false,
            plugins: {{
                title: {{
                    display: true,
                    text: '{title}',
                    font: {{ size: 16 }}
                }},
                legend: {{
                    display: {(chartType == "pie" || chartType == "doughnut" ? "true" : "false")}
                }}
            }},
            scales: {{
                y: {{
                    beginAtZero: true,
                    title: {{
                        display: true,
                        text: '{yLabel}'
                    }}
                }},
                x: {{
                    title: {{
                        display: true,
                        text: '{xLabel}'
                    }}
                }}
            }}
        }}
    }}";
        }

        private string GenerateSalesHtmlTable(List<SalesReportData> salesData)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("<table class='table table-striped table-bordered'>");
            sb.AppendLine("<thead class='thead-dark'><tr><th>Product</th><th>Customer</th><th>Staff</th><th>Quantity</th><th>Price</th><th>Date</th></tr></thead>");
            sb.AppendLine("<tbody>");

            foreach (var item in salesData)
            {
                sb.AppendLine($"<tr><td>{item.ProductName}</td><td>{item.CustomerName}</td><td>{item.StaffName}</td><td>{item.Quantity}</td><td>${item.Price:F2}</td><td>{item.OrderDate:yyyy-MM-dd}</td></tr>");
            }

            sb.AppendLine("</tbody></table>");
            return sb.ToString();
        }

        private string GeneratePopularProductsHtmlTable(List<PopularProductsData> popularProducts)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("<table class='table table-striped table-bordered'>");
            sb.AppendLine("<thead class='thead-dark'><tr><th>Product</th><th>Total Sold</th><th>Total Revenue</th></tr></thead>");
            sb.AppendLine("<tbody>");

            foreach (var item in popularProducts)
            {
                sb.AppendLine($"<tr><td>{item.ProductName}</td><td>{item.TotalSold}</td><td>${item.TotalRevenue:F2}</td></tr>");
            }

            sb.AppendLine("</tbody></table>");
            return sb.ToString();
        }

        private string GenerateStockHtmlTable(List<StockReportData> stockData)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("<table class='table table-striped table-bordered'>");
            sb.AppendLine("<thead class='thead-dark'><tr><th>Product</th><th>Brand</th><th>Category</th><th>Quantity</th><th>Price</th></tr></thead>");
            sb.AppendLine("<tbody>");

            foreach (var item in stockData)
            {
                sb.AppendLine($"<tr><td>{item.ProductName}</td><td>{item.Brand}</td><td>{item.Category}</td><td>{item.QuantityInStock}</td><td>${item.ListPrice:F2}</td></tr>");
            }

            sb.AppendLine("</tbody></table>");
            return sb.ToString();
        }

        private string GenerateStaffPerformanceHtmlTable(List<StaffPerformanceData> staffPerformance)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("<table class='table table-striped table-bordered'>");
            sb.AppendLine("<thead class='thead-dark'><tr><th>Staff Name</th><th>Total Sales</th><th>Total Revenue</th><th>Order Count</th></tr></thead>");
            sb.AppendLine("<tbody>");

            foreach (var item in staffPerformance)
            {
                sb.AppendLine($"<tr><td>{item.StaffName}</td><td>{item.TotalSales}</td><td>${item.TotalRevenue:F2}</td><td>{item.OrderCount}</td></tr>");
            }

            sb.AppendLine("</tbody></table>");
            return sb.ToString();
        }

        private string GenerateSalesFrequencyHtmlTable(List<SalesFrequencyData> salesFrequency)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("<table class='table table-striped table-bordered'>");
            sb.AppendLine("<thead class='thead-dark'><tr><th>Period</th><th>Total Sales</th><th>Total Orders</th><th>Total Revenue</th></tr></thead>");
            sb.AppendLine("<tbody>");

            foreach (var item in salesFrequency)
            {
                sb.AppendLine($"<tr><td>{item.Period}</td><td>{item.TotalSales}</td><td>{item.TotalOrders}</td><td>${item.TotalRevenue:F2}</td></tr>");
            }

            sb.AppendLine("</tbody></table>");
            return sb.ToString();
        }

        private string GenerateCustomerPerformanceHtmlTable(List<CustomerPerformanceData> customerPerformance)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("<table class='table table-striped table-bordered'>");
            sb.AppendLine("<thead class='thead-dark'><tr><th>Customer Name</th><th>Total Purchases</th><th>Total Spent</th><th>Order Count</th></tr></thead>");
            sb.AppendLine("<tbody>");

            foreach (var item in customerPerformance)
            {
                sb.AppendLine($"<tr><td>{item.CustomerName}</td><td>{item.TotalPurchases}</td><td>${item.TotalSpent:F2}</td><td>{item.OrderCount}</td></tr>");
            }

            sb.AppendLine("</tbody></table>");
            return sb.ToString();
        }

        private string SerializeToJson(object obj)
        {
            var serializer = new JavaScriptSerializer();
            return serializer.Serialize(obj);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SaveReport(ReportSaveModel model)
        {
            try
            {
                if (string.IsNullOrEmpty(model.FileName))
                {
                    TempData["ErrorMessage"] = "Please enter a file name.";
                    return RedirectToAction("Index");
                }

                var appDataPath = Server.MapPath("~/App_Data/");
                if (!Directory.Exists(appDataPath))
                    Directory.CreateDirectory(appDataPath);

                var reportsPath = Path.Combine(appDataPath, "Reports");
                if (!Directory.Exists(reportsPath))
                    Directory.CreateDirectory(reportsPath);

                var fullFileName = model.FileName + "." + model.FileType;
                var filePath = Path.Combine(reportsPath, fullFileName);

                switch (model.FileType.ToLower())
                {
                    case "pdf":
                        GeneratePdfWithExactChart(model, filePath);
                        break;
                    case "docx":
                        GenerateWordReportWithChart(model, filePath);
                        break;
                    case "csv":
                        GenerateCsvReport(model, filePath);
                        break;
                    case "txt":
                        GenerateTextReport(model, filePath);
                        break;
                    default:
                        GenerateTextReport(model, filePath);
                        break;
                }

                var metaFilePath = filePath + ".meta";
                System.IO.File.WriteAllText(metaFilePath, model.Description ?? "");

                TempData["SuccessMessage"] = "Report saved successfully!";
                return RedirectToAction("Archive");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error saving report: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        private void GeneratePdfWithExactChart(ReportSaveModel model, string filePath)
        {
            using (FileStream fs = new FileStream(filePath, FileMode.Create))
            {
                Document document = new Document(PageSize.A4, 50, 50, 25, 25);
                PdfWriter writer = PdfWriter.GetInstance(document, fs);

                document.Open();

                iTextSharp.text.Font titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 18);
                Paragraph title = new Paragraph("Bike Stores Business Report", titleFont);
                title.Alignment = Element.ALIGN_CENTER;
                title.SpacingAfter = 20f;
                document.Add(title);

                iTextSharp.text.Font dateFont = FontFactory.GetFont(FontFactory.HELVETICA, 12);
                Paragraph date = new Paragraph($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}", dateFont);
                date.SpacingAfter = 10f;
                document.Add(date);

                Paragraph reportTypePara = new Paragraph($"Report Type: {GetReportTypeDisplayName(model.ReportType)}", dateFont);
                reportTypePara.SpacingAfter = 10f;
                document.Add(reportTypePara);

                Paragraph chartTypePara = new Paragraph($"Chart Type: {model.ChartType.ToUpper()} Chart", dateFont);
                chartTypePara.SpacingAfter = 10f;
                document.Add(chartTypePara);

                if (!string.IsNullOrEmpty(model.Description))
                {
                    Paragraph desc = new Paragraph($"Description: {model.Description}", dateFont);
                    desc.SpacingAfter = 10f;
                    document.Add(desc);
                }

                document.Add(new Paragraph(" "));
                document.Add(new Paragraph(new Chunk(new iTextSharp.text.pdf.draw.LineSeparator())));
                document.Add(new Paragraph(" "));

                iTextSharp.text.Font sectionFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 14);
                Paragraph chartHeader = new Paragraph("Chart Visualization", sectionFont);
                chartHeader.SpacingAfter = 10f;
                document.Add(chartHeader);

                string chartDescription = GetChartDescription(model.ReportType, model.ChartType);
                Paragraph chartDesc = new Paragraph(chartDescription, dateFont);
                chartDesc.SpacingAfter = 15f;
                document.Add(chartDesc);

                if (!string.IsNullOrEmpty(model.ChartImageData))
                {
                    try
                    {
                        string base64Data = model.ChartImageData.Split(',')[1];
                        byte[] chartImageBytes = Convert.FromBase64String(base64Data);

                        iTextSharp.text.Image chartImage = iTextSharp.text.Image.GetInstance(chartImageBytes);
                        chartImage.Alignment = iTextSharp.text.Image.ALIGN_CENTER;

                        if (chartImage.Width > document.PageSize.Width - 100)
                        {
                            chartImage.ScaleToFit(document.PageSize.Width - 100, document.PageSize.Height - 200);
                        }

                        chartImage.SpacingAfter = 15f;
                        document.Add(chartImage);

                        Paragraph chartNote = new Paragraph("Chart generated using the same visualization as shown in the web application.", dateFont);
                        chartNote.SpacingAfter = 15f;
                        document.Add(chartNote);
                    }
                    catch (Exception ex)
                    {
                        Paragraph error = new Paragraph($"Note: Chart image could not be embedded. {ex.Message}", dateFont);
                        document.Add(error);
                        AddChartDataSummary(document, model);
                    }
                }
                else
                {
                    Paragraph noChart = new Paragraph("Chart visualization is available in the web application. Data summary is shown below.", dateFont);
                    document.Add(noChart);
                    AddChartDataSummary(document, model);
                }

                document.Add(new Paragraph(" "));
                document.Add(new Paragraph(new Chunk(new iTextSharp.text.pdf.draw.LineSeparator())));
                document.Add(new Paragraph(" "));

                Paragraph dataHeader = new Paragraph("Detailed Data Table", sectionFont);
                dataHeader.SpacingAfter = 10f;
                document.Add(dataHeader);

                AddTableDataToPdf(document, model);

                document.Close();
            }
        }

        private void AddChartDataSummary(Document document, ReportSaveModel model)
        {
            try
            {
                var serializer = new JavaScriptSerializer();
                dynamic reportData = serializer.Deserialize<object>(model.ReportData);

                iTextSharp.text.Font dataFont = FontFactory.GetFont(FontFactory.HELVETICA, 10);
                iTextSharp.text.Font headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10);

                PdfPTable table = new PdfPTable(2);
                table.WidthPercentage = 100;
                table.SpacingBefore = 10f;
                table.SpacingAfter = 10f;

                table.AddCell(new PdfPCell(new Phrase("Item", headerFont)));
                table.AddCell(new PdfPCell(new Phrase("Value", headerFont)));

                switch (model.ReportType)
                {
                    case "sales":
                        if (reportData is object[] salesArray)
                        {
                            var topProducts = salesArray
                                .Cast<Dictionary<string, object>>()
                                .GroupBy(x => x["ProductName"].ToString())
                                .Select(g => new { Product = g.Key, TotalSales = g.Sum(x => Convert.ToInt32(x["Quantity"])) })
                                .OrderByDescending(x => x.TotalSales)
                                .Take(10)
                                .ToList();

                            foreach (var product in topProducts)
                            {
                                table.AddCell(new PdfPCell(new Phrase(product.Product, dataFont)));
                                table.AddCell(new PdfPCell(new Phrase(product.TotalSales.ToString(), dataFont)));
                            }
                        }
                        break;

                    case "popular":
                        if (reportData is object[] popularArray)
                        {
                            foreach (var item in popularArray.Cast<Dictionary<string, object>>())
                            {
                                table.AddCell(new PdfPCell(new Phrase(item["ProductName"].ToString(), dataFont)));
                                table.AddCell(new PdfPCell(new Phrase(item["TotalSold"].ToString(), dataFont)));
                            }
                        }
                        break;

                    case "stock":
                        if (reportData is object[] stockArray)
                        {
                            var brandStock = stockArray.Cast<Dictionary<string, object>>()
                                                      .GroupBy(x => x["Brand"].ToString())
                                                      .Select(g => new { Brand = g.Key, TotalStock = g.Sum(x => Convert.ToInt32(x["QuantityInStock"])) })
                                                      .OrderByDescending(x => x.TotalStock)
                                                      .ToList();

                            foreach (var brand in brandStock)
                            {
                                table.AddCell(new PdfPCell(new Phrase(brand.Brand, dataFont)));
                                table.AddCell(new PdfPCell(new Phrase(brand.TotalStock.ToString(), dataFont)));
                            }
                        }
                        break;

                    case "staff":
                        if (reportData is object[] staffArray)
                        {
                            foreach (var item in staffArray.Cast<Dictionary<string, object>>())
                            {
                                table.AddCell(new PdfPCell(new Phrase(item["StaffName"].ToString(), dataFont)));
                                table.AddCell(new PdfPCell(new Phrase($"${Convert.ToDecimal(item["TotalRevenue"]):F2}", dataFont)));
                            }
                        }
                        break;

                    case "frequency":
                        if (reportData is object[] frequencyArray)
                        {
                            foreach (var item in frequencyArray.Cast<Dictionary<string, object>>())
                            {
                                table.AddCell(new PdfPCell(new Phrase(item["Period"].ToString(), dataFont)));
                                table.AddCell(new PdfPCell(new Phrase(item["TotalSales"].ToString(), dataFont)));
                            }
                        }
                        break;

                    case "customer":
                        if (reportData is object[] customerArray)
                        {
                            foreach (var item in customerArray.Cast<Dictionary<string, object>>())
                            {
                                table.AddCell(new PdfPCell(new Phrase(item["CustomerName"].ToString(), dataFont)));
                                table.AddCell(new PdfPCell(new Phrase($"${Convert.ToDecimal(item["TotalSpent"]):F2}", dataFont)));
                            }
                        }
                        break;
                }

                document.Add(table);
            }
            catch (Exception ex)
            {
                iTextSharp.text.Font errorFont = FontFactory.GetFont(FontFactory.HELVETICA, 10);
                Paragraph error = new Paragraph($"Note: Chart data summary could not be generated. {ex.Message}", errorFont);
                document.Add(error);
            }
        }

        private void AddTableDataToPdf(Document document, ReportSaveModel model)
        {
            try
            {
                var serializer = new JavaScriptSerializer();
                dynamic reportData = serializer.Deserialize<object>(model.ReportData);

                iTextSharp.text.Font dataFont = FontFactory.GetFont(FontFactory.HELVETICA, 8);
                iTextSharp.text.Font headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 8);

                int columnCount = GetColumnCountForReportType(model.ReportType);
                PdfPTable table = new PdfPTable(columnCount);
                table.WidthPercentage = 100;
                table.SpacingBefore = 10f;
                table.SpacingAfter = 10f;

                AddTableHeaders(table, headerFont, model.ReportType);

                switch (model.ReportType)
                {
                    case "sales":
                        if (reportData is object[] salesArray)
                        {
                            foreach (var item in salesArray.Cast<Dictionary<string, object>>())
                            {
                                table.AddCell(new PdfPCell(new Phrase(item["ProductName"].ToString(), dataFont)));
                                table.AddCell(new PdfPCell(new Phrase(item["CustomerName"].ToString(), dataFont)));
                                table.AddCell(new PdfPCell(new Phrase(item["StaffName"].ToString(), dataFont)));
                                table.AddCell(new PdfPCell(new Phrase(item["Quantity"].ToString(), dataFont)));
                                table.AddCell(new PdfPCell(new Phrase($"${Convert.ToDecimal(item["Price"]):F2}", dataFont)));
                                table.AddCell(new PdfPCell(new Phrase(DateTime.Parse(item["OrderDate"].ToString()).ToString("yyyy-MM-dd"), dataFont)));
                            }
                        }
                        break;

                    case "popular":
                        if (reportData is object[] popularArray)
                        {
                            foreach (var item in popularArray.Cast<Dictionary<string, object>>())
                            {
                                table.AddCell(new PdfPCell(new Phrase(item["ProductName"].ToString(), dataFont)));
                                table.AddCell(new PdfPCell(new Phrase(item["TotalSold"].ToString(), dataFont)));
                                table.AddCell(new PdfPCell(new Phrase($"${Convert.ToDecimal(item["TotalRevenue"]):F2}", dataFont)));
                            }
                        }
                        break;

                    case "stock":
                        if (reportData is object[] stockArray)
                        {
                            foreach (var item in stockArray.Cast<Dictionary<string, object>>())
                            {
                                table.AddCell(new PdfPCell(new Phrase(item["ProductName"].ToString(), dataFont)));
                                table.AddCell(new PdfPCell(new Phrase(item["Brand"].ToString(), dataFont)));
                                table.AddCell(new PdfPCell(new Phrase(item["Category"].ToString(), dataFont)));
                                table.AddCell(new PdfPCell(new Phrase(item["QuantityInStock"].ToString(), dataFont)));
                                table.AddCell(new PdfPCell(new Phrase($"${Convert.ToDecimal(item["ListPrice"]):F2}", dataFont)));
                            }
                        }
                        break;

                    case "staff":
                        if (reportData is object[] staffArray)
                        {
                            foreach (var item in staffArray.Cast<Dictionary<string, object>>())
                            {
                                table.AddCell(new PdfPCell(new Phrase(item["StaffName"].ToString(), dataFont)));
                                table.AddCell(new PdfPCell(new Phrase(item["TotalSales"].ToString(), dataFont)));
                                table.AddCell(new PdfPCell(new Phrase($"${Convert.ToDecimal(item["TotalRevenue"]):F2}", dataFont)));
                                table.AddCell(new PdfPCell(new Phrase(item["OrderCount"].ToString(), dataFont)));
                            }
                        }
                        break;

                    case "frequency":
                        if (reportData is object[] frequencyArray)
                        {
                            foreach (var item in frequencyArray.Cast<Dictionary<string, object>>())
                            {
                                table.AddCell(new PdfPCell(new Phrase(item["Period"].ToString(), dataFont)));
                                table.AddCell(new PdfPCell(new Phrase(item["TotalSales"].ToString(), dataFont)));
                                table.AddCell(new PdfPCell(new Phrase(item["TotalOrders"].ToString(), dataFont)));
                                table.AddCell(new PdfPCell(new Phrase($"${Convert.ToDecimal(item["TotalRevenue"]):F2}", dataFont)));
                            }
                        }
                        break;

                    case "customer":
                        if (reportData is object[] customerArray)
                        {
                            foreach (var item in customerArray.Cast<Dictionary<string, object>>())
                            {
                                table.AddCell(new PdfPCell(new Phrase(item["CustomerName"].ToString(), dataFont)));
                                table.AddCell(new PdfPCell(new Phrase(item["TotalPurchases"].ToString(), dataFont)));
                                table.AddCell(new PdfPCell(new Phrase($"${Convert.ToDecimal(item["TotalSpent"]):F2}", dataFont)));
                                table.AddCell(new PdfPCell(new Phrase(item["OrderCount"].ToString(), dataFont)));
                            }
                        }
                        break;
                }

                document.Add(table);
            }
            catch (Exception ex)
            {
                iTextSharp.text.Font errorFont = FontFactory.GetFont(FontFactory.HELVETICA, 10);
                Paragraph error = new Paragraph($"Note: Table data could not be generated. {ex.Message}", errorFont);
                document.Add(error);
            }
        }

        private int GetColumnCountForReportType(string reportType)
        {
            switch (reportType)
            {
                case "sales": return 6;
                case "popular": return 3;
                case "stock": return 5;
                case "staff": return 4;
                case "frequency": return 4;
                case "customer": return 4;
                default: return 2;
            }
        }

        private void AddTableHeaders(PdfPTable table, iTextSharp.text.Font headerFont, string reportType)
        {
            switch (reportType)
            {
                case "sales":
                    table.AddCell(new PdfPCell(new Phrase("Product", headerFont)));
                    table.AddCell(new PdfPCell(new Phrase("Customer", headerFont)));
                    table.AddCell(new PdfPCell(new Phrase("Staff", headerFont)));
                    table.AddCell(new PdfPCell(new Phrase("Quantity", headerFont)));
                    table.AddCell(new PdfPCell(new Phrase("Price", headerFont)));
                    table.AddCell(new PdfPCell(new Phrase("Date", headerFont)));
                    break;
                case "popular":
                    table.AddCell(new PdfPCell(new Phrase("Product", headerFont)));
                    table.AddCell(new PdfPCell(new Phrase("Total Sold", headerFont)));
                    table.AddCell(new PdfPCell(new Phrase("Total Revenue", headerFont)));
                    break;
                case "stock":
                    table.AddCell(new PdfPCell(new Phrase("Product", headerFont)));
                    table.AddCell(new PdfPCell(new Phrase("Brand", headerFont)));
                    table.AddCell(new PdfPCell(new Phrase("Category", headerFont)));
                    table.AddCell(new PdfPCell(new Phrase("Quantity", headerFont)));
                    table.AddCell(new PdfPCell(new Phrase("Price", headerFont)));
                    break;
                case "staff":
                    table.AddCell(new PdfPCell(new Phrase("Staff Name", headerFont)));
                    table.AddCell(new PdfPCell(new Phrase("Total Sales", headerFont)));
                    table.AddCell(new PdfPCell(new Phrase("Total Revenue", headerFont)));
                    table.AddCell(new PdfPCell(new Phrase("Order Count", headerFont)));
                    break;
                case "frequency":
                    table.AddCell(new PdfPCell(new Phrase("Period", headerFont)));
                    table.AddCell(new PdfPCell(new Phrase("Total Sales", headerFont)));
                    table.AddCell(new PdfPCell(new Phrase("Total Orders", headerFont)));
                    table.AddCell(new PdfPCell(new Phrase("Total Revenue", headerFont)));
                    break;
                case "customer":
                    table.AddCell(new PdfPCell(new Phrase("Customer Name", headerFont)));
                    table.AddCell(new PdfPCell(new Phrase("Total Purchases", headerFont)));
                    table.AddCell(new PdfPCell(new Phrase("Total Spent", headerFont)));
                    table.AddCell(new PdfPCell(new Phrase("Order Count", headerFont)));
                    break;
            }
        }

        private void GenerateWordReportWithChart(ReportSaveModel model, string filePath)
        {
            string htmlContent = GenerateHtmlReport(model);
            System.IO.File.WriteAllText(filePath, htmlContent);
        }

        private string GenerateHtmlReport(ReportSaveModel model)
        {
            var serializer = new JavaScriptSerializer();
            dynamic reportData = serializer.Deserialize<object>(model.ReportData);

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($@"<!DOCTYPE html>
<html>
<head>
    <title>Bike Stores Report - {GetReportTypeDisplayName(model.ReportType)}</title>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 40px; }}
        .header {{ text-align: center; font-size: 24px; font-weight: bold; margin-bottom: 30px; color: #2c3e50; }}
        .info {{ margin-bottom: 20px; padding: 15px; background-color: #f8f9fa; border-radius: 5px; }}
        .section {{ margin: 30px 0; }}
        .section-title {{ font-size: 18px; font-weight: bold; margin-bottom: 15px; color: #2c3e50; border-bottom: 2px solid #007bff; padding-bottom: 5px; }}
        table {{ width: 100%; border-collapse: collapse; margin: 10px 0; }}
        th, td {{ border: 1px solid #ddd; padding: 12px; text-align: left; }}
        th {{ background-color: #f2f2f2; font-weight: bold; }}
        .note {{ font-style: italic; color: #666; margin: 10px 0; }}
        .chart-img {{ max-width: 100%; height: auto; margin: 20px 0; border: 1px solid #ddd; }}
    </style>
</head>
<body>
    <div class='header'>🚴 Bike Stores Business Report</div>
    
    <div class='info'>
        <strong>Generated:</strong> {DateTime.Now:yyyy-MM-dd HH:mm:ss}<br>
        <strong>Report Type:</strong> {GetReportTypeDisplayName(model.ReportType)}<br>
        <strong>Chart Type:</strong> {model.ChartType.ToUpper()} Chart<br>");

            if (!string.IsNullOrEmpty(model.Description))
            {
                sb.AppendLine($"        <strong>Description:</strong> {model.Description}<br>");
            }

            sb.AppendLine($@"    </div>

    <div class='section'>
        <div class='section-title'>Chart Visualization</div>
        <p>{GetChartDescription(model.ReportType, model.ChartType)}</p>");

            if (!string.IsNullOrEmpty(model.ChartImageData))
            {
                sb.AppendLine($@"        <img src='{model.ChartImageData}' alt='Chart Visualization' class='chart-img' />");
            }
            else
            {
                sb.AppendLine($@"        <p class='note'>Chart visualization not available in exported format.</p>");
            }

            sb.AppendLine($@"    </div>

    <div class='section'>
        <div class='section-title'>Detailed Data</div>");

            sb.AppendLine(GenerateHtmlDetailedTable(model.ReportType, reportData));

            sb.AppendLine($@"
    </div>
</body>
</html>");

            return sb.ToString();
        }

        private string GenerateHtmlDetailedTable(string reportType, dynamic reportData)
        {
            return "<p>Detailed table would appear here</p>";
        }

        private void GenerateCsvReport(ReportSaveModel model, string filePath)
        {
            var csvContent = GenerateCsvContent(model);
            System.IO.File.WriteAllText(filePath, csvContent);
        }

        private string GenerateCsvContent(ReportSaveModel model)
        {
            return GenerateComprehensiveReport(model);
        }

        private void GenerateTextReport(ReportSaveModel model, string filePath)
        {
            var textContent = GenerateTextContent(model);
            System.IO.File.WriteAllText(filePath, textContent);
        }

        private string GenerateTextContent(ReportSaveModel model)
        {
            return GenerateComprehensiveReport(model);
        }

        private string GetReportTypeDisplayName(string reportType)
        {
            switch (reportType)
            {
                case "sales": return "Current Sales Report";
                case "popular": return "Popular Products Report";
                case "stock": return "Stock Items Report";
                case "staff": return "Staff Performance Ranking";
                case "frequency": return "Sales Frequency Report";
                case "customer": return "Customer Performance Ranking";
                default: return reportType;
            }
        }

        private string GetChartDescription(string reportType, string chartType)
        {
            string chartTypeName;
            switch (chartType)
            {
                case "bar":
                    chartTypeName = "Bar Chart";
                    break;
                case "line":
                    chartTypeName = "Line Chart";
                    break;
                case "pie":
                    chartTypeName = "Pie Chart";
                    break;
                case "doughnut":
                    chartTypeName = "Doughnut Chart";
                    break;
                default:
                    chartTypeName = "Chart";
                    break;
            }

            switch (reportType)
            {
                case "sales":
                    return $"This {chartTypeName} displays the top 10 products by sales volume, showing the most popular items based on quantity sold.";
                case "popular":
                    return $"This {chartTypeName} illustrates the 15 most popular products ranked by total units sold, highlighting best-performing inventory.";
                case "stock":
                    return $"This {chartTypeName} shows current inventory levels grouped by brand, providing visibility into stock distribution across product lines.";
                case "staff":
                    return $"This {chartTypeName} ranks staff members by total revenue generated, demonstrating individual sales performance and contribution.";
                case "frequency":
                    return $"This Line Chart displays monthly sales trends over time, revealing seasonal patterns and sales cycles.";
                case "customer":
                    return $"This {chartTypeName} identifies top customers by total spending, highlighting valuable client relationships and loyalty.";
                default:
                    return $"This {chartTypeName} provides visual analysis of the business data.";
            }
        }

        private string GenerateComprehensiveReport(ReportSaveModel model)
        {
            var serializer = new JavaScriptSerializer();
            dynamic reportData = serializer.Deserialize<object>(model.ReportData);

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"Bike Stores Business Report");
            sb.AppendLine($"============================");
            sb.AppendLine($"Report Type: {GetReportTypeDisplayName(model.ReportType)}");
            sb.AppendLine($"Chart Type: {model.ChartType.ToUpper()} Chart");
            sb.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"Description: {model.Description}");
            sb.AppendLine();
            sb.AppendLine("CHART ANALYSIS");
            sb.AppendLine("--------------");
            sb.AppendLine(GetChartDescription(model.ReportType, model.ChartType));
            sb.AppendLine();

            return sb.ToString();
        }

        public ActionResult Archive()
        {
            var reportFiles = new List<ArchiveFileModel>();
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

                    reportFiles.Add(new ArchiveFileModel
                    {
                        FileName = info.Name,
                        Description = description,
                        CreatedDate = info.CreationTime,
                        FileType = info.Extension.TrimStart('.'),
                        FileSize = info.Length
                    });
                }
            }

            return View("Archive", reportFiles);
        }

        public ActionResult DownloadFile(string fileName)
        {
            var filePath = Server.MapPath("~/App_Data/Reports/" + fileName);
            if (System.IO.File.Exists(filePath))
            {
                string contentType = "application/octet-stream";

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

                    TempData["SuccessMessage"] = "File deleted successfully!";
                }
                else
                {
                    TempData["ErrorMessage"] = "File not found";
                }

                return RedirectToAction("Archive");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error deleting file: {ex.Message}";
                return RedirectToAction("Archive");
            }
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