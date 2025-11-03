using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace u22730797_HW03.Models
{
    [Table("order_items", Schema = "sales")]
    public class OrderItem
    {
        [Key, Column(Order = 1)]
        public int order_id { get; set; }

        [Key, Column(Order = 2)]
        public int item_id { get; set; }

        public int product_id { get; set; }
        public int quantity { get; set; }
        public decimal list_price { get; set; }
        public decimal discount { get; set; }
    }
}