using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace CoreBB.Web.Models.ViewModels
{
    public class RegisterViewModel
    {
        [Required, DisplayName("Name")]
        public string Name { get; set; }
        [Required, DisplayName("Password")]
        [UIHint("password")]
        public string Password { get; set; }
        [Required, DisplayName("Repeat Password")]
        [UIHint("password")]
        public string RepeatPassword { get; set; }
        [Required, DisplayName("Self-Introduction")]
        public string Description { get; set; }
    }
}
