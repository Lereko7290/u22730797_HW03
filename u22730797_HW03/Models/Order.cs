using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace u22730797_HW03.Models
{
    [Table("orders", Schema = "sales")]
    public class Order
    {
        [Key]
        public int order_id { get; set; }
        public int customer_id { get; set; }
        public byte order_status { get; set; }
        public System.DateTime order_date { get; set; }
        public System.DateTime required_date { get; set; }
        public System.DateTime? shipped_date { get; set; }
        public int store_id { get; set; }
        public int staff_id { get; set; }
    }
}