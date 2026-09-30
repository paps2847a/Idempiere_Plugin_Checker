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

        var adminUsers = await _db.AdminUsers
            .OrderBy(u => u.IdUsr)
            .ToListAsync();

        var totalPlugins = await _db.Plugins.CountAsync();

        var vm = new AdminDashboardViewModel
        {
            Ambientes = ambientes,
            KeyUsers = keyUsers,
            AdminUsers = adminUsers,
            CurrentUsername = User.Identity?.Name ?? string.Empty,
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
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAmbiente(int id)
    {
        var ambiente = await _db.Ambientes
            .Include(a => a.Plugins)
            .Include(a => a.KeyUsers)
            .FirstOrDefaultAsync(a => a.IdAmb == id);

        if (ambiente == null)
        {
            TempData["AdminSuccess"] = false;
            TempData["AdminMessage"] = "El ambiente seleccionado no existe.";
            return RedirectToAction(nameof(Index));
        }

        var nombre = ambiente.NamAmb;
        _db.Ambientes.Remove(ambiente);
        await _db.SaveChangesAsync();

        TempData["AdminSuccess"] = true;
        TempData["AdminMessage"] = $"Ambiente '{nombre}' y sus datos asociados fueron eliminados correctamente.";
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
    [HttpGet]
    public async Task<IActionResult> EditKeyUser(int idUsr, int idAmb)
    {
        var keyUser = await _db.KeyUsersAmbientes
            .Include(k => k.Ambiente)
            .FirstOrDefaultAsync(k => k.IdUsr == idUsr && k.IdAmb == idAmb);

        if (keyUser == null)
        {
            return NotFound();
        }

        var model = new KeyUserEditViewModel
        {
            IdUsr = keyUser.IdUsr,
            IdAmb = keyUser.IdAmb,
            AmbienteNombre = keyUser.Ambiente?.NamAmb ?? $"Ambiente {keyUser.IdAmb}",
            UsrNam = keyUser.UsrNam,
            IsAct = keyUser.IsAct
        };

        return View(model);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditKeyUser(KeyUserEditViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var keyUser = await _db.KeyUsersAmbientes
            .FirstOrDefaultAsync(k => k.IdUsr == model.IdUsr && k.IdAmb == model.IdAmb);

        if (keyUser == null)
        {
            return NotFound();
        }

        keyUser.UsrNam = model.UsrNam.Trim();
        keyUser.IsAct = model.IsAct;
        if (!string.IsNullOrWhiteSpace(model.UsrPass))
        {
            keyUser.UsrPass = model.UsrPass.Trim();
        }

        await _db.SaveChangesAsync();

        TempData["AdminSuccess"] = true;
        TempData["AdminMessage"] = $"Usuario de solicitud '{keyUser.UsrNam}' actualizado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleKeyUser(int idUsr, int idAmb)
    {
        var keyUser = await _db.KeyUsersAmbientes
            .FirstOrDefaultAsync(k => k.IdUsr == idUsr && k.IdAmb == idAmb);

        if (keyUser != null)
        {
            keyUser.IsAct = !keyUser.IsAct;
            await _db.SaveChangesAsync();
            TempData["AdminSuccess"] = true;
            TempData["AdminMessage"] = $"Usuario '{keyUser.UsrNam}' ahora está {(keyUser.IsAct ? "Activo" : "Inactivo")}.";
        }

        return RedirectToAction(nameof(Index));
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteKeyUser(int idUsr, int idAmb)
    {
        var keyUser = await _db.KeyUsersAmbientes
            .FirstOrDefaultAsync(k => k.IdUsr == idUsr && k.IdAmb == idAmb);

        if (keyUser != null)
        {
            var name = keyUser.UsrNam;
            _db.KeyUsersAmbientes.Remove(keyUser);
            await _db.SaveChangesAsync();
            TempData["AdminSuccess"] = true;
            TempData["AdminMessage"] = $"Usuario de solicitud '{name}' eliminado correctamente.";
        }

        return RedirectToAction(nameof(Index));
    }

    [Authorize]
    [HttpGet]
    public IActionResult CreateAdminUser()
    {
        return View(new AdminUserFormViewModel());
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateAdminUser(AdminUserFormViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.UsrPass))
        {
            ModelState.AddModelError(nameof(model.UsrPass), "La contraseña es obligatoria para nuevos administradores.");
        }

        var usernameTrimmed = model.UsrNam.Trim();
        if (await _db.AdminUsers.AnyAsync(u => u.UsrNam.ToLower() == usernameTrimmed.ToLower()))
        {
            ModelState.AddModelError(nameof(model.UsrNam), "El nombre de usuario ya está registrado.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var nextIdUsr = (await _db.AdminUsers.Select(u => (int?)u.IdUsr).MaxAsync() ?? 0) + 1;
        var admin = new AdminUser
        {
            IdUsr = nextIdUsr,
            UsrNam = usernameTrimmed,
            UsrPass = model.UsrPass!.Trim(),
            IsAct = model.IsAct,
            RegDat = DateTime.Now
        };

        _db.AdminUsers.Add(admin);
        await _db.SaveChangesAsync();

        TempData["AdminSuccess"] = true;
        TempData["AdminMessage"] = $"Administrador '{admin.UsrNam}' registrado exitosamente.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> EditAdminUser(int id)
    {
        var admin = await _db.AdminUsers.FindAsync(id);
        if (admin == null)
        {
            return NotFound();
        }

        var currentUsername = User.Identity?.Name ?? string.Empty;
        var isSelf = string.Equals(admin.UsrNam, currentUsername, StringComparison.OrdinalIgnoreCase);

        var model = new AdminUserFormViewModel
        {
            IdUsr = admin.IdUsr,
            UsrNam = admin.UsrNam,
            IsAct = admin.IsAct,
            IsSelf = isSelf
        };

        return View(model);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditAdminUser(AdminUserFormViewModel model)
    {
        var admin = await _db.AdminUsers.FindAsync(model.IdUsr);
        if (admin == null)
        {
            return NotFound();
        }

        var currentUsername = User.Identity?.Name ?? string.Empty;
        var isSelf = string.Equals(admin.UsrNam, currentUsername, StringComparison.OrdinalIgnoreCase);
        model.IsSelf = isSelf;

        var usernameTrimmed = model.UsrNam.Trim();
        if (await _db.AdminUsers.AnyAsync(u => u.IdUsr != model.IdUsr && u.UsrNam.ToLower() == usernameTrimmed.ToLower()))
        {
            ModelState.AddModelError(nameof(model.UsrNam), "El nombre de usuario ya está en uso.");
        }

        if (isSelf && !model.IsAct)
        {
            ModelState.AddModelError(nameof(model.IsAct), "No puedes desactivar tu propia cuenta de administrador.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var oldUsername = admin.UsrNam;
        admin.UsrNam = usernameTrimmed;
        admin.IsAct = isSelf ? true : model.IsAct;

        if (!string.IsNullOrWhiteSpace(model.UsrPass))
        {
            admin.UsrPass = model.UsrPass.Trim();
        }

        await _db.SaveChangesAsync();

        // Si el usuario autenticado modificó su propio nombre de usuario, renovamos la cookie de sesión
        if (isSelf && !string.Equals(oldUsername, admin.UsrNam, StringComparison.OrdinalIgnoreCase))
        {
            if (HttpContext?.RequestServices?.GetService(typeof(IAuthenticationService)) != null)
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, admin.UsrNam),
                    new Claim(ClaimTypes.NameIdentifier, admin.IdUsr.ToString()),
                    new Claim(ClaimTypes.Role, "Admin")
                };
                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
            }
        }

        TempData["AdminSuccess"] = true;
        TempData["AdminMessage"] = $"Administrador '{admin.UsrNam}' actualizado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleAdminUser(int id)
    {
        var admin = await _db.AdminUsers.FindAsync(id);
        if (admin == null)
        {
            return RedirectToAction(nameof(Index));
        }

        var currentUsername = User.Identity?.Name ?? string.Empty;
        if (string.Equals(admin.UsrNam, currentUsername, StringComparison.OrdinalIgnoreCase))
        {
            TempData["AdminSuccess"] = false;
            TempData["AdminMessage"] = "No puedes desactivar tu propia cuenta de administrador.";
            return RedirectToAction(nameof(Index));
        }

        admin.IsAct = !admin.IsAct;
        await _db.SaveChangesAsync();

        TempData["AdminSuccess"] = true;
        TempData["AdminMessage"] = $"Administrador '{admin.UsrNam}' ahora está {(admin.IsAct ? "Activo" : "Inactivo")}.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAdminUser(int id)
    {
        var admin = await _db.AdminUsers.FindAsync(id);
        if (admin == null)
        {
            return RedirectToAction(nameof(Index));
        }

        var currentUsername = User.Identity?.Name ?? string.Empty;

        // Regla 1: No puedes eliminar tu propia cuenta mientras estás en sesión
        if (string.Equals(admin.UsrNam, currentUsername, StringComparison.OrdinalIgnoreCase))
        {
            TempData["AdminSuccess"] = false;
            TempData["AdminMessage"] = "No puedes eliminar tu propia cuenta mientras estás en sesión.";
            return RedirectToAction(nameof(Index));
        }

        // Regla 2: El usuario por defecto (o cualquier admin) solo se puede borrar
        // si y solo si existe al menos otro usuario administrador activo con el que se pueda entrar.
        var hasOtherActiveAdmin = await _db.AdminUsers
            .AnyAsync(u => u.IdUsr != admin.IdUsr && u.IsAct);

        if (!hasOtherActiveAdmin)
        {
            TempData["AdminSuccess"] = false;
            TempData["AdminMessage"] = "No se puede eliminar este administrador: debe existir al menos otro administrador activo para ingresar al sistema.";
            return RedirectToAction(nameof(Index));
        }

        var name = admin.UsrNam;
        _db.AdminUsers.Remove(admin);
        await _db.SaveChangesAsync();

        TempData["AdminSuccess"] = true;
        TempData["AdminMessage"] = $"Administrador '{name}' eliminado correctamente de la base de datos.";
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
