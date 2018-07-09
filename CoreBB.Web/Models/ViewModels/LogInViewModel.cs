using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace CoreBB.Web.Models.ViewModels
{
    public class LogInViewModel
    {
        [Required]
        [DisplayName("Username")]
        public string Name { get; set; }
        [Required]
        [DisplayName("Password")]
        [UIHint("Password")]
        public string Password { get; set; }
    }
}
