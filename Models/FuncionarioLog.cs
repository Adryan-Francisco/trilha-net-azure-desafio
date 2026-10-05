using System.Text.Json;
using System.Globalization;
using Azure;
using Azure.Data.Tables;

namespace TrilhaNetAzureDesafio.Models
{
    public class FuncionarioLog : Funcionario, ITableEntity
    {
        public FuncionarioLog() { }

        public FuncionarioLog(Funcionario funcionario, TipoAcao tipoAcao, string partitionKey, string rowKey)
        {
            base.Id = funcionario.Id;
            base.Nome = funcionario.Nome;
            base.Endereco = funcionario.Endereco;
            base.Ramal = funcionario.Ramal;
            base.EmailProfissional = funcionario.EmailProfissional;
            base.Departamento = funcionario.Departamento;
            base.Salario = funcionario.Salario;
            base.DataAdmissao = funcionario.DataAdmissao;
            TipoAcao = tipoAcao;
            JSON = JsonSerializer.Serialize(funcionario);
            PartitionKey = partitionKey;
            RowKey = rowKey;
        }

        public TipoAcao TipoAcao { get; set; }
        public string JSON { get; set; }
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        public TableEntity ToTableEntity() => new TableEntity(PartitionKey, RowKey)
        {
            [nameof(Id)] = Id,
            [nameof(Nome)] = Nome,
            [nameof(Endereco)] = Endereco,
            [nameof(Ramal)] = Ramal,
            [nameof(EmailProfissional)] = EmailProfissional,
            [nameof(Departamento)] = Departamento,
            // Table Storage does not support decimal. A string retains exact monetary values.
            [nameof(Salario)] = Salario.ToString(CultureInfo.InvariantCulture),
            [nameof(DataAdmissao)] = DataAdmissao?.ToUniversalTime(),
            [nameof(TipoAcao)] = (int)TipoAcao,
            [nameof(JSON)] = JSON
        };
    }
}
