using System.Collections.Generic;

namespace u22730797_HW03.Models
{
    public class HomeViewModel
    {
        public List<Staff> Staffs { get; set; }
        public List<Customer> Customers { get; set; }
        public List<Product> Products { get; set; }
        public List<Brand> Brands { get; set; }
        public List<Category> Categories { get; set; }

        // Pagination properties
        public int StaffPage { get; set; }
        public int CustomerPage { get; set; }
        public int StaffTotalPages { get; set; }
        public int CustomerTotalPages { get; set; }
        public int PageSize { get; set; } = 5; 
    }
}