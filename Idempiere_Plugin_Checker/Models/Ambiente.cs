using System;

namespace Idempiere_Plugin_Checker.Models;

public class Ambiente
{
    public int IdAmb { get; set; }
    public string NamAmb { get; set; } = string.Empty;
    public string DirAmb { get; set; } = string.Empty;
    public DateTime RegDat { get; set; } = DateTime.Now;
    public bool IsAct { get; set; } = true;

    /// <summary>
    /// Lapso de consulta en minutos entre cada actualización periódica (por defecto 5 min).
    /// </summary>
    public int SyncIntervalMinutes { get; set; } = 5;

    /// <summary>
    /// Marca de tiempo de la última sincronización ejecutada por el worker o manual.
    /// </summary>
    public DateTime? LastSyncAt { get; set; }

    public ICollection<PluginData> Plugins { get; set; } = new List<PluginData>();
    public ICollection<KeyUsersAmbiente> KeyUsers { get; set; } = new List<KeyUsersAmbiente>();
}
