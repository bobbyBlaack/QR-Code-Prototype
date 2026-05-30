using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace QR_Code_Prototype.Models
{
    public class PackagePassModel
    {

        [Key]
        public int PackageID { get; set; }

        public string PackagePass { get; set; } = string.Empty;


    }
}
