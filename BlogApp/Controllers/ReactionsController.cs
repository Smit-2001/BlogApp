using BlogApp.Data;
using BlogApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BlogApp.Controllers
{
    [Authorize]
    public class ReactionsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ReactionsController(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpPost]
        public async Task<IActionResult> Add(int blogPostId, string type)
        {
            if (!User.Identity.IsAuthenticated)
                return Unauthorized();

            var userId = _userManager.GetUserId(User);

            if (string.IsNullOrWhiteSpace(type))
                return BadRequest("Reaction type is required.");

            if (string.IsNullOrEmpty(userId))
                return BadRequest("User not found or not logged in.");

            try
            {
                var existingReaction = await _context.Reactions
                    .FirstOrDefaultAsync(r => r.BlogPostId == blogPostId && r.UserId == userId);

                if (existingReaction != null)
                {
                    existingReaction.Type = type;
                }
                else
                {
                    _context.Reactions.Add(new Reaction
                    {
                        BlogPostId = blogPostId,
                        UserId = userId,
                        Type = type
                    });
                }

                await _context.SaveChangesAsync();

                var counts = await _context.Reactions
                    .Where(r => r.BlogPostId == blogPostId)
                    .GroupBy(r => r.Type)
                    .Select(g => new { type = g.Key, count = g.Count() })
                    .ToListAsync();

                return Json(counts);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message, stackTrace = ex.StackTrace });
            }
        }
    }
}