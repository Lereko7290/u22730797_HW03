using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace u22730797_HW03.Models
{
    public class ProductViewModel
    {
        public int product_id { get; set; }
        public string product_name { get; set; }
        public string brand_name { get; set; }
        public string category_name { get; set; }
        public short model_year { get; set; }
        public decimal list_price { get; set; }
    }
}