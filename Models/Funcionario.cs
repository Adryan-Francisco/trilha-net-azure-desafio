using System.ComponentModel.DataAnnotations;

namespace TrilhaNetAzureDesafio.Models
{
    public class Funcionario
    {
        public Funcionario() { }

        public Funcionario(int id, string nome, string endereco, string ramal, string emailProfissional, string departamento, decimal salario, DateTime dataAdmissao)
        {
            Id = id;
            Nome = nome;
            Endereco = endereco;
            Ramal = ramal;
            EmailProfissional = emailProfissional;
            Departamento = departamento;
            Salario = salario;
            DataAdmissao = dataAdmissao;
        }

        public int Id { get; set; }
        [Required, StringLength(200)] public string Nome { get; set; }
        [StringLength(1000)] public string Endereco { get; set; }
        [StringLength(20)] public string Ramal { get; set; }
        [Required, EmailAddress, StringLength(254)] public string EmailProfissional { get; set; }
        [Required, StringLength(200)] public string Departamento { get; set; }
        [Range(typeof(decimal), "0", "9999999999999999.99", ParseLimitsInInvariantCulture = true)] public decimal Salario { get; set; }
        public DateTimeOffset? DataAdmissao { get; set; }
    }
}
