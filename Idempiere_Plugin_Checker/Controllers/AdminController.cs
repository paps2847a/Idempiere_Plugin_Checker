using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Idempiere_Plugin_Checker.DB;
using Idempiere_Plugin_Checker.Models;
using Idempiere_Plugin_Checker.Services;

namespace Idempiere_Plugin_Checker.Controllers;

public class AdminController : Controller
{
    private readonly DataContext _db;
    private readonly JsonExtractor _jsonExtractor;
    private readonly ILogger<AdminController> _logger;

    public AdminController(DataContext db, JsonExtractor jsonExtractor, ILogger<AdminController> logger)
    {
        _db = db;
        _jsonExtractor = jsonExtractor;
        _logger = logger;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction(nameof(Index));
        }

        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var admin = await _db.AdminUsers
            .FirstOrDefaultAsync(u => u.UsrNam == model.Username && u.UsrPass == model.Password && u.IsAct);

        if (admin == null)
        {
            ModelState.AddModelError(string.Empty, "Usuario o contraseña inválidos o cuenta inactiva.");
            return View(model);
        }

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, admin.UsrNam),
            new Claim(ClaimTypes.NameIdentifier, admin.IdUsr.ToString()),
            new Claim(ClaimTypes.Role, "Admin")
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = model.RememberMe,
            ExpiresUtc = model.RememberMe ? DateTimeOffset.UtcNow.AddDays(7) : DateTimeOffset.UtcNow.AddHours(8)
        };

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProperties);
        _logger.LogInformation("Administrador {User} inició sesión con éxito.", admin.UsrNam);

        if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
        {
            return Redirect(model.ReturnUrl);
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        _logger.LogInformation("Administrador cerró sesión.");
        return RedirectToAction(nameof(Login));
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var ambientes = await _db.Ambientes
            .Include(a => a.Plugins)
            .Include(a => a.KeyUsers)
            .OrderBy(a => a.IdAmb)
            .ToListAsync();

        var keyUsers = await _db.KeyUsersAmbientes
            .Include(k => k.Ambiente)
            .OrderBy(k => k.IdAmb)
            .ThenBy(k => k.IdUsr)
            .ToListAsync();

        var totalPlugins = await _db.Plugins.CountAsync();

        var vm = new AdminDashboardViewModel
        {
            Ambientes = ambientes,
            KeyUsers = keyUsers,
            TotalPlugins = totalPlugins,
            Message = TempData["AdminMessage"] as string,
            MessageSuccess = TempData["AdminSuccess"] as bool?
        };

        return View(vm);
    }

    [Authorize]
    [HttpGet]
    public IActionResult CreateAmbiente()
    {
        return View(new AmbienteFormViewModel());
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateAmbiente(AmbienteFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var ambiente = new Ambiente
        {
            NamAmb = model.NamAmb.Trim(),
            DirAmb = model.DirAmb.Trim(),
            IsAct = model.IsAct,
            SyncIntervalMinutes = model.SyncIntervalMinutes > 0 ? model.SyncIntervalMinutes : 5,
            RegDat = DateTime.Now
        };

        _db.Ambientes.Add(ambiente);
        await _db.SaveChangesAsync();

        // Si se suministraron credenciales iniciales para OSGi, creamos el KeyUsersAmbiente
        if (!string.IsNullOrWhiteSpace(model.UsrNam) && !string.IsNullOrWhiteSpace(model.UsrPass))
        {
            var nextIdUsr = (await _db.KeyUsersAmbientes.Select(k => (int?)k.IdUsr).MaxAsync() ?? 0) + 1;
            var keyUser = new KeyUsersAmbiente
            {
                IdUsr = nextIdUsr,
                IdAmb = ambiente.IdAmb,
                UsrNam = model.UsrNam.Trim(),
                UsrPass = model.UsrPass.Trim(),
                IsAct = true,
                RegDat = DateTime.Now
            };

            _db.KeyUsersAmbientes.Add(keyUser);
            await _db.SaveChangesAsync();
        }

        TempData["AdminSuccess"] = true;
        TempData["AdminMessage"] = $"Ambiente '{ambiente.NamAmb}' creado exitosamente con lapso de sincronización de {ambiente.SyncIntervalMinutes} minutos.";

        return RedirectToAction(nameof(Index));
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> EditAmbiente(int id)
    {
        var ambiente = await _db.Ambientes.FindAsync(id);
        if (ambiente == null)
        {
            return NotFound();
        }

        var model = new AmbienteFormViewModel
        {
            IdAmb = ambiente.IdAmb,
            NamAmb = ambiente.NamAmb,
            DirAmb = ambiente.DirAmb,
            IsAct = ambiente.IsAct,
            SyncIntervalMinutes = ambiente.SyncIntervalMinutes
        };

        return View(model);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditAmbiente(AmbienteFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var ambiente = await _db.Ambientes.FindAsync(model.IdAmb);
        if (ambiente == null)
        {
            return NotFound();
        }

        ambiente.NamAmb = model.NamAmb.Trim();
        ambiente.DirAmb = model.DirAmb.Trim();
        ambiente.IsAct = model.IsAct;
        ambiente.SyncIntervalMinutes = model.SyncIntervalMinutes > 0 ? model.SyncIntervalMinutes : 5;

        await _db.SaveChangesAsync();

        TempData["AdminSuccess"] = true;
        TempData["AdminMessage"] = $"Ambiente '{ambiente.NamAmb}' actualizado exitosamente (lapso: {ambiente.SyncIntervalMinutes} min).";

        return RedirectToAction(nameof(Index));
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleAmbiente(int id)
    {
        var ambiente = await _db.Ambientes.FindAsync(id);
        if (ambiente != null)
        {
            ambiente.IsAct = !ambiente.IsAct;
            await _db.SaveChangesAsync();
            TempData["AdminSuccess"] = true;
            TempData["AdminMessage"] = $"Ambiente '{ambiente.NamAmb}' ahora está {(ambiente.IsAct ? "Activo" : "Inactivo")}.";
        }

        return RedirectToAction(nameof(Index));
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> CreateKeyUser(int? ambienteId)
    {
        var ambientes = await _db.Ambientes.OrderBy(a => a.NamAmb).ToListAsync();
        var model = new KeyUserFormViewModel
        {
            IdAmb = ambienteId ?? (ambientes.FirstOrDefault()?.IdAmb ?? 0),
            AmbientesDisponibles = ambientes
        };

        return View(model);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateKeyUser(KeyUserFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.AmbientesDisponibles = await _db.Ambientes.OrderBy(a => a.NamAmb).ToListAsync();
            return View(model);
        }

        var nextIdUsr = (await _db.KeyUsersAmbientes.Select(k => (int?)k.IdUsr).MaxAsync() ?? 0) + 1;

        var keyUser = new KeyUsersAmbiente
        {
            IdUsr = nextIdUsr,
            IdAmb = model.IdAmb,
            UsrNam = model.UsrNam.Trim(),
            UsrPass = model.UsrPass.Trim(),
            IsAct = model.IsAct,
            RegDat = DateTime.Now
        };

        _db.KeyUsersAmbientes.Add(keyUser);
        await _db.SaveChangesAsync();

        var amb = await _db.Ambientes.FindAsync(model.IdAmb);

        TempData["AdminSuccess"] = true;
        TempData["AdminMessage"] = $"Usuario de solicitud '{keyUser.UsrNam}' asignado al ambiente '{amb?.NamAmb ?? model.IdAmb.ToString()}'.";

        return RedirectToAction(nameof(Index));
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TestConnection(int idAmb)
    {
        var result = await _jsonExtractor.SyncAmbientePluginsAsync(_db, idAmb);

        TempData["AdminSuccess"] = result.Success;
        TempData["AdminMessage"] = result.Message;

        return RedirectToAction(nameof(Index));
    }
}
