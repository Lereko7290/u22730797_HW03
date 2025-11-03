using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace u22730797_HW03.Models
{
    [Table("stores", Schema = "sales")]
    public class Store
    {
        [Key]
        public int store_id { get; set; }
        public string store_name { get; set; }
        public string phone { get; set; }
        public string email { get; set; }
        public string street { get; set; }
        public string city { get; set; }
        public string state { get; set; }
        public string zip_code { get; set; }
    }
}