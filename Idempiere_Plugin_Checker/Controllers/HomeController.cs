using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Idempiere_Plugin_Checker.DB;
using Idempiere_Plugin_Checker.Models;
using Idempiere_Plugin_Checker.Services;

namespace Idempiere_Plugin_Checker.Controllers;

public class HomeController : Controller
{
    private readonly DataContext _db;
    private readonly JsonExtractor _jsonExtractor;
    private readonly ILogger<HomeController> _logger;

    public HomeController(DataContext db, JsonExtractor jsonExtractor, ILogger<HomeController> logger)
    {
        _db = db;
        _jsonExtractor = jsonExtractor;
        _logger = logger;
    }

    public async Task<IActionResult> Index(int? ambienteId)
    {
        var ambientes = await _db.Ambientes
            .Where(a => a.IsAct)
            .Include(a => a.Plugins)
            .OrderBy(a => a.IdAmb)
            .ToListAsync();

        var query = _db.Plugins
            .Include(p => p.Ambiente)
            .AsQueryable();

        if (ambienteId.HasValue && ambienteId.Value > 0)
        {
            query = query.Where(p => p.IdAmb == ambienteId.Value);
        }

        var plugins = await query
            .OrderBy(p => p.SymbolicName)
            .ToListAsync();

        var vm = new HomeViewModel
        {
            Ambientes = ambientes,
            Plugins = plugins,
            SelectedAmbienteId = ambienteId,
            SyncMessage = TempData["SyncMessage"] as string,
            SyncSuccess = TempData["SyncSuccess"] as bool?
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SyncAmbiente(int id)
    {
        var result = await _jsonExtractor.SyncAmbientePluginsAsync(_db, id);

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            return Json(new { success = result.Success, message = result.Message, count = result.SyncedCount });
        }

        TempData["SyncSuccess"] = result.Success;
        TempData["SyncMessage"] = result.Message;

        return RedirectToAction(nameof(Index), new { ambienteId = id });
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
