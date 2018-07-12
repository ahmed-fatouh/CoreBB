using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;

namespace CoreBB.Web.Controllers
{
    public class ErrorController : Controller
    {
        public ViewResult Index()
        {
            var exception = HttpContext.Features.Get<IExceptionHandlerFeature>();
            ViewData["StatusCode"] = HttpContext.Response.StatusCode;
            ViewData["Message"] = exception.Error.Message;
            ViewData["StackTrace"] = exception.Error.StackTrace;
            ViewBag.Title = "Error Page";
            return View();
        }

        public ViewResult AccessDenied()
        {
            ViewBag.Title = "Access Denied";
            return View();
        }
    }
}