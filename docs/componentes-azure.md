# Componentes Azure — GeoLoc

## Visão Geral dos Recursos

A solução utiliza **4 recursos Azure**, provisionados via Terraform:

```
Resource Group: rg4geoloc
├── Storage Account (geolocstorXXXXXX)
│   └── Table: UserAddresses
├── Azure Maps Account (geoloc-maps)
└── Function App (geoloc-func-XXXXXX)
    └── Service Plan (geoloc-plan) — Consumption/Y1
```

---

## 1. Resource Group

| Propriedade | Valor |
|-------------|-------|
| **Nome** | `rg4geoloc` |
| **Região** | East US 2 |
| **Propósito** | Container lógico que agrupa todos os recursos |

O Resource Group facilita o gerenciamento do ciclo de vida: ao excluí-lo, todos os recursos são removidos juntos. Em ambientes reais, permite controle de acesso (RBAC) e billing por grupo.

---

## 2. Azure Storage Account (Table Storage)

| Propriedade | Valor |
|-------------|-------|
| **Tipo** | StorageV2 (General Purpose v2) |
| **Replicação** | LRS (Locally Redundant Storage) |
| **Tier** | Standard |
| **Tabela** | `UserAddresses` |

### Papel na Arquitetura
Armazena os **endereços cadastrados dos usuários**. Cada registro é uma entidade com:

| Campo | Tipo | Exemplo | Descrição |
|-------|------|---------|-----------|
| `PartitionKey` | string | `BR-SP` | Região (para particionamento) |
| `RowKey` | string | `user001` | Identificador único do usuário |
| `FullAddress` | string | `Av. Paulista, 1000` | Endereço (logradouro) |
| `City` | string | `São Paulo` | Cidade |
| `State` | string | `SP` | Estado |
| `ZipCode` | string | `01310-100` | CEP |
| `Country` | string | `Brazil` | País |

### Por que Table Storage?
- **Custo extremamente baixo** — centavos por mês para volumes de demo
- **NoSQL simples** — modelo chave-valor, sem necessidade de SQL
- **SDK nativo** — `Azure.Data.Tables` com suporte a `ITableEntity`
- **Serverless-friendly** — não requer provisionamento de throughput

### Uso duplo
A mesma Storage Account é usada pela Function App para armazenamento interno (`AzureWebJobsStorage`) e para a tabela de dados, reduzindo custos.

---

## 3. Azure Maps Account

| Propriedade | Valor |
|-------------|-------|
| **SKU** | Gen2 (G2) |
| **Região** | Global |
| **API utilizada** | Search Address |

### Papel na Arquitetura
Converte **endereço textual em coordenadas geográficas** (geocodificação). Sem ele, seria impossível comparar o endereço cadastrado com as coordenadas GPS do dispositivo.

### Como criar o Azure Maps Account

#### Via Portal Azure

