using AlSultaanMoving.Data;
using AlSultaanMoving.Models;
using Microsoft.AspNetCore.Mvc;

namespace AlSultaanMoving.Controllers;

public class ServicesController : Controller
{
    private readonly IContentRepository _repo;

    public ServicesController(IContentRepository repo) => _repo = repo;

    public IActionResult Index()
    {
        return View(_repo.All);
    }

    public IActionResult Details(string slug)
    {
        var service = _repo.GetService(slug);
        if (service == null) return NotFound();

        var vm = new ServiceDetailsViewModel
        {
            Service = service,
            Others = _repo.GetServices().Where(s => s.Slug != service.Slug).ToList(),
            ServiceTypes = _repo.GetServiceTypes().ToList(),
            Form = new ContactMessage { ServiceType = service.Title }
        };
        return View(vm);
    }
}
