using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.Entity;

namespace u22730797_HW03.Models
{
    public class BikeStoresContext : DbContext
    {
        public BikeStoresContext() : base("name=BikeStoresConnection")
        {
        }

        public DbSet<Staff> staffs { get; set; }
        public DbSet<Customer> customers { get; set; }
        public DbSet<Product> products { get; set; }
        public DbSet<Brand> brands { get; set; }
        public DbSet<Category> categories { get; set; }
        public DbSet<Order> orders { get; set; }
        public DbSet<OrderItem> order_items { get; set; }
        public DbSet<Store> stores { get; set; }
        public DbSet<Stock> stocks { get; set; }
    }
}