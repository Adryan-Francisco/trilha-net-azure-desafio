# Execução e publicação

## Implementação

Web API em .NET 8, com os quatro endpoints do enunciado. O SQL Server mantém os funcionários e uma fila durável (`LogsPendentes`). Inclusão, atualização e remoção salvam um snapshot do funcionário na mesma transação da alteração. O publicador em segundo plano transfere até 100 logs por ciclo para Azure Table Storage, a cada 10 segundos. Ele só remove um registro pendente depois de confirmar o envio; uma falha conserva o registro para nova tentativa.

`FuncionarioLog` continua herdando de `Funcionario`. Para gravar no Table Storage, `ToTableEntity` converte salário para texto decimal exato e ação para inteiro (0 = inclusão, 1 = atualização, 2 = remoção). As chaves são o ID do funcionário e o GUID do evento. `JSON` contém o snapshot completo; `DataAcao` registra o instante UTC da alteração, enquanto `Timestamp` é mantido pelo Azure. As chaves estáveis evitam duplicar o evento em reenvios. O histórico permanece após excluir o funcionário. A publicação é eventual: aguarde o ciclo e consulte a tabela pelo Storage Explorer.

## Executar localmente

Requisitos: SDK .NET 8 ou posterior, SQL Server (ou LocalDB) e Azure Storage ou Azurite com serviço de tabelas habilitado.

Na raiz do projeto, configure valores apenas no ambiente da sessão PowerShell. Substitua a conexão SQL conforme seu servidor:

```powershell
$env:ConnectionStrings__ConexaoPadrao = 'Server=(localdb)\MSSQLLocalDB;Database=RHDesafio;Trusted_Connection=True;TrustServerCertificate=True'
$env:ConnectionStrings__SAConnectionString = 'UseDevelopmentStorage=true'
$env:ConnectionStrings__AzureTableName = 'FuncionarioLog'
dotnet tool install dotnet-ef --version 8.0.22 --tool-path .tools
.tools\dotnet-ef database update
dotnet run
```

Se `.tools` já estiver instalado, pule o comando de instalação. Abra `https://localhost:7090/swagger`. A migration inicial já existia; a nova migration adiciona somente `LogsPendentes`, preservando as colunas dos funcionários.

Exemplo de POST `/Funcionario`:

```json
{
  "nome": "Ana Silva",
  "endereco": "Rua 123",
  "ramal": "1234",
  "emailProfissional": "ana@empresa.com",
  "departamento": "TI",
  "salario": 1234.56,
  "dataAdmissao": "2026-10-05T12:00:00Z"
}
```

Use o ID retornado para GET, PUT e DELETE `/Funcionario/{id}`. No PUT envie todos os campos. O ID da rota prevalece; no POST o ID deve ser omitido ou zero. Respostas: POST 201, GET e PUT 200, DELETE 204; ID inexistente retorna 404; dados inválidos retornam 400.

## Validação

```powershell
dotnet build
dotnet test Tests\Tests.csproj
dotnet publish -c Release -o artifacts\publish
```

Os testes usam SQLite em memória e um cliente de tabela simulado. Verificam CRUD, snapshots, validação, IDs inexistentes e recuperação de falha no Storage. Eles não substituem a validação em SQL Database e Azure Table reais.

## Publicar no Azure

Requisitos: assinatura Azure, Azure CLI autenticada (`az login`), SDK .NET e permissão para criar os recursos. O template cria App Service Linux B1 com Always On, SQL Database Basic, Storage Account e tabela. Esses recursos têm cobrança. Use um prefixo de 3 a 11 letras minúsculas/números, começando por letra.

```powershell
az login
az account set --subscription '<id-da-assinatura>'
az group create --name rg-rh-desafio --location brazilsouth
az deployment group create --resource-group rg-rh-desafio --name rh --template-file infra/main.bicep --parameters prefix=rhdesafio sqlAdmin=rhadmin
```

A CLI solicita o parâmetro seguro `sqlPassword`; não grave a senha no repositório. Confira as saídas da implantação para obter os nomes do App Service e SQL Server.

Antes de iniciar a API, aplique as migrations ao SQL Database. No portal, abra o SQL Server, adicione temporariamente seu IP público no firewall e configure `ConnectionStrings__ConexaoPadrao` nesta sessão com a conexão do banco RH. Execute:

```powershell
.tools\dotnet-ef database update
```

Depois remova do firewall a regra temporária do seu IP. O template habilita a regra Azure Services para permitir acesso do App Service ao SQL. As conexões do aplicativo são configuradas pelo template nas configurações do App Service.

```powershell
dotnet publish -c Release -o artifacts\publish
Compress-Archive -Path artifacts\publish\* -DestinationPath artifacts\app.zip -Force
az webapp deploy --resource-group rg-rh-desafio --name '<appName-da-saida>' --src-path artifacts/app.zip --type zip
```

Abra a URL `swaggerUrl` da implantação, execute POST → GET → PUT → DELETE e confira três eventos em `FuncionarioLog` pelo Azure Storage Explorer. Não foi realizada publicação real sem assinatura e autenticação Azure fornecidas para esta sessão.

Referências: [App Service para .NET](https://learn.microsoft.com/azure/app-service/quickstart-dotnetcore), [tipos aceitos pelo Azure Table](https://learn.microsoft.com/rest/api/storageservices/understanding-the-table-service-data-model).
