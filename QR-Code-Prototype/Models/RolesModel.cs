using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace QR_Code_Prototype.Models
{
    public class RolesModel
    {

        [Key]
        public int RoleID { get; set; }

        public string RoleName { get; set; } = string.Empty;


    }
}
