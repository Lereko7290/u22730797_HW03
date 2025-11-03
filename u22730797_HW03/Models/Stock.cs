using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace u22730797_HW03.Models
{
    [Table("stocks", Schema = "production")]
    public class Stock
    {
        [Key, Column(Order = 1)]
        public int store_id { get; set; }

        [Key, Column(Order = 2)]
        public int product_id { get; set; }

        public int quantity { get; set; }
    }
}