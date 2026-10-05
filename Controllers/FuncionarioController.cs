using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using TrilhaNetAzureDesafio.Context;
using TrilhaNetAzureDesafio.Models;

namespace TrilhaNetAzureDesafio.Controllers;

[ApiController]
[Route("[controller]")]
public class FuncionarioController : ControllerBase
{
    private readonly RHContext _context;
    public FuncionarioController(RHContext context) => _context = context;

    private void RegistrarLog(Funcionario funcionario, TipoAcao tipoAcao)
    {
        _context.LogsPendentes.Add(new LogPendente
        {
            Id = Guid.NewGuid(), JSON = JsonSerializer.Serialize(funcionario),
            TipoAcao = tipoAcao, CriadoEm = DateTime.UtcNow
        });
    }

    [HttpGet("{id}")]
    public IActionResult ObterPorId(int id)
    {
        var funcionario = _context.Funcionarios.Find(id);
        return funcionario == null ? NotFound() : Ok(funcionario);
    }

    [HttpPost]
    public IActionResult Criar(Funcionario funcionario)
    {
        if (funcionario.Id != 0)
            return BadRequest("O Id é gerado pelo banco de dados.");

        using var transaction = _context.Database.BeginTransaction();
        _context.Funcionarios.Add(funcionario);
        _context.SaveChanges();
        RegistrarLog(funcionario, TipoAcao.Inclusao);
        _context.SaveChanges();
        transaction.Commit();
        return CreatedAtAction(nameof(ObterPorId), new { id = funcionario.Id }, funcionario);
    }

    [HttpPut("{id}")]
    public IActionResult Atualizar(int id, Funcionario funcionario)
    {
        var funcionarioBanco = _context.Funcionarios.Find(id);
        if (funcionarioBanco == null)
            return NotFound();

        funcionarioBanco.Nome = funcionario.Nome;
        funcionarioBanco.Endereco = funcionario.Endereco;
        funcionarioBanco.Ramal = funcionario.Ramal;
        funcionarioBanco.EmailProfissional = funcionario.EmailProfissional;
        funcionarioBanco.Departamento = funcionario.Departamento;
        funcionarioBanco.Salario = funcionario.Salario;
        funcionarioBanco.DataAdmissao = funcionario.DataAdmissao;
        _context.Funcionarios.Update(funcionarioBanco);
        RegistrarLog(funcionarioBanco, TipoAcao.Atualizacao);
        _context.SaveChanges();
        return Ok();
    }

    [HttpDelete("{id}")]
    public IActionResult Deletar(int id)
    {
        var funcionarioBanco = _context.Funcionarios.Find(id);
        if (funcionarioBanco == null)
            return NotFound();

        RegistrarLog(funcionarioBanco, TipoAcao.Remocao);
        _context.Funcionarios.Remove(funcionarioBanco);
        _context.SaveChanges();
        return NoContent();
    }
}
