using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Idempiere_Plugin_Checker.Models;

public class PluginData
{
    public int Id { get; set; }
    public int IdAmb { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool Fragment { get; set; }
    public int StateRaw { get; set; }
    public string State { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string SymbolicName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string? SubidoPor { get; set; }

    public Ambiente? Ambiente { get; set; }

    /// <summary>
    /// Extrae las iniciales del autor si la versión contiene el formato:
    /// e.g. "12.0.0.PR20260915164128" -> "PR"
    /// </summary>
    public string ResolvedSubidoPor
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(SubidoPor))
                return SubidoPor;

            if (!string.IsNullOrWhiteSpace(Version))
            {
                var match = Regex.Match(Version, @"(?:\.|\b)([A-Za-z]+)(\d{14})\b");
                if (match.Success)
                {
                    return match.Groups[1].Value.ToUpperInvariant();
                }
            }

            return "Sistema";
        }
    }

    /// <summary>
    /// Extrae y formatea la fecha y hora si la versión contiene el formato con 14 dígitos:
    /// e.g. "12.0.0.PR20260915164128" -> "15/09/2026 16:41:28"
    /// </summary>
    public string? ResolvedUploadDate
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Version))
                return null;

            var match = Regex.Match(Version, @"(?:\.|\b)[A-Za-z]+(\d{14})\b");
            if (match.Success)
            {
                var dateStr = match.Groups[1].Value;
                if (DateTime.TryParseExact(dateStr, "yyyyMMddHHmmss", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
                {
                    return dt.ToString("dd/MM/yyyy HH:mm");
                }
            }

            return null;
        }
    }

    public bool IsActive => string.Equals(State, "Active", StringComparison.OrdinalIgnoreCase);
}
