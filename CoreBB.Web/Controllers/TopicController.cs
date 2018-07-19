using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using CoreBB.Web.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace CoreBB.Web.Controllers
{
    [Authorize]
    public class TopicController : Controller
    {
        private CoreBBContext _dbContext;

        public TopicController(CoreBBContext dbContext)
        {
            _dbContext = dbContext;
        }

        [AllowAnonymous]
        public async Task<IActionResult> Index(int forumId)
        {
            Forum forum = await _dbContext.Forum.SingleOrDefaultAsync(f => f.Id == forumId);
            if (forum == null)
                return RedirectToAction("AccessDenied", "Error");
            IEnumerable<Topic> topics = _dbContext.Topic.Include("Owner")
                                        .Where(t => t.ForumId == forumId && t.Id == t.RootTopicId).ToList();
            ViewBag.ForumId = forum.Id;
            ViewBag.ForumName = _dbContext.Forum.SingleOrDefault(f => f.Id == forumId).Name;
            return View(topics);
        }

        [HttpGet]
        public ViewResult Create(int forumId)
        {
            ViewBag.ForumId = forumId;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(Topic model, int forumId)
        {
            if (ModelState.IsValid)
            {
                User user = await _dbContext.User.SingleOrDefaultAsync(u => u.Name == User.Identity.Name);
                Forum forum = await _dbContext.Forum.SingleOrDefaultAsync(f => f.Id == forumId);
                if (user != null && forum !=null && !user.IsLocked && !forum.IsLocked)
                {
                    model.ForumId = forumId;
                    model.OwnerId = user.Id;
                    model.PostDateTime = DateTime.Now;
                    model.IsLocked = false;
                    await _dbContext.Topic.AddAsync(model);
                    await _dbContext.SaveChangesAsync();
                    model.RootTopicId = model.Id;
                    _dbContext.Topic.Update(model);
                    await _dbContext.SaveChangesAsync();
                    return RedirectToAction("Read", new { rootTopicId = model.Id });
                }
                return Content("Access Denied!");
            }
            return View(model);
        }

        [AllowAnonymous]
        public async Task<IActionResult> Read (int rootTopicId)
        {
            Topic rootTopic = await _dbContext.Topic.SingleOrDefaultAsync(t => t.Id == rootTopicId);
            if (rootTopic == null)
                return RedirectToAction("AccessDenied", "Error");
            IEnumerable<Topic> topics = await _dbContext.Topic.Include("Owner").Include("ModifiedByUser")
                                              .Where(t => t.RootTopicId == rootTopicId || t.Id == rootTopicId)
                                              .OrderBy(t => t.PostDateTime).ToListAsync();
            return View(topics);
        }

        [HttpPost]
        public async Task<IActionResult> Reply (Topic model)
        {
            User user = await _dbContext.User.SingleOrDefaultAsync(u => u.Name == User.Identity.Name);
            if (user == null || user.IsLocked)
                return Content("Access Denied");
            Topic rootTopic = await _dbContext.Topic.Include("Forum").SingleOrDefaultAsync(t => t.Id == model.RootTopicId);
            Topic repliedToTopic = await _dbContext.Topic.SingleOrDefaultAsync(t => t.Id == model.ReplyToTopicId);
            if (rootTopic == null || rootTopic.IsLocked || rootTopic.Forum.IsLocked || 
                user.IsLocked || rootTopic.Id != repliedToTopic.RootTopicId ||
                rootTopic.ForumId != model.ForumId)
                return RedirectToAction("AccessDenied", "Error");
            if (model.Content == null || model.Content.Length == 0)
               return Content("No content to submit");
            model.Title = "";
            model.PostDateTime = DateTime.Now;
            model.OwnerId = user.Id;
            model.IsLocked = false;
            model.ModifiedByUserId = null;
            model.ModifyDateTime = null;
            await _dbContext.AddAsync(model);
            await _dbContext.SaveChangesAsync();
            return RedirectToAction("Read", new { rootTopicId = model.RootTopicId });
        }

        [HttpGet]
        public async Task<ViewComponentResult> GetActionForm(int topicId, string formType)
        {
            Topic topic = await _dbContext.Topic.Include("Owner").SingleOrDefaultAsync(t => t.Id == topicId);
            if (topic == null)
                throw new Exception("Topic Not Found.");
            if (!(topic.Owner?.Name == User.Identity.Name || User.IsInRole(Roles.Administrator)))
                throw new Exception("You aren't permitted to Edit");
            if (formType.ToLower() == "edit")
                return ViewComponent("Edit", topic);
            else if (formType.ToLower() == "delete")
                return ViewComponent("Delete", topic);
            else
                throw new Exception("You specified a wrong form type.");
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Topic model)
        {
            Topic topic = await _dbContext.Topic.Include("Owner").Include("ModifiedByUser").Include("Forum").SingleOrDefaultAsync(t => t.Id == model.Id);
            if (topic == null || topic.RootTopicId != model.RootTopicId || 
                topic.ForumId != model.ForumId || topic.ReplyToTopicId != model.ReplyToTopicId)
                return Content("The topic isn't valid");
            Topic rootTopic = await _dbContext.Topic.SingleOrDefaultAsync(t => t.Id == topic.RootTopicId);
            User user = await _dbContext.User.SingleOrDefaultAsync(u => u.Name == User.Identity.Name);
            if ((user == null || user.Name != topic.Owner?.Name && !User.IsInRole(Roles.Administrator)))
                return RedirectToAction("AccessDenied", "Error");
            if ((rootTopic.IsLocked || topic.Forum.IsLocked || user.IsLocked) && !User.IsInRole(Roles.Administrator))
                return RedirectToAction("AccessDenied", "Error");
            bool isRootTopic = topic.RootTopicId == topic.Id;
            if (model.Content == null || (model.Title == null & isRootTopic))
                return Content("Content " + (isRootTopic ? "and Title are" : "is") + " required.");
            
            topic.Content = model.Content;
            topic.Title = isRootTopic ? model.Title : "";
            topic.IsLocked = User.IsInRole(Roles.Administrator) ? model.IsLocked : false;
            if (DateTime.Now.Subtract(topic.ModifyDateTime ?? topic.PostDateTime).TotalMinutes > 5 ||
                User.Identity.Name != (topic.ModifiedByUser?.Name ?? topic.Owner?.Name))
            {
                topic.ModifiedByUserId = user.Id;
                topic.ModifyDateTime = DateTime.Now;
            }
            _dbContext.Topic.Update(topic);
            await _dbContext.SaveChangesAsync();
            return RedirectToAction("Read", new { rootTopicId = topic.RootTopicId });
        }

        [HttpPost]
        public async Task<IActionResult> Delete(Topic model)
        {
            Topic topic = await _dbContext.Topic.Include("Owner").Include("ModifiedByUser").Include("Forum")
                          .SingleOrDefaultAsync(t => t.Id == model.Id);
            if (topic == null || topic.RootTopicId != model.RootTopicId ||
                topic.ForumId != model.ForumId || topic.ReplyToTopicId != model.ReplyToTopicId)
                return Content("The topic isn't valid");
            Topic rootTopic = await _dbContext.Topic.SingleOrDefaultAsync(t => t.Id == topic.RootTopicId);
            User user = await _dbContext.User.SingleOrDefaultAsync(u => u.Name == User.Identity.Name);
            if ((user == null || user.Name != topic.Owner?.Name && !User.IsInRole(Roles.Administrator)))
                return RedirectToAction("AccessDenied", "Error");
            if ((rootTopic.IsLocked || topic.Forum.IsLocked || user.IsLocked) && !User.IsInRole(Roles.Administrator))
                return RedirectToAction("AccessDenied", "Error");
            bool isRootTopic = topic.RootTopicId == topic.Id;

            if (isRootTopic)
            {
                var topicsToDelete = _dbContext.Topic.Where(t => t.RootTopicId == topic.Id);
                _dbContext.Topic.RemoveRange(topicsToDelete);
                await _dbContext.SaveChangesAsync();
                return RedirectToAction("Index", new { forumId = topic.ForumId });
            }
            else
            {
                var topicsRepliedToDeleted = _dbContext.Topic.Where(t => t.ReplyToTopicId == topic.Id);
                if (topicsRepliedToDeleted != null)
                {
                    await topicsRepliedToDeleted.ForEachAsync(t => t.ReplyToTopicId = null);
                    _dbContext.Topic.UpdateRange(topicsRepliedToDeleted);
                    await _dbContext.SaveChangesAsync();
                }
                _dbContext.Topic.Remove(topic);
                await _dbContext.SaveChangesAsync();
                return RedirectToAction("Read", new { rootTopicId = topic.RootTopicId });
            }
        }

        private string GetTopicAsString(Topic topic)
        {
            string topicString = "";
            topicString += "Id: " + topic.Id + ", ";
            topicString += "ForumId: " + topic.ForumId + ", ";
            topicString += "OwnerId: " + topic.OwnerId + ", ";
            topicString += "RootTopicId: " + topic.RootTopicId + ", ";
            topicString += "ReplyToTopicId: " + topic.ReplyToTopicId + ", ";
            topicString += "ModifiedByUserId: " + topic.ModifiedByUserId + ", ";
            topicString += "PostDateTime: " + topic.PostDateTime + ", ";
            topicString += "ModifyDateTime: " + topic.ModifyDateTime + ", ";
            topicString += "Title: " + topic.Title + ", ";
            topicString += "Content: " + topic.Content;
            return topicString;
        }
    }
}