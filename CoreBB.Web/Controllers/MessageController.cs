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
    public class MessageController : Controller
    {
        private CoreBBContext _dbContext; 

        public MessageController(CoreBBContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<IActionResult> Index()
        {
            User user = await _dbContext.User.SingleOrDefaultAsync(u => u.Name == User.Identity.Name);
            var lastMessagesGroups = _dbContext.Message.Include("FromUser").Include("ToUser")
                                   .Where(m => m.FromUserId == user.Id || m.ToUserId == user.Id)
                                   .Select(m => new { M = m, FromToUserId = m.ToUserId == user.Id ? m.FromUserId : m.ToUserId })
                                   .GroupBy(n => n.FromToUserId)
                                   .Select(g => g.OrderBy(n => n.M.SendDateTime).Last());

            var messages = new List<Message>();
            foreach (var lmg in lastMessagesGroups)
            {
                messages.Add(lmg.M);
            }
            messages = messages.OrderByDescending(m => m.SendDateTime).ToList();

            return View(messages);
        }

        public async Task<IActionResult> Detail(int user1, int user2)
        {
            User firstUser = await _dbContext.User.SingleOrDefaultAsync(u => u.Id == user1);
            User secondUser = await _dbContext.User.SingleOrDefaultAsync(u => u.Id == user2);
            if (firstUser?.Name != User.Identity.Name && secondUser?.Name != User.Identity.Name)
                return RedirectToAction("AccessDenied", "Error");
            if (firstUser == null || secondUser == null)
                return Content("Error Finding Users");

            var messages = _dbContext.Message.Where(m => (m.FromUserId == user1 && m.ToUserId == user2) ||
                            (m.FromUserId == user2 && m.ToUserId == user1))
                           .OrderBy(m => m.SendDateTime);
            //isRead
            await messages.ForEachAsync(m => m.IsRead = m.ToUser.Name == User.Identity.Name ? true : m.IsRead);
            _dbContext.Message.UpdateRange(messages);
            await _dbContext.SaveChangesAsync();
            ViewBag.SendToUserId = (firstUser.Name == User.Identity.Name) ? secondUser.Id : firstUser.Id;
            return View(messages);
        }

        [HttpPost]
        public async Task<IActionResult> Send(Message model)
        {
            User sentFromUser = await _dbContext.User.SingleOrDefaultAsync(u => u.Id == model.FromUserId);
            if (sentFromUser == null || User.Identity.Name != sentFromUser.Name || sentFromUser.IsLocked)
                return Content("Access Denied");
            User sentToUser = await _dbContext.User.SingleOrDefaultAsync(u => u.Id == model.ToUserId);
            if (sentToUser == null)
                return Content($"User with id: {model.ToUserId} is not found.");
            if (sentToUser.Id == model.FromUserId)
                return Content("You can't send a self message.");
            if (!ModelState.IsValid)
                return RedirectToAction("Detail", new { user1 = sentFromUser.Id, user2 = sentToUser.Id });

            model.Title = "";
            model.SendDateTime = DateTime.Now;
            model.IsRead = false;
            await _dbContext.AddAsync(model);
            await _dbContext.SaveChangesAsync();
            return RedirectToAction("Detail", new { user1 = model.FromUserId, user2 = model.ToUserId });
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int messageId)
        {
            Message message = await _dbContext.Message.Include("FromUser")
                              .SingleOrDefaultAsync(m => m.Id == messageId);
            return ViewComponent("DeleteMessage", message);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(Message model)
        {
            Message message = await _dbContext.Message.Include("FromUser")
                              .SingleOrDefaultAsync(m => m.Id == model.Id);
            if (message == null || message.FromUser.Name != User.Identity.Name)
                return Content("Access Denied");
            if (model.FromUserId != message.FromUserId || model.ToUserId != message.ToUserId)
                return Content("Bad Message");
            _dbContext.Message.Remove(message);
            await _dbContext.SaveChangesAsync();
            return RedirectToAction("Detail", new { user1 = model.FromUserId, user2 = model.ToUserId });
        }
    }
}