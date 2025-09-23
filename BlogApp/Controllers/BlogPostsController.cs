using BlogApp.Data;
using BlogApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace BlogApp.Controllers
{
    [Authorize]
    public class BlogPostsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;

        public BlogPostsController(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        [AllowAnonymous]
        public async Task<IActionResult> Index(
            string searchString,
            string sortOrder,
            int? categoryId,
            int pageNumber = 1,
            int pageSize = 3)
        {
            var blogPosts = _context.BlogPosts
                .Include(b => b.Category)
                .Include(b => b.Comments)
                .Include(b => b.Reactions)
                .AsQueryable();


            if (!string.IsNullOrEmpty(searchString))
            {
                blogPosts = blogPosts.Where(b =>
                    b.Title.Contains(searchString) ||
                    b.Author.Contains(searchString) ||
                    (b.Category != null && b.Category.Name.Contains(searchString)));
            }

            if (categoryId.HasValue && categoryId.Value > 0)
            {
                blogPosts = blogPosts.Where(b => b.CategoryId == categoryId.Value);
            }

            blogPosts = sortOrder switch
            {
                "createdDate_asc" => blogPosts.OrderBy(b => b.CreatedDate),
                "createdDate_desc" => blogPosts.OrderByDescending(b => b.CreatedDate),
                "title_asc" => blogPosts.OrderBy(b => b.Title),
                "title_desc" => blogPosts.OrderByDescending(b => b.Title),
                "author_asc" => blogPosts.OrderBy(b => b.Author),
                "author_desc" => blogPosts.OrderByDescending(b => b.Author),
                "category_asc" => blogPosts.OrderBy(b => b.Category.Name),
                "category_desc" => blogPosts.OrderByDescending(b => b.Category.Name),
                _ => blogPosts.OrderByDescending(b => b.CreatedDate) // Default sort
            };

            int totalPosts = await blogPosts.CountAsync();
            int totalPages = (int)Math.Ceiling(totalPosts / (double)pageSize);

            var posts = await blogPosts
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Categories = new SelectList(_context.Categories, "Id", "Name", categoryId);

            ViewBag.SortOrder = sortOrder;
            ViewBag.SearchString = searchString;
            ViewBag.CategoryId = categoryId;
            ViewBag.CurrentPage = pageNumber;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalPages = totalPages;

            return View(posts);
        }

       
        [AllowAnonymous]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var blogPost = await _context.BlogPosts
                .Include(b => b.Reactions)
                .Include(b => b.Comments)
                    .ThenInclude(c => c.User)  
                    .Include(b => b.Category)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (blogPost == null) return NotFound();

            return View(blogPost);
        }

      

        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            ViewBag.Categories = new SelectList(_context.Categories, "Id", "Name");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BlogPost blogPost, IFormFile imageFile)
        {
            if (imageFile == null || imageFile.Length == 0)
            {
                ModelState.AddModelError("ImagePath", "Image is required.");
            }
            if (ModelState.IsValid)
            {
                if (imageFile != null && imageFile.Length > 0)
                {
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
                    var extension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();

                    if (!allowedExtensions.Contains(extension))
                    {
                        ModelState.AddModelError("ImagePath", "Only .jpg, .jpeg, and .png files are allowed.");

                        ViewBag.Categories = new SelectList(_context.Categories, "Id", "Name", blogPost.CategoryId);
                        return View(blogPost);
                    }

                    var uploadsDir = Path.Combine(_env.WebRootPath, "images");
                    if (!Directory.Exists(uploadsDir))
                        Directory.CreateDirectory(uploadsDir);

                    var fileName = Path.GetFileName(imageFile.FileName);
                    var filePath = Path.Combine(uploadsDir, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await imageFile.CopyToAsync(stream);
                    }

                    blogPost.ImagePath = "/images/" + fileName;
                }

                blogPost.CreatedDate = DateTime.Now;
                _context.Add(blogPost);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Categories = new SelectList(_context.Categories, "Id", "Name", blogPost.CategoryId);
            return View(blogPost);
        }


   

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var blogPost = await _context.BlogPosts.FindAsync(id);
            if (blogPost == null) return NotFound();

            ViewBag.Categories = new SelectList(_context.Categories, "Id", "Name", blogPost.CategoryId);
            return View(blogPost);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, BlogPost blogPost, IFormFile? imageFile)
        {
            if (id != blogPost.Id)
                return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    var existingPost = await _context.BlogPosts.FindAsync(id);
                    if (existingPost == null)
                        return NotFound();

                    existingPost.Title = blogPost.Title;
                    existingPost.Author = blogPost.Author;
                    existingPost.Content = blogPost.Content;
                    existingPost.CategoryId = blogPost.CategoryId;
                    existingPost.UpdatedDate = DateTime.Now;
                    if (imageFile != null && imageFile.Length > 0)
                    {
                        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
                        var extension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();

                        if (!allowedExtensions.Contains(extension))
                        {
                            ModelState.AddModelError("ImagePath", "Only .jpg, .jpeg, and .png files are allowed.");
                            ViewBag.Categories = new SelectList(_context.Categories, "Id", "Name", blogPost.CategoryId);
                            return View(blogPost);
                        }

                        var uploadsDir = Path.Combine(_env.WebRootPath, "images");
                        if (!Directory.Exists(uploadsDir))
                            Directory.CreateDirectory(uploadsDir);

                        var fileName = Path.GetFileName(imageFile.FileName);
                        var filePath = Path.Combine(uploadsDir, fileName);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await imageFile.CopyToAsync(stream);
                        }

                        existingPost.ImagePath = "/images/" + fileName;
                    }

                    _context.Update(existingPost);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.BlogPosts.Any(e => e.Id == blogPost.Id))
                        return NotFound();
                    else
                        throw;
                }
            }

            ViewBag.Categories = new SelectList(_context.Categories, "Id", "Name", blogPost.CategoryId);
            return View(blogPost);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var blogPost = await _context.BlogPosts.Include(b => b.Category).FirstOrDefaultAsync(m => m.Id == id);
            if (blogPost == null) return NotFound();

            return View(blogPost);
        }

        [HttpPost, ActionName("DeleteConfirmed")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var blogPost = await _context.BlogPosts.FindAsync(id);
            if (blogPost != null)
            {
                if (!string.IsNullOrEmpty(blogPost.ImagePath))
                {
                    var imageFullPath = Path.Combine(_env.WebRootPath, blogPost.ImagePath.TrimStart('/'));
                    if (System.IO.File.Exists(imageFullPath))
                        System.IO.File.Delete(imageFullPath);
                }

                _context.BlogPosts.Remove(blogPost);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool BlogPostExists(int id)
        {
            return _context.BlogPosts.Any(e => e.Id == id);
        }
    }
}
