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

    /// <summary>
    /// Body for error status codes, re-executed by UseStatusCodePagesWithReExecute.
    /// The status is set explicitly so a direct visit to /error/404 is a 404 as well.
    /// </summary>
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Status(int code)
    {
        Response.StatusCode = code is >= 400 and <= 599 ? code : StatusCodes.Status404NotFound;
        return View("NotFound", _repo.All);
    }
}
