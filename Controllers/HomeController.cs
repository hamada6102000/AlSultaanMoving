using System.Diagnostics;
using AlSultaanMoving.Data;
using AlSultaanMoving.Models;
using Microsoft.AspNetCore.Mvc;

namespace AlSultaanMoving.Controllers;

public class HomeController : Controller
{
    private readonly IContentRepository _repo;

    public HomeController(IContentRepository repo) => _repo = repo;

    public IActionResult Index()
    {
        var vm = new HomeViewModel
        {
            Content = _repo.All,
            LatestPosts = _repo.GetLatestPosts(3).ToList(),
            Form = new ContactMessage()
        };
        return View(vm);
    }

    public IActionResult About()
    {
        return View(_repo.All);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
