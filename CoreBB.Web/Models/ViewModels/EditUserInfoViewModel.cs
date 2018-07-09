using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace CoreBB.Web.Models.ViewModels
{
    public class EditUserInfoViewModel
    {
        public string CurrentName { get; set; }
        [Required, DisplayName("Name")]
        public string NewName { get; set; }
        [Required, DisplayName("Description")]
        public string Description { get; set; }
        [DisplayName("Administrator?")]
        public bool IsAdministrator { get; set; }
        [DisplayName("Locked?")]
        public bool IsLocked { get; set; }
    }
}
