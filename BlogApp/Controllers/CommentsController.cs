using BlogApp.Data;
using BlogApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace BlogApp.Controllers
{
    [Authorize]
    public class CommentsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public CommentsController(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpPost]
        [HttpPost]
        public async Task<IActionResult> Add(int blogPostId, string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return BadRequest("Comment cannot be empty.");

            var user = await _userManager.GetUserAsync(User);

            var comment = new Comment
            {
                BlogPostId = blogPostId,
                UserId = user.Id,
                Content = content,
                CreatedDate = DateTime.Now,
                User = user
            };

            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            return Json(new
            {
                content = comment.Content,
                createdDate = comment.CreatedDate.ToString("g"),
                user = user.FullName
            });
        }
    }
}