1. Acesse o [Portal Azure](https://portal.azure.com)
2. Clique em **"Create a resource"** (Criar um recurso)
3. Pesquise por **"Azure Maps"** e selecione **Azure Maps**
4. Clique em **"Create"**
5. Preencha:
   - **Subscription:** selecione sua assinatura
   - **Resource group:** selecione ou crie um (ex: `rg4geoloc`)
   - **Name:** nome do recurso (ex: `geoloc-maps`)
   - **Pricing tier:** selecione **Gen2**
6. Clique em **"Review + create"** e depois **"Create"**
7. Aguarde o deploy finalizar

#### Via Azure CLI

```bash
az maps account create \
    --name geoloc-maps \
    --resource-group rg4geoloc \
    --sku G2 \
    --kind Gen2
```

#### Via Terraform (usado neste projeto)

```hcl
resource "azurerm_maps_account" "main" {
  name                = "geoloc-maps"
  resource_group_name = azurerm_resource_group.main.name
  location            = "global"
  sku_name            = "G2"
}
```

### Como obter a chave de autenticação (Subscription Key)

#### Via Portal Azure

1. Acesse o recurso **Azure Maps** no [Portal Azure](https://portal.azure.com)
2. No menu lateral, clique em **"Authentication"** (Autenticação)
3. Na seção **"Shared Key Authentication"**, copie a **Primary Key**
4. Esta é a `subscription-key` usada nas chamadas à API

#### Via Azure CLI

```bash
az maps account keys list \
    --name geoloc-maps \
    --resource-group rg4geoloc \
    --query primaryKey -o tsv
```

#### Via Terraform (output)

```bash
terraform output -raw azure_maps_key
```

### Como usar a chave na aplicação

A chave é passada como parâmetro `subscription-key` na URL de cada chamada à API:

```
GET https://atlas.microsoft.com/search/address/json
    ?api-version=1.0
    &subscription-key={SUA_CHAVE_AQUI}
    &query=Av. Paulista, 1000, São Paulo
    &limit=1
```

No código C# deste projeto, a chave é lida da variável de ambiente `AzureMapsSubscriptionKey`:

```csharp
// Configuração (api/local.settings.json)
"AzureMapsSubscriptionKey": "<SUA_CHAVE_AQUI>"

// Uso no serviço (api/Services/AzureMapsService.cs)
_subscriptionKey = configuration["AzureMapsSubscriptionKey"];

var url = $"https://atlas.microsoft.com/search/address/json"
        + $"?api-version=1.0"
        + $"&subscription-key={_subscriptionKey}"
        + $"&query={encodedAddress}&limit=1";
```

> **Importante:** A chave deve ser mantida no backend (Azure Functions). Nunca exponha a `subscription-key` no código do app mobile — todas as chamadas ao Azure Maps passam pelo backend.

### API: Search Address
```
GET https://atlas.microsoft.com/search/address/json
    ?api-version=1.0
    &subscription-key={key}
    &query={endereço codificado}
    &limit=1
```

**Entrada:** `"Av. Paulista, 1000, São Paulo, SP, 01310-100, Brazil"`

**Saída:**
```json
{
  "results": [{
    "position": {
      "lat": -23.5650242,
      "lon": -46.6519791
    },
    "address": {
      "freeformAddress": "Avenida Paulista, 1000, São Paulo, SP"
    }
  }]
}
```

### Custo
- Gen2 inclui **5.000 transações gratuitas/mês** para Search
- Acima de 5.000: **$4.50 por 1.000 transações**
- Desconto por volume a partir de 100.000 transações/mês
- Para demo, o custo é efetivamente zero

---

## 4. Azure Functions App

| Propriedade | Valor |
|-------------|-------|
| **Runtime** | .NET 8 Isolated Worker |
| **Plano** | Consumption (Y1) — Serverless |
| **OS** | Linux |
| **Versão** | Azure Functions v4 |

### Papel na Arquitetura
É o **backend da solução** — hospeda o endpoint HTTP que orquestra todo o fluxo de validação de localização.

### Endpoint

| Método | Rota | Auth Level |
|--------|------|-----------|
| POST | `/api/validate-location` | Function |

### Fluxo interno
```
Request → Parse JSON → Query Table Storage → Geocode (Azure Maps) → Haversine → Response
```

### Consumption Plan
- **Sem custo fixo** — paga apenas por execução
- **1 milhão** de execuções gratuitas por mês
- **400.000 GB-s** de computação gratuitos por mês
- Scale automático (0 a N instâncias)
- Cold start de ~1-2 segundos na primeira execução

### App Settings (variáveis de ambiente)

| Setting | Descrição |
|---------|-----------|
| `TableStorageConnectionString` | Connection string da Storage Account |
| `AzureMapsSubscriptionKey` | Chave de autenticação do Azure Maps |
| `FUNCTIONS_WORKER_RUNTIME` | `dotnet-isolated` |
| `AzureWebJobsStorage` | Storage para runtime interno |

---

## Diagrama de Custos (estimativa mensal para demo)

| Recurso | Custo estimado |
|---------|---------------|
| Resource Group | Gratuito |
| Storage Account (LRS) | ~$0.01 |
| Azure Maps Search (até 5K req) | Gratuito |
| Azure Maps Search (acima de 5K) | $4.50 / 1.000 req |
| Function App (Consumption) | Gratuito |
| **Total (demo)** | **< $0.05/mês** |

---

## Provisioning (Terraform)

Todos os recursos são provisionados via Terraform. Veja os arquivos em `infra/`:

```hcl
# Exemplo simplificado
resource "azurerm_storage_account" "main" { ... }
resource "azurerm_storage_table" "user_addresses" { ... }
resource "azurerm_maps_account" "main" { ... }
resource "azurerm_linux_function_app" "main" { ... }
```

Para provisionar:
```bash
cd infra
cp terraform.tfvars.example terraform.tfvars
# Preencha tenant_id e subscription_id
terraform init && terraform apply
```
