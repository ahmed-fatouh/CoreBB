using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using CoreBB.Web.Models;
using CoreBB.Web.Models.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace CoreBB.Web.Controllers
{
    [Authorize]
    public class UserController : Controller
    {
        private CoreBBContext _dbContext;
        
        public UserController(CoreBBContext dbContext)
        {
            _dbContext = dbContext;
        }

        [AllowAnonymous]
        public async Task<ViewResult> Register()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return View();
        }

        [AllowAnonymous, HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            User user = new Models.User();
            if (ModelState.IsValid)
            {
                model.Name = model.Name.Trim();

                user = _dbContext.User.FirstOrDefault(u => u.Name.Equals(model.Name, StringComparison.CurrentCultureIgnoreCase));
                if (user == null)
                {
                    if (model.Password == model.RepeatPassword)
                    {
                        user = new Models.User
                        {
                            Name = model.Name,
                            Description = model.Description,
                            IsAdministrator = _dbContext.User.Count() == 0 ? true : false,
                            RegisterDateTime = DateTime.Now,
                            IsLocked = false
                        };
                        PasswordHasher<User> hasher = new PasswordHasher<User>();
                        user.PasswordHash = hasher.HashPassword(user, model.Password);
                        await _dbContext.User.AddAsync(user);
                        await _dbContext.SaveChangesAsync();
                    }
                    else
                    {
                        ModelState.AddModelError("", "The repeated password must be identical.");
                    }
                }
                else
                {
                    ModelState.AddModelError("","There exists a user with the same name");
                }
            }
            if (ModelState.IsValid)
            {
                await LoginUserAsync(user);
                return RedirectToAction("Index", "Home");
            }
            else
                return View(model);
        }

        private async Task LoginUserAsync(User user)
        {
            List<Claim> claims = new List<Claim> ();
            claims.Add(new Claim(ClaimTypes.Name, user.Name));
            if (user.IsAdministrator)
                claims.Add(new Claim(ClaimTypes.Role, Roles.Administrator));
            ClaimsIdentity claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            ClaimsPrincipal claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, claimsPrincipal);
            user.LastLoginDateTime = DateTime.Now;
            await _dbContext.SaveChangesAsync();
        }

        [AllowAnonymous]
        public async Task<ViewResult> LogIn()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> LogIn(LogInViewModel model)
        {
            Models.User user = new Models.User() ;
            if (ModelState.IsValid)
            {
                user = _dbContext.User
                       .SingleOrDefault(u => u.Name.Equals(model.Name, StringComparison.CurrentCultureIgnoreCase));
                
                if (user != null)
                {
                    PasswordHasher<User> passwordHasher = new PasswordHasher<Models.User>();
                    PasswordVerificationResult verificationResult = passwordHasher.VerifyHashedPassword(user, user.PasswordHash,model.Password);
                    if (verificationResult == PasswordVerificationResult.Success)
                    {
                        await LoginUserAsync(user);
                        return RedirectToAction("Index", "Home");
                    }  
                }
                ModelState.AddModelError("", "Username or Password incorrect");
            }
            model.Password = null;
            return View(model);
        }

        public async Task<RedirectToActionResult> LogOut()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        public IActionResult Detail(string name)
        {
            User user = _dbContext.User.SingleOrDefault(u => u.Name == name);
            if (user == null)
                return Content($"User '{name}' not found!");
            return View(user);   
        }

        public IActionResult Edit(string name)
        {
            if (!User.IsInRole(Roles.Administrator) && User.Identity.Name != name)
                return Content("Access Denied!");
            User user = _dbContext.User.SingleOrDefault(u => u.Name == name);
            if (user == null)
                return Content($"User '{name}' not found!");
            var model = new EditUserInfoViewModel
            {
                CurrentName = user.Name,
                NewName = user.Name,
                Description = user.Description,
                IsAdministrator = user.IsAdministrator,
                IsLocked = user.IsLocked
            };
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(EditUserInfoViewModel model)
        {
            if (!User.IsInRole(Roles.Administrator) && User.Identity.Name != model.CurrentName)
                return Content("Access Denied!");
            User user = new Models.User();
            if (ModelState.IsValid)
            {
                if (model.NewName != model.CurrentName)
                {
                    user = _dbContext.User.SingleOrDefault(u => u.Name == model.NewName);
                    if (user != null)
                    {
                        ModelState.AddModelError("", "The new name chosen already exists");
                        return View("Edit", model);
                    }
                        
                }
                user = _dbContext.User.SingleOrDefault(u => u.Name == model.CurrentName);
                if (user == null)
                    return Content($"User '{model.CurrentName}' not found!");
                user.Name = model.NewName;
                user.Description = model.Description;
                user.IsAdministrator = model.IsAdministrator;
                user.IsLocked = model.IsLocked;
                _dbContext.User.Update(user);
                await _dbContext.SaveChangesAsync();
                if (model.NewName != model.CurrentName)
                    await LogOut();
                return RedirectToAction("Detail", new { name = user.Name });
            }
            return View("Edit", model);
        }

        public IActionResult ChangePassword (string name)
        {
            if (!User.IsInRole(Roles.Administrator) && User.Identity.Name != name)
                return Content("Access Denied!");
            User user = _dbContext.User.SingleOrDefault(u => u.Name == name);
            if (user == null)
                return Content($"User '{name}' not found!");
            EditUserPasswordViewModel model = new EditUserPasswordViewModel
            {
                Name = user.Name,
                PasswordChanged = false
            };
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ChangePassword(EditUserPasswordViewModel model)
        {
            if (!User.IsInRole(Roles.Administrator) && User.Identity.Name != model.Name)
                return Content("Access Denied!");
            if (ModelState.IsValid)
            {
                User user = _dbContext.User.SingleOrDefault(u => u.Name == model.Name);
                if (user == null)
                    return Content($"User '{model.Name}' not found!");
                PasswordHasher<User> passwordHasher = new PasswordHasher<Models.User> ();
                PasswordVerificationResult result = passwordHasher.VerifyHashedPassword(user, user.PasswordHash,model.CurrentPassword);
                if (result == PasswordVerificationResult.Success || User.IsInRole(Roles.Administrator))
                {
                    if (model.NewPassword == model.RepeatNewPassword)
                    {
                        string newPasswordHash = passwordHasher.HashPassword(user, model.NewPassword);
                        user.PasswordHash = newPasswordHash;
                        _dbContext.User.Update(user);
                        await _dbContext.SaveChangesAsync();
                        model.PasswordChanged = true;
                        return View(model);
                    }
                    ModelState.AddModelError("", "The repeated password must be the same as new password.");
                }
                else
                    ModelState.AddModelError("", "The current password is incorrect");
            }
            return View(model);
        }

        [Authorize(Roles = Roles.Administrator)]
        public ViewResult Index()
        {
            IEnumerable<User> users = _dbContext.User.ToList();
            return View(users);
        }

        [HttpGet]
        public IActionResult DeleteForm (string name)
        {
            if (User.Identity.Name != name && !User.IsInRole(Roles.Administrator))
                return Content("<p class='text-danger'>Access Denied!</p>");
            User user = _dbContext.User.SingleOrDefault(u => u.Name == name);
            if (user == null)
                return Content($"<p class='text-danger'>User '{name}' not found!</p>");
            return ViewComponent("DeleteUser", new { model = user });
        }

        [HttpPost]
        public async Task<IActionResult> Delete (string name)
        {
            if (User.Identity.Name != name && !User.IsInRole(Roles.Administrator))
                return Content("Access Denied!");
            User user = await _dbContext.User.SingleOrDefaultAsync(u => u.Name == name);
            if (user == null)
                return Content($"User '{name}' not found!");
            if (user.IsAdministrator)
            {
                var admins = _dbContext.User.Where(u => u.IsAdministrator);
                if (await admins.CountAsync() == 1)
                    return Content("You are the only Administrator. \nCreate another admin to delete the current.");
                var adminForums = _dbContext.Forum.Where(f => f.OwnerId == user.Id);
                if (await adminForums.CountAsync() > 0)
                {
                    User oldestAdmin = admins.Where(a => a.Name != user.Name).OrderBy(a => a.RegisterDateTime).First();
                    await adminForums.ForEachAsync(f => f.OwnerId = oldestAdmin.Id);
                    _dbContext.Forum.UpdateRange(adminForums);
                }
            }
            var userTopics = _dbContext.Topic.Where(t => t.OwnerId == user.Id);
            await userTopics.ForEachAsync(t => t.OwnerId = null);
            _dbContext.Topic.UpdateRange(userTopics);
            await _dbContext.SaveChangesAsync();
            var topicsModifiedByUser = _dbContext.Topic.Where(t => t.ModifiedByUserId == user.Id);
            await topicsModifiedByUser.ForEachAsync(t => t.ModifiedByUserId = null);
            _dbContext.Topic.UpdateRange(userTopics);
            await _dbContext.SaveChangesAsync();
            var userReceivedMessages = _dbContext.Message.Where(t => t.ToUserId == user.Id);
            //await userReceivedMessages.ForEachAsync(t => t.ToUserId = -1);
            _dbContext.Message.RemoveRange(userReceivedMessages);
            await _dbContext.SaveChangesAsync();
            var userSentMessages = _dbContext.Message.Where(t => t.FromUserId == user.Id);
            //await userSentMessages.ForEachAsync(t => t.FromUserId = -1);
            _dbContext.Message.RemoveRange(userSentMessages);
            await _dbContext.SaveChangesAsync();
            _dbContext.User.Remove(user);
            await _dbContext.SaveChangesAsync();
            if (User.Identity.Name == name)
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }
    }
}