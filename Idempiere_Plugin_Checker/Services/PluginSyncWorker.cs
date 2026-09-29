using Microsoft.EntityFrameworkCore;
using Idempiere_Plugin_Checker.DB;

namespace Idempiere_Plugin_Checker.Services;

/// <summary>
/// Worker ligero en segundo plano que consulta y actualiza periódicamente el estado
/// de los plugins en cada ambiente activo de iDempiere según su lapso configurado.
/// Si no existen ambientes activos, no realiza peticiones.
/// </summary>
public class PluginSyncWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PluginSyncWorker> _logger;
    private readonly IConfiguration _configuration;

    public PluginSyncWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<PluginSyncWorker> logger,
        IConfiguration configuration)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("PluginSyncWorker iniciado.");

        // Retardo inicial breve para permitir que la aplicación arranque por completo
        var initialDelaySec = _configuration.GetValue<int>("WorkerSettings:InitialDelaySeconds", 10);
        await Task.Delay(TimeSpan.FromSeconds(initialDelaySec), stoppingToken);

        // Ciclo periódico del worker (revisa cada 30 segundos qué ambientes han cumplido su lapso)
        var checkTickSeconds = _configuration.GetValue<int>("WorkerSettings:CheckTickSeconds", 30);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var isEnabled = _configuration.GetValue<bool>("WorkerSettings:Enabled", true);
                if (!isEnabled)
                {
                    _logger.LogDebug("PluginSyncWorker está deshabilitado en la configuración.");
                    await Task.Delay(TimeSpan.FromSeconds(checkTickSeconds), stoppingToken);
                    continue;
                }

                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<DataContext>();
                var jsonExtractor = scope.ServiceProvider.GetRequiredService<JsonExtractor>();

                // Obtener ambientes activos
                var activeAmbientes = await db.Ambientes
                    .Where(a => a.IsAct)
                    .ToListAsync(stoppingToken);

                // REGLA: Si no existen ambientes, el worker no hace nada
                if (!activeAmbientes.Any())
                {
                    _logger.LogDebug("PluginSyncWorker: No existen ambientes activos configurados. Omitiendo consultas.");
                    await Task.Delay(TimeSpan.FromSeconds(checkTickSeconds), stoppingToken);
                    continue;
                }

                var now = DateTime.Now;

                foreach (var amb in activeAmbientes)
                {
                    if (stoppingToken.IsCancellationRequested)
                        break;

                    // El lapso por defecto es de 5 minutos si no está especificado
                    var intervalMinutes = amb.SyncIntervalMinutes > 0 ? amb.SyncIntervalMinutes : 5;
                    var interval = TimeSpan.FromMinutes(intervalMinutes);

                    // Verificar si ya transcurrió el tiempo de espera configurado para este ambiente
                    var isDue = !amb.LastSyncAt.HasValue || (now - amb.LastSyncAt.Value) >= interval;

                    if (isDue)
                    {
                        _logger.LogInformation(
                            "PluginSyncWorker: Consultando ambiente '{Ambiente}' (ID: {Id}, Lapso: {Interval} min)...",
                            amb.NamAmb, amb.IdAmb, intervalMinutes);

                        try
                        {
                            var result = await jsonExtractor.SyncAmbientePluginsAsync(db, amb.IdAmb);
                            amb.LastSyncAt = DateTime.Now;
                            await db.SaveChangesAsync(stoppingToken);

                            _logger.LogInformation(
                                "PluginSyncWorker: Ambiente '{Ambiente}' actualizado. Éxito: {Success}. {Message}",
                                amb.NamAmb, result.Success, result.Message);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(
                                ex,
                                "PluginSyncWorker: Error al consultar ambiente '{Ambiente}'. Se reintentará en el próximo ciclo.",
                                amb.NamAmb);

                            // Registrar la tentativa para no reintentar de inmediato en el siguiente segundo si falló
                            amb.LastSyncAt = DateTime.Now;
                            await db.SaveChangesAsync(stoppingToken);
                        }
                    }
                }
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "PluginSyncWorker: Ocurrió un error no controlado en el ciclo del worker.");
            }

            // Esperar el intervalo de sondeo hasta la siguiente revisión
            await Task.Delay(TimeSpan.FromSeconds(checkTickSeconds), stoppingToken);
        }

        _logger.LogInformation("PluginSyncWorker detenido.");
    }
}
