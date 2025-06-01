using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace RazorPagesProject.Filters
{
    public class AuthenticationFilter : IPageFilter
    {
        public void OnPageHandlerExecuted(PageHandlerExecutedContext context)
        {
        }

        public void OnPageHandlerExecuting(PageHandlerExecutingContext context)
        {
            // Skip authentication for login and logout pages
            if (context.HandlerMethod?.Name == "OnGet" && 
                (context.ActionDescriptor.RelativePath == "/Login" || 
                 context.ActionDescriptor.RelativePath == "/Logout"))
            {
                return;
            }

            var sessionUsername = context.HttpContext.Session.GetString("username");
            var sessionToken = context.HttpContext.Session.GetString("token");
            var sessionId = context.HttpContext.Session.GetString("session_id");

            var cookieUsername = context.HttpContext.Request.Cookies["username"];
            var cookieToken = context.HttpContext.Request.Cookies["token"];
            var cookieSessionId = context.HttpContext.Request.Cookies["session_id"];

            if (string.IsNullOrEmpty(sessionUsername) || 
                string.IsNullOrEmpty(sessionToken) || 
                string.IsNullOrEmpty(sessionId) ||
                string.IsNullOrEmpty(cookieUsername) || 
                string.IsNullOrEmpty(cookieToken) || 
                string.IsNullOrEmpty(cookieSessionId) ||
                sessionUsername != cookieUsername || 
                sessionToken != cookieToken || 
                sessionId != cookieSessionId)
            {
                context.Result = new RedirectToPageResult("/Login");
            }
        }

        public void OnPageHandlerSelected(PageHandlerSelectedContext context)
        {
        }
    }
} 