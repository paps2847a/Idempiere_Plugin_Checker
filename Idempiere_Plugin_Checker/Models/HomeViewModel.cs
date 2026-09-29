using System.Collections.Generic;

namespace Idempiere_Plugin_Checker.Models;

public class HomeViewModel
{
    public List<Ambiente> Ambientes { get; set; } = new();
    public List<PluginData> Plugins { get; set; } = new();

    public int? SelectedAmbienteId { get; set; }
    public string? SearchTerm { get; set; }

    public int TotalAmbientes => Ambientes.Count;
    public int TotalPlugins => Plugins.Count;
    public int TotalActivePlugins => Plugins.Count(p => p.IsActive);

    public string? SyncMessage { get; set; }
    public bool? SyncSuccess { get; set; }
}
