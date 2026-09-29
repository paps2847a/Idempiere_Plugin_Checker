using System;

namespace Idempiere_Plugin_Checker.Models;

public class Ambiente
{
    public int IdAmb { get; set; }
    public string NamAmb { get; set; } = string.Empty;
    public string DirAmb { get; set; } = string.Empty;
    public DateTime RegDat { get; set; } = DateTime.Now;
    public bool IsAct { get; set; } = true;

    public ICollection<PluginData> Plugins { get; set; } = new List<PluginData>();
    public ICollection<KeyUsersAmbiente> KeyUsers { get; set; } = new List<KeyUsersAmbiente>();
}
