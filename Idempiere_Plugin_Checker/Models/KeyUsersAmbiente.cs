using System;

namespace Idempiere_Plugin_Checker.Models;

public class KeyUsersAmbiente
{
    public int IdUsr { get; set; }
    public int IdAmb { get; set; }
    public string UsrNam { get; set; } = string.Empty;
    public string UsrPass { get; set; } = string.Empty;
    public DateTime RegDat { get; set; } = DateTime.Now;
    public bool IsAct { get; set; } = true;

    public Ambiente? Ambiente { get; set; }
}
