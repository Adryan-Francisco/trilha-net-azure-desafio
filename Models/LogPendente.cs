namespace TrilhaNetAzureDesafio.Models;

// Persisted in the same SQL transaction as the employee change.
public class LogPendente
{
    public Guid Id { get; set; }
    public string JSON { get; set; }
    public TipoAcao TipoAcao { get; set; }
    public DateTime CriadoEm { get; set; }
}

