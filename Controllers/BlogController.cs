using AlSultaanMoving.Data;
using AlSultaanMoving.Models;
using Microsoft.AspNetCore.Mvc;

namespace AlSultaanMoving.Controllers;

public class BlogController : Controller
{
    private const int PageSize = 9;
    private readonly IContentRepository _repo;

    public BlogController(IContentRepository repo) => _repo = repo;

    public IActionResult Index(int page = 1, string? q = null)
    {
        var (items, totalPages) = _repo.GetPostsPage(page, PageSize, q);
        var vm = new BlogIndexViewModel
        {
            Posts = items.ToList(),
            Page = Math.Clamp(page, 1, totalPages),
            TotalPages = totalPages,
            Query = q
        };
        return View(vm);
    }

    public IActionResult Post(string slug)
    {
        var post = _repo.GetPost(slug);
        if (post == null) return NotFound();

        var related = _repo.GetPosts()
            .Where(p => p.Slug != post.Slug)
            .Take(3)
            .ToList();

        return View(new BlogPostViewModel { Post = post, Related = related });
    }
}
