using System.ComponentModel.DataAnnotations;
using Idempiere_Plugin_Checker.Models;

namespace Idempiere_Plugin_Checker.Models;

public class LoginViewModel
{
    [Required(ErrorMessage = "El nombre de usuario es obligatorio.")]
    [Display(Name = "Usuario")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Recordar sesión")]
    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
}

public class AmbienteFormViewModel
{
    public int IdAmb { get; set; }

    [Required(ErrorMessage = "El nombre del ambiente es obligatorio.")]
    [StringLength(150, ErrorMessage = "El nombre no puede superar los 150 caracteres.")]
    [Display(Name = "Nombre del Ambiente")]
    public string NamAmb { get; set; } = string.Empty;

    [Required(ErrorMessage = "La URL base del ambiente es obligatoria.")]
    [StringLength(300, ErrorMessage = "La URL no puede superar los 300 caracteres.")]
    [Url(ErrorMessage = "Debe ser una URL válida (ej. https://192.168.6.107:8443)")]
    [Display(Name = "URL del Ambiente (Base URL)")]
    public string DirAmb { get; set; } = string.Empty;

    [Display(Name = "Activo")]
    public bool IsAct { get; set; } = true;

    [Required(ErrorMessage = "El lapso de consulta es obligatorio.")]
    [Range(1, 1440, ErrorMessage = "El lapso debe estar entre 1 y 1440 minutos (24 horas).")]
    [Display(Name = "Lapso de Consulta / Espera (Minutos)")]
    public int SyncIntervalMinutes { get; set; } = 5;

    // Credenciales iniciales opcionales para la consola OSGi
    [Display(Name = "Usuario OSGi")]
    public string? UsrNam { get; set; }

    [DataType(DataType.Password)]
    [Display(Name = "Contraseña OSGi")]
    public string? UsrPass { get; set; }
}

public class KeyUserFormViewModel
{
    public int IdUsr { get; set; }

    [Required(ErrorMessage = "Debes seleccionar un ambiente.")]
    [Display(Name = "Ambiente")]
    public int IdAmb { get; set; }

    [Required(ErrorMessage = "El nombre de usuario OSGi es obligatorio.")]
    [StringLength(100, ErrorMessage = "No puede superar 100 caracteres.")]
    [Display(Name = "Usuario de Solicitud (OSGi Console)")]
    public string UsrNam { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    [DataType(DataType.Password)]
    [StringLength(255)]
    [Display(Name = "Contraseña de Solicitud")]
    public string UsrPass { get; set; } = string.Empty;

    [Display(Name = "Activo")]
    public bool IsAct { get; set; } = true;

    public List<Ambiente> AmbientesDisponibles { get; set; } = new();
}

public class KeyUserEditViewModel
{
    public int IdUsr { get; set; }
    public int IdAmb { get; set; }

    [Display(Name = "Ambiente")]
    public string AmbienteNombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "El nombre de usuario OSGi es obligatorio.")]
    [StringLength(100, ErrorMessage = "No puede superar 100 caracteres.")]
    [Display(Name = "Usuario de Solicitud (OSGi Console)")]
    public string UsrNam { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [StringLength(255)]
    [Display(Name = "Contraseña de Solicitud (dejar en blanco para mantener la actual)")]
    public string? UsrPass { get; set; }

    [Display(Name = "Activo")]
    public bool IsAct { get; set; } = true;
}

public class AdminUserFormViewModel
{
    public int IdUsr { get; set; }

    [Required(ErrorMessage = "El nombre de usuario es obligatorio.")]
    [StringLength(100, ErrorMessage = "No puede superar 100 caracteres.")]
    [Display(Name = "Nombre de Usuario")]
    public string UsrNam { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [StringLength(255)]
    [Display(Name = "Contraseña")]
    public string? UsrPass { get; set; }

    [DataType(DataType.Password)]
    [Compare("UsrPass", ErrorMessage = "Las contraseñas no coinciden.")]
    [Display(Name = "Confirmar Contraseña")]
    public string? ConfirmPass { get; set; }

    [Display(Name = "Cuenta Activa")]
    public bool IsAct { get; set; } = true;

    public bool IsEdit => IdUsr > 0;
    public bool IsSelf { get; set; }
}

public class AdminDashboardViewModel
{
    public List<Ambiente> Ambientes { get; set; } = new();
    public List<KeyUsersAmbiente> KeyUsers { get; set; } = new();
    public List<AdminUser> AdminUsers { get; set; } = new();

    public string CurrentUsername { get; set; } = string.Empty;

    public int TotalAmbientes => Ambientes.Count;
    public int TotalAmbientesActivos => Ambientes.Count(a => a.IsAct);
    public int TotalKeyUsers => KeyUsers.Count;
    public int TotalAdminUsers => AdminUsers.Count;
    public int TotalPlugins { get; set; }

    public string? Message { get; set; }
    public bool? MessageSuccess { get; set; }
}
