using System;

namespace Idempiere_Plugin_Checker.Models;

public class AdminUser
{
    public int IdUsr { get; set; }
    public string UsrNam { get; set; } = string.Empty;
    public string UsrPass { get; set; } = string.Empty;
    public DateTime RegDat { get; set; } = DateTime.Now;
    public bool IsAct { get; set; } = true;
}
