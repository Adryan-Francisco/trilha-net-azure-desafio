using Azure;
using Azure.Data.Tables;
using Azure.Data.Tables.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using TrilhaNetAzureDesafio.Context;
using TrilhaNetAzureDesafio.Controllers;
using TrilhaNetAzureDesafio.Models;
using TrilhaNetAzureDesafio.Services;
using Xunit;

public class FuncionarioTests
{
    private static Funcionario Exemplo() => new()
    {
        Nome = "Ana", Endereco = "Rua 123", Ramal = "1234",
        EmailProfissional = "ana@empresa.com", Departamento = "TI/Infra",
        Salario = 1234.56m, DataAdmissao = DateTimeOffset.UtcNow
    };

    [Fact]
    public void CrudPreservaSnapshotsEId()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        using var context = new RHContext(new DbContextOptionsBuilder<RHContext>().UseSqlite(connection).Options);
        context.Database.EnsureCreated();
        var controller = new FuncionarioController(context);
        var funcionario = Exemplo();
        Assert.IsType<CreatedAtActionResult>(controller.Criar(funcionario));
        Assert.True(funcionario.Id > 0);
        Assert.IsType<OkObjectResult>(controller.ObterPorId(funcionario.Id));

        var atualizado = new Funcionario(999, "Beatriz", "Rua nova", "4321", "bia@empresa.com", "RH", 2000m, DateTime.UtcNow);
        Assert.IsType<OkResult>(controller.Atualizar(funcionario.Id, atualizado));
        Assert.Equal("Beatriz", funcionario.Nome);
        Assert.Equal("Rua nova", funcionario.Endereco);
        Assert.Equal("4321", funcionario.Ramal);
        Assert.Equal("bia@empresa.com", funcionario.EmailProfissional);
        Assert.Equal("RH", funcionario.Departamento);
        Assert.Equal(2000m, funcionario.Salario);
        Assert.Equal(atualizado.DataAdmissao, funcionario.DataAdmissao);
        Assert.NotEqual(999, funcionario.Id);
        Assert.IsType<NoContentResult>(controller.Deletar(funcionario.Id));
        context.ChangeTracker.Clear();
        Assert.Empty(context.Funcionarios);
        Assert.IsType<NotFoundResult>(controller.ObterPorId(funcionario.Id));
        var logs = context.LogsPendentes.OrderBy(l => l.CriadoEm).ToList();
        Assert.Equal(new[] { TipoAcao.Inclusao, TipoAcao.Atualizacao, TipoAcao.Remocao }, logs.Select(l => l.TipoAcao));
        Assert.Equal("Ana", JsonSerializer.Deserialize<Funcionario>(logs[0].JSON).Nome);
        Assert.Equal("Beatriz", JsonSerializer.Deserialize<Funcionario>(logs[2].JSON).Nome);
        Assert.All(logs, l => Assert.Equal(funcionario.Id, JsonSerializer.Deserialize<Funcionario>(l.JSON).Id));
    }

    [Fact]
    public void AusentesNaoGeramLogsEIdFornecidoERejeitado()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        using var context = new RHContext(new DbContextOptionsBuilder<RHContext>().UseSqlite(connection).Options);
        context.Database.EnsureCreated();
        var controller = new FuncionarioController(context);
        Assert.IsType<NotFoundResult>(controller.Atualizar(42, Exemplo()));
        Assert.IsType<NotFoundResult>(controller.Deletar(42));
        var funcionario = Exemplo();
        funcionario.Id = 42;
        Assert.IsType<BadRequestObjectResult>(controller.Criar(funcionario));
        Assert.Empty(context.LogsPendentes);
        Assert.Empty(context.Funcionarios);
    }

    [Fact]
    public void ValidacaoRejeitaNomeEmailESalarioInvalidos()
    {
        var funcionario = Exemplo();
        funcionario.Nome = "";
        funcionario.EmailProfissional = "invalido";
        funcionario.Salario = -1;
        var errors = new List<ValidationResult>();
        Assert.False(Validator.TryValidateObject(funcionario, new ValidationContext(funcionario), errors, true));
        Assert.Equal(3, errors.Count);
    }

    [Fact]
    public async Task FalhaNoStorageMantemLogParaReenvioSemDuplicacao()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        var services = new ServiceCollection();
        services.AddDbContext<RHContext>(o => o.UseSqlite(connection));
        using var provider = services.BuildServiceProvider();
        Guid logId;
        using (var scope = provider.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<RHContext>();
            context.Database.EnsureCreated();
            new FuncionarioController(context).Criar(Exemplo());
            logId = context.LogsPendentes.Single().Id;
        }
        var table = new FakeTableClient { Falhar = true };
        var worker = new PublicadorLogs(provider.GetRequiredService<IServiceScopeFactory>(), table, NullLogger<PublicadorLogs>.Instance);
        await Assert.ThrowsAsync<RequestFailedException>(() => worker.PublicarPendentesAsync(default));
        using (var scope = provider.CreateScope())
            Assert.Single(scope.ServiceProvider.GetRequiredService<RHContext>().LogsPendentes);
        table.Falhar = false;
        await worker.PublicarPendentesAsync(default);
        await worker.PublicarPendentesAsync(default);
        using (var scope = provider.CreateScope())
            Assert.Empty(scope.ServiceProvider.GetRequiredService<RHContext>().LogsPendentes);
        var entity = Assert.Single(table.Entities).Value;
        Assert.Equal(logId.ToString(), entity.RowKey);
        Assert.Equal(TimeSpan.Zero, ((DateTimeOffset)entity["DataAcao"] ).Offset);
        Assert.Equal("1234.56", entity["Salario"]);
        Assert.Equal(0, entity["TipoAcao"]);
        Assert.Equal("TI/Infra", entity["Departamento"]);
        Assert.Equal(1, JsonSerializer.Deserialize<Funcionario>((string)entity["JSON"]).Id);
    }

    private class FakeTableClient : TableClient
    {
        public bool Falhar { get; set; }
        public Dictionary<string, TableEntity> Entities { get; } = new();
        public override Task<Response<TableItem>> CreateIfNotExistsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<Response<TableItem>>(null);
        public override Task<Response> UpsertEntityAsync<T>(T entity, TableUpdateMode mode = TableUpdateMode.Merge, CancellationToken cancellationToken = default)
        {
            if (Falhar) throw new RequestFailedException(503, "Storage indisponível");
            Entities[entity.RowKey] = (TableEntity)(object)entity;
            return Task.FromResult<Response>(null);
        }
    }
}
