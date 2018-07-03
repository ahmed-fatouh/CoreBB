using System;
using System.Collections.Generic;

namespace CoreBB.Web.Models
{
    public partial class Topic
    {
        public Topic()
        {
            InverseReplyToTopic = new HashSet<Topic>();
            InverseRootTopic = new HashSet<Topic>();
        }

        public int Id { get; set; }
        public int OwnerId { get; set; }
        public int ForumId { get; set; }
        public int? RootTopicId { get; set; }
        public int? ReplyToTopicId { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public DateTime PostDateTime { get; set; }
        public int? ModifiedByUserId { get; set; }
        public DateTime? ModifyDateTime { get; set; }
        public bool IsLocked { get; set; }

        public Forum Forum { get; set; }
        public User ModifiedByUser { get; set; }
        public User Owner { get; set; }
        public Topic ReplyToTopic { get; set; }
        public Topic RootTopic { get; set; }
        public ICollection<Topic> InverseReplyToTopic { get; set; }
        public ICollection<Topic> InverseRootTopic { get; set; }
    }
}
