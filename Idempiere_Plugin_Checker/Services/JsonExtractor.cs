using System;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Idempiere_Plugin_Checker.DB;
using Idempiere_Plugin_Checker.Models;
using Microsoft.EntityFrameworkCore;
using Models;

namespace Idempiere_Plugin_Checker.Services;

public class SyncResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int SyncedCount { get; set; }
}

public class JsonExtractor
{
    private readonly ILogger<JsonExtractor>? _logger;

    public JsonExtractor(ILogger<JsonExtractor>? logger = null)
    {
        _logger = logger;
    }

    public async Task<SyncResult> SyncAmbientePluginsAsync(DataContext db, int idAmb)
    {
        var ambiente = await db.Ambientes
            .Include(a => a.KeyUsers)
            .FirstOrDefaultAsync(a => a.IdAmb == idAmb);

        if (ambiente == null)
        {
            return new SyncResult { Success = false, Message = $"Ambiente con ID {idAmb} no encontrado." };
        }

        var keyUser = ambiente.KeyUsers.FirstOrDefault(k => k.IsAct);
        if (keyUser == null)
        {
            return new SyncResult { Success = false, Message = $"Ambiente con ID {idAmb} no encontrado." };
        }

        var user = keyUser?.UsrNam;
        var pass = keyUser?.UsrPass;
        var baseUrl = ambiente.DirAmb;

        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return new SyncResult { Success = false, Message = "La URL del ambiente no está configurada." };
        }

        try
        {
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            };

            using var client = new HttpClient(handler)
            {
                BaseAddress = new Uri(baseUrl),
                Timeout = TimeSpan.FromSeconds(6)
            };

            var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{user}:{pass}"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);

            var response = await client.GetAsync("/osgi/system/console/bundles.json");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var root = JsonSerializer.Deserialize<Root>(json);

            if (root?.Data == null || !root.Data.Any())
            {
                return new SyncResult { Success = true, Message = "No se encontraron bundles en el endpoint.", SyncedCount = 0 };
            }

            var pluginBundles = root.Data
                .Where(x => string.Equals(x.Category, "idempiere-plugin", StringComparison.OrdinalIgnoreCase))
                .ToList();

            // Eliminar plugins previos de este ambiente para sincronización limpia
            var existing = await db.Plugins.Where(p => p.IdAmb == idAmb).ToListAsync();
            db.Plugins.RemoveRange(existing);

            foreach (var item in pluginBundles)
            {
                var plugin = new PluginData
                {
                    Id = item.Id,
                    IdAmb = idAmb,
                    Name = item.Name ?? item.SymbolicName ?? "Unknown",
                    SymbolicName = item.SymbolicName ?? "unknown.plugin",
                    Version = item.Version ?? "1.0.0",
                    State = item.State ?? "Active",
                    StateRaw = item.StateRaw,
                    Fragment = item.Fragment,
                    Category = item.Category ?? "idempiere-plugin"
                };

                // Si la versión tiene formato: 12.0.0.PR20260915164128, extraer iniciales
                var match = Regex.Match(plugin.Version, @"(?:\.|\b)([A-Za-z]+)(\d{14})\b");
                if (match.Success)
                {
                    plugin.SubidoPor = match.Groups[1].Value.ToUpperInvariant();
                }
                else
                {
                    plugin.SubidoPor = "No Identificado";
                }

                db.Plugins.Add(plugin);
            }

            await db.SaveChangesAsync();

            return new SyncResult
            {
                Success = true,
                Message = $"Sincronización exitosa: {pluginBundles.Count} plugins actualizados para '{ambiente.NamAmb}'.",
                SyncedCount = pluginBundles.Count
            };
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error al sincronizar plugins para el ambiente {Ambiente}", ambiente.NamAmb);
            return new SyncResult
            {
                Success = false,
                Message = $"No se pudo conectar a '{ambiente.NamAmb}' ({baseUrl}): {ex.Message}"
            };
        }
    }

    public async Task GetActivesPlugins(DataContext db)
    {
        await SyncAmbientePluginsAsync(db, 1);
    }
}
