using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TrilhaNetAzureDesafio.Models;

namespace TrilhaNetAzureDesafio.Context
{
    public class RHContext : DbContext
    {
        public RHContext(DbContextOptions<RHContext> options) : base(options)
        {

        }

        public DbSet<Funcionario> Funcionarios { get; set; }
        public DbSet<LogPendente> LogsPendentes { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Funcionario>().Property(f => f.Salario).HasPrecision(18, 2);
            // API validation must not truncate existing SQL data during migration.
            foreach (var property in new[] { "Nome", "Endereco", "Ramal", "EmailProfissional", "Departamento" })
                modelBuilder.Entity<Funcionario>().Property<string>(property)
                    .IsRequired(false).Metadata.SetMaxLength(null);
            modelBuilder.Entity<LogPendente>().Property(l => l.JSON).IsRequired();
        }
    }
}
