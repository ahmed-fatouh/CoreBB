using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using CoreBB.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace CoreBB.Web.Controllers
{
    [Authorize]
    public class ForumController : Controller
    {
        private CoreBBContext _dbContext;

        public ForumController(CoreBBContext dbContext)
        {
            _dbContext = dbContext;
        }

        [AllowAnonymous]
        public IActionResult Index()
        {
            IEnumerable<Forum> forums = _dbContext.Forum.Include("Owner").ToList();
            return View(forums);
        }

        [Authorize(Roles = Roles.Administrator)]
        [HttpGet]
        public ViewResult Create() => View();

        [HttpPost]
        public async Task<IActionResult> Create(Forum model)
        {
            User user = _dbContext.User.SingleOrDefault(u => u.Name == User.Identity.Name);
            if (!User.IsInRole(Roles.Administrator) || user.IsLocked)
                return Content("<p class='text-danger'>Access Denied!</p>");
            if (ModelState.IsValid)
            {
                Forum forum = _dbContext.Forum.SingleOrDefault(f => f.Name == model.Name);
                if (forum == null)
                {
                    model.CreateDateTime = DateTime.Now;
                    model.IsLocked = false;
                    model.OwnerId = user.Id;
                    await _dbContext.Forum.AddAsync(model);
                    await _dbContext.SaveChangesAsync();
                    return RedirectToAction("Index");
                }
                ModelState.AddModelError("", "The forum name already exists.");
            }
            return View(model);
        }

        public async Task<IActionResult> Detail (int forumId)
        {
            Forum forum = await _dbContext.Forum.Include("Owner").SingleOrDefaultAsync(f => f.Id == forumId);
            if (forum == null)
                return Content($"<p class='text-danger'>The requested Forum is not found!</p>");
            return View(forum);
        }

        [Authorize(Roles = Roles.Administrator)]
        [HttpGet]
        public async Task<IActionResult> Edit (int forumId)
        {
            Forum forum = await _dbContext.Forum.Include("Owner").SingleOrDefaultAsync(f => f.Id == forumId);
            if (forum == null)
                return Content($"<p class='text-danger'>The requested Forum is not found!</p>");
            if (forum.Owner.Name != User.Identity.Name)
                RedirectToAction("AccessDenied", "Error");
            return View(forum);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Forum model)
        {
            if (!User.IsInRole(Roles.Administrator))
                return Content("<p class='text-danger'>Access Denied!</p>");
            if (ModelState.IsValid)
            {
                Forum forum = await _dbContext.Forum.Include("Owner").SingleOrDefaultAsync(f => f.Id == model.Id);
                if (forum == null)
                    return Content($"<p class='text-danger'>The requested Forum is not found!</p>");
                if (forum.Owner.Name != User.Identity.Name)
                    return Content("<p class='text-danger'>Access Denied!</p>");
                Forum sameNameForum = await _dbContext.Forum.SingleOrDefaultAsync(f => f.Name == model.Name);
                if (sameNameForum == null || sameNameForum?.Id == forum.Id)
                {
                    forum.Name = model.Name;
                    forum.Description = model.Description;
                    forum.IsLocked = model.IsLocked;
                    _dbContext.Forum.Update(forum);
                    await _dbContext.SaveChangesAsync();
                    return RedirectToAction("Detail", new { forumId = model.Id });
                }
                else
                    ModelState.AddModelError("", "There exists a forum with the same name.");
            }
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Delete (int forumId)
        {
            Forum forum = await _dbContext.Forum.Include("Owner").SingleOrDefaultAsync(f => f.Id == forumId);
            if (forum == null)
                return Content($"<p class='text-danger'>Forum with ID: {forumId} not found!</p>");
            bool isTrueAdmin = User.IsInRole(Roles.Administrator) && User.Identity.Name == forum.Owner.Name;
            if (!isTrueAdmin)
                return Content("<p class='text-danger'>Access Denied!</p>");
            return ViewComponent("DeleteForum", new { model = forum });
        }

        [HttpPost]
        public async Task<IActionResult> Delete(Forum model)
        {
            Forum forum = await _dbContext.Forum.Include("Owner").SingleOrDefaultAsync(f => f.Id == model.Id);
            if (forum == null)
                return Content($"Forum with ID: {model.Id} not found!");
            bool isTrueAdmin = User.IsInRole(Roles.Administrator) && User.Identity.Name == forum.Owner.Name;
            if (!isTrueAdmin)
                return Content("Access Denied!");
            var TopicsInForum = _dbContext.Topic.Where(t => t.ForumId == forum.Id);
            if (await TopicsInForum.CountAsync() != 0)
            {
                _dbContext.Topic.RemoveRange(TopicsInForum);
                await _dbContext.SaveChangesAsync();
            }
            _dbContext.Forum.Remove(forum);
            await _dbContext.SaveChangesAsync();
            return RedirectToAction("Index");
        }
    }
}