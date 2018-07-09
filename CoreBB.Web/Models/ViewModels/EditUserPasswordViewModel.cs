using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace CoreBB.Web.Models.ViewModels
{
    public class EditUserPasswordViewModel
    {
        public string Name { get; set; }
        [Required, DisplayName("Current Password")]
        [UIHint("Password")]
        public string CurrentPassword { get; set; }
        [Required, DisplayName("New Password")]
        [UIHint("Password")]
        public string NewPassword { get; set; }
        [Required, DisplayName("Repeat New Password")]
        [UIHint("Password")]
        public string RepeatNewPassword { get; set; }
        public bool PasswordChanged { get; set; }
    }
}
