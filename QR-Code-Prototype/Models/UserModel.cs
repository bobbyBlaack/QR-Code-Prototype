using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace QR_Code_Prototype.Models
{
    public class UserModel
    {
        [Key]
        public int UserID { get; set; }

        public string UserName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string ContactNumber { get; set; } = string.Empty;

    }
}
