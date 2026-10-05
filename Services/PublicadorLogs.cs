using Azure.Data.Tables;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using TrilhaNetAzureDesafio.Context;
using TrilhaNetAzureDesafio.Models;

namespace TrilhaNetAzureDesafio.Services;

public class PublicadorLogs : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly TableClient _table;
    private readonly ILogger<PublicadorLogs> _logger;

    public PublicadorLogs(IServiceScopeFactory scopes, TableClient table, ILogger<PublicadorLogs> logger)
    {
        _scopes = scopes;
        _table = table;
        _logger = logger;
    }

    public async Task PublicarPendentesAsync(CancellationToken cancellationToken)
    {
        await _table.CreateIfNotExistsAsync(cancellationToken);
        using var scope = _scopes.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<RHContext>();
        var pendentes = await context.LogsPendentes.OrderBy(l => l.CriadoEm)
            .Take(100).ToListAsync(cancellationToken);

        foreach (var pendente in pendentes)
        {
            var funcionario = JsonSerializer.Deserialize<Funcionario>(pendente.JSON);
            // Employee IDs are valid keys regardless of characters in the department name.
            var log = new FuncionarioLog(funcionario, pendente.TipoAcao,
                funcionario.Id.ToString(System.Globalization.CultureInfo.InvariantCulture), pendente.Id.ToString());
            var entity = log.ToTableEntity();
            entity["DataAcao"] = new DateTimeOffset(DateTime.SpecifyKind(pendente.CriadoEm, DateTimeKind.Utc));
            // Stable keys avoid duplicate logs if acknowledgement in SQL fails.
            await _table.UpsertEntityAsync(entity, TableUpdateMode.Replace, cancellationToken);
            context.LogsPendentes.Remove(pendente);
            await context.SaveChangesAsync(cancellationToken);
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try { await PublicarPendentesAsync(stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Falha ao publicar logs. Registros permanecem pendentes no SQL.");
            }

            try { await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
        }
    }
}
