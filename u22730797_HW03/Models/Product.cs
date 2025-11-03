using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace u22730797_HW03.Models
{
    [Table("products", Schema = "production")]
    public class Product
    {
        [Key]
        public int product_id { get; set; }
        public string product_name { get; set; }
        public int brand_id { get; set; }
        public int category_id { get; set; }
        public short model_year { get; set; }
        public decimal list_price { get; set; }

        [ForeignKey("brand_id")]
        public virtual Brand Brand { get; set; }

        [ForeignKey("category_id")]
        public virtual Category Category { get; set; }
    }
}