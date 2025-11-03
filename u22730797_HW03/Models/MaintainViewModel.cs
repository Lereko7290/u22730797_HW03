using System.Collections.Generic;

namespace u22730797_HW03.Models
{
    public class MaintainViewModel
    {
        public List<Staff> Staffs { get; set; }
        public List<Customer> Customers { get; set; }
        public List<Product> Products { get; set; }

        // Pagination properties
        public int StaffPage { get; set; }
        public int CustomerPage { get; set; }
        public int ProductPage { get; set; }
        public int StaffTotalPages { get; set; }
        public int CustomerTotalPages { get; set; }
        public int ProductTotalPages { get; set; }
        public int PageSize { get; set; } = 10; // Show 10 items per page by default
    }
}