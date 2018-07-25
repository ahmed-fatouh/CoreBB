using CoreBB.Web.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CoreBB.Web.Components
{
    public class ReplyViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke(Topic model) => View(model);
    }

    public class EditViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke(Topic model) => View(model);
    }

    public class DeleteViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke(Topic model) => View(model);
    }

    public class DeleteUserViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke(User model) => View(model);
    }

    public class DeleteForumViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke(Forum model) => View(model);
    }

    public class DeleteMessageViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke(Message model) => View(model);
    }
}
