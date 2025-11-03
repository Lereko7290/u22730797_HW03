using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace u22730797_HW03.Models
{
    [Table("staffs", Schema = "sales")]
    public class Staff
    {
        [Key]
        public int staff_id { get; set; }
        public string first_name { get; set; }
        public string last_name { get; set; }
        public string email { get; set; }
        public string phone { get; set; }
        public byte active { get; set; }
        public int store_id { get; set; }
        public int? manager_id { get; set; }
    }
}