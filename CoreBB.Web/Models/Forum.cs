using System;
using System.Collections.Generic;

namespace CoreBB.Web.Models
{
    public partial class Forum
    {
        public Forum()
        {
            Topic = new HashSet<Topic>();
        }

        public int Id { get; set; }
        public int OwnerId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime CreateDateTime { get; set; }
        public bool IsLocked { get; set; }

        public User Owner { get; set; }
        public ICollection<Topic> Topic { get; set; }
    }
}
