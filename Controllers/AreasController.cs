using AlSultaanMoving.Data;
using Microsoft.AspNetCore.Mvc;

namespace AlSultaanMoving.Controllers;

public class AreasController : Controller
{
    private readonly IContentRepository _repo;

    public AreasController(IContentRepository repo) => _repo = repo;

    public IActionResult Index()
    {
        return View(_repo.All);
    }
}
