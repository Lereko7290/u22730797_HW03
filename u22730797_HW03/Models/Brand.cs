using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace u22730797_HW03.Models
{
    [Table("brands", Schema = "production")]
    public class Brand
    {
        [Key]
        public int brand_id { get; set; }
        public string brand_name { get; set; }
    }
}