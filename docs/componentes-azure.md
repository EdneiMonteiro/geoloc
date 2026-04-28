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

### Autenticação via Managed Identity (recomendado para produção)

Em produção, o recomendado é substituir a Shared Key por **Managed Identity + Microsoft Entra ID**, eliminando chaves estáticas do código e configurações.

#### Por que usar Managed Identity?

| Aspecto | Shared Key | Managed Identity |
|---------|-----------|-----------------|
| **Segurança** | Chave estática que pode vazar | Sem chaves — token gerenciado pelo Azure |
| **Rotação** | Manual (regenerar + redistribuir) | Automática (tokens de curta duração) |
| **Auditoria** | Difícil rastrear quem usou | Integrada com Azure AD logs |
| **Revogação** | Requer regenerar a chave (afeta todos) | Revoga por role assignment individual |

#### Passo 1: Habilitar System-Assigned Managed Identity na Function App

**Via Portal Azure:**
1. Acesse a **Function App** no Portal Azure
2. Menu lateral → **Identity**
3. Na aba **System assigned**, defina **Status = On**
4. Clique em **Save** e confirme

**Via Azure CLI:**
```bash
az functionapp identity assign \
    --name geoloc-func-XXXXXX \
    --resource-group rg4geoloc
```

**Via Terraform:**
```hcl
resource "azurerm_linux_function_app" "main" {
  # ... outras propriedades ...

  identity {
    type = "SystemAssigned"
  }
}
```

O comando/configuração retorna o `principalId` da identidade gerenciada — anote esse valor.

#### Passo 2: Atribuir a role `Azure Maps Data Reader` à Managed Identity

A Managed Identity precisa de permissão para acessar o Azure Maps. As roles disponíveis são:

| Role | Permissão | Quando usar |
|------|-----------|-------------|
| **Azure Maps Data Reader** | Acesso somente leitura às APIs REST | **Recomendado para esta PoC** — geocodificação é leitura |
| **Azure Maps Search and Render Data Reader** | Acesso somente a Search e Render | Alternativa mais restritiva |
| **Azure Maps Data Contributor** | Leitura + escrita + exclusão | Apenas se precisar criar/editar dados |

**Via Portal Azure:**
1. Acesse o recurso **Azure Maps** no Portal
2. Menu lateral → **Access control (IAM)**
3. Clique em **+ Add** → **Add role assignment**
4. Selecione a role: **Azure Maps Data Reader**
5. Em **Members**, selecione **Managed identity**
6. Clique em **+ Select members** → selecione a Function App
7. Clique em **Review + assign**

**Via Azure CLI:**
```bash
# Obter o principalId da Function App
PRINCIPAL_ID=$(az functionapp identity show \
    --name geoloc-func-XXXXXX \
    --resource-group rg4geoloc \
    --query principalId -o tsv)

# Obter o Resource ID do Azure Maps
MAPS_ID=$(az maps account show \
    --name geoloc-maps \
    --resource-group rg4geoloc \
    --query id -o tsv)

# Atribuir a role
az role assignment create \
    --assignee "$PRINCIPAL_ID" \
    --role "Azure Maps Data Reader" \
    --scope "$MAPS_ID"
```

**Via Terraform:**
```hcl
resource "azurerm_role_assignment" "maps_reader" {
  scope                = azurerm_maps_account.main.id
  role_definition_name = "Azure Maps Data Reader"
  principal_id         = azurerm_linux_function_app.main.identity[0].principal_id
}
```

#### Passo 3: Obter o Client ID do Azure Maps

O Client ID (também chamado `x-ms-client-id`) é um GUID único da conta Azure Maps, diferente da subscription key. É necessário para autenticação via Entra ID.

**Via Portal Azure:**
1. Acesse o recurso **Azure Maps** → **Authentication**
2. Copie o **Client ID** exibido na seção Microsoft Entra ID

**Via Azure CLI:**
```bash
az maps account show \
    --name geoloc-maps \
    --resource-group rg4geoloc \
    --query properties.uniqueId -o tsv
```

#### Passo 4: Alterar o código para usar Managed Identity

Em vez de passar `subscription-key` na URL, o código passa um **Bearer token** no header `Authorization` e o **Client ID** no header `x-ms-client-id`:

```csharp
// Exemplo conceitual — NÃO implementado nesta PoC

using Azure.Identity;

public class AzureMapsService
{
    private readonly HttpClient _httpClient;
    private readonly string _clientId; // Client ID do Azure Maps (x-ms-client-id)

    public AzureMapsService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _clientId = configuration["AzureMapsClientId"];
    }

    public async Task<GeoCoordinate?> GeocodeAddressAsync(string address)
    {
        // 1. Obter token via Managed Identity
        var credential = new DefaultAzureCredential();
        var token = await credential.GetTokenAsync(
            new Azure.Core.TokenRequestContext(
                new[] { "https://atlas.microsoft.com/.default" }));

        // 2. Montar request com headers de autenticação
        var url = $"https://atlas.microsoft.com/search/address/json"
                + $"?api-version=1.0"
                + $"&query={Uri.EscapeDataString(address)}&limit=1";

        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("x-ms-client-id", _clientId);
        request.Headers.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token.Token);

        // 3. Enviar request
        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        // ... parsear resposta (igual ao código atual)
    }
}
```

**App Settings necessárias (em vez de `AzureMapsSubscriptionKey`):**
```json
{
  "AzureMapsClientId": "<CLIENT_ID_DO_AZURE_MAPS>"
}
```

#### Passo 5 (opcional): Desabilitar autenticação por Shared Key

Após migrar para Managed Identity, desabilite a autenticação por chave para máxima segurança:

**Via Azure CLI:**
```bash
az maps account update \
    --name geoloc-maps \
    --resource-group rg4geoloc \
    --disable-local-auth true
```

**Via Terraform:**
```hcl
resource "azurerm_maps_account" "main" {
  name                  = "geoloc-maps"
  resource_group_name   = azurerm_resource_group.main.name
  location              = "global"
  sku_name              = "G2"
  local_auth_enabled    = false
}
```

> **Nota:** Esta PoC utiliza Shared Key para simplicidade. A migração para Managed Identity é recomendada antes de ir para produção.

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
