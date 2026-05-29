using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace QR_Code_Prototype.Models
{
    public class UserModel
    {
        [Key]
        public int UserID { get; set; }

        public string UserName { get; set; }

        public string Email { get; set; }

        public string ContactNumber { get; set; }

    }
}
