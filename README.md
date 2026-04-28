# Disclaimer

> **Notice:** Any sample scripts, code, or commands comes with the following notification.
>
> This Sample Code is provided for the purpose of illustration only and is not intended to be used in a production environment. THIS SAMPLE CODE AND ANY RELATED INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A PARTICULAR PURPOSE.
>
> We grant You a nonexclusive, royalty-free right to use and modify the Sample Code and to reproduce and distribute the object code form of the Sample Code, provided that You agree: (i) to not use Our name, logo, or trademarks to market Your software product in which the Sample Code is embedded; (ii) to include a valid copyright notice on Your software product in which the Sample Code is embedded; and (iii) to indemnify, hold harmless, and defend Us and Our suppliers from and against any claims or lawsuits, including attorneys' fees, that arise or result from the use or distribution of the Sample Code.
>
> Please note: None of the conditions outlined in the disclaimer above will supersede the terms and conditions contained within the Customers Support Services Description.

---

# GeoLoc — Validação de Localização com Azure Maps

Prova de conceito que demonstra o uso do **Azure Maps** para validação de geolocalização. O usuário tira uma foto no iPhone, o app captura as coordenadas GPS do dispositivo e verifica se ele está a menos de **50 metros** do endereço cadastrado.

## Documentação

| Documento | Descrição |
|-----------|-----------|
| [docs/arquitetura.md](docs/arquitetura.md) | Diagrama e descrição da arquitetura |
| [docs/fluxo-logico.md](docs/fluxo-logico.md) | Fluxo detalhado passo a passo |
| [docs/componentes-azure.md](docs/componentes-azure.md) | Detalhes dos 4 recursos Azure |
| [docs/apresentacao.html](docs/apresentacao.html) | Apresentação para cliente (abrir no browser) |

## Arquitetura

```
iPhone (React Native / Expo)
  ├─ Câmera → Foto
  ├─ Device Location → GPS coords (lat/lng)
  └─ HTTP POST → Azure Function
                    ├─ Consulta Table Storage (endereço do usuário)
                    ├─ Geocodifica endereço via Azure Maps Search API
                    ├─ Calcula distância (Haversine)
                    └─ Retorna: { isWithinRadius, distanceMeters }
```

### Componentes Azure

| Recurso | Finalidade |
|---------|-----------|
| **Resource Group** (`rg4geoloc`) | Container lógico para todos os recursos |
| **Storage Account** (Table Storage) | Armazena tabela `UserAddresses` com endereços cadastrados |
| **Azure Maps Account** | Geocodifica endereço textual → coordenadas (lat/lng) |
| **Azure Functions** (Consumption, .NET 8) | Backend serverless — endpoint `POST /api/validate-location` |

## Autenticação do Azure Maps

O Azure Maps suporta dois métodos de autenticação. Esta PoC usa **Shared Key** por simplicidade:

| Método | Usado nesta PoC | Recomendado para produção |
|--------|:-:|:-:|
| **Shared Key** (`subscription-key` na URL) | ✅ | |
| **Managed Identity** (Bearer token via Entra ID) | | ✅ |

### Shared Key (usado nesta PoC)

1. Criar o Azure Maps Account (Portal, CLI ou Terraform)
2. Obter a **Primary Key** em: Azure Maps → Authentication → Shared Key Authentication
3. Configurar como variável de ambiente `AzureMapsSubscriptionKey`
4. A chave é passada como `subscription-key` na URL da API

```bash
# Obter a chave via CLI
az maps account keys list --name geoloc-maps --resource-group rg4geoloc --query primaryKey -o tsv
```

### Managed Identity (recomendado para produção)

Elimina chaves estáticas — a Function App usa seu próprio token OAuth gerenciado pelo Azure:

1. **Habilitar** Managed Identity na Function App:
   ```bash
   az functionapp identity assign --name <function_app> --resource-group rg4geoloc
   ```

2. **Atribuir role** `Azure Maps Data Reader` à Managed Identity:
   ```bash
   PRINCIPAL_ID=$(az functionapp identity show --name <function_app> --resource-group rg4geoloc --query principalId -o tsv)
   MAPS_ID=$(az maps account show --name geoloc-maps --resource-group rg4geoloc --query id -o tsv)
   az role assignment create --assignee "$PRINCIPAL_ID" --role "Azure Maps Data Reader" --scope "$MAPS_ID"
   ```

3. **Obter Client ID** do Azure Maps:
   ```bash
   az maps account show --name geoloc-maps --resource-group rg4geoloc --query properties.uniqueId -o tsv
   ```

4. **No código**, usar `DefaultAzureCredential` e enviar `x-ms-client-id` + `Authorization: Bearer {token}` em vez de `subscription-key`

5. **(Opcional)** Desabilitar Shared Key:
   ```bash
   az maps account update --name geoloc-maps --resource-group rg4geoloc --disable-local-auth true
   ```

**Roles RBAC disponíveis:**

| Role | Permissão |
|------|-----------|
| `Azure Maps Data Reader` | Somente leitura — **recomendado para geocodificação** |
| `Azure Maps Search and Render Data Reader` | Apenas Search + Render |
| `Azure Maps Data Contributor` | Leitura + escrita + exclusão |

> Documentação detalhada com exemplos de código: [docs/componentes-azure.md](docs/componentes-azure.md)

## Pré-requisitos

- **Azure CLI** instalado e autenticado (`az login`)
- **Terraform** >= 1.5
- **.NET 8 SDK**
- **Node.js** >= 18
- **Expo CLI** (`npm install -g expo-cli`)
- **Expo Go** no iPhone (disponível na App Store)
- **Azurite** (opcional, para desenvolvimento local do Table Storage)

## Setup

### 1. Infraestrutura (Terraform)

```bash
cd infra
cp terraform.tfvars.example terraform.tfvars
# Edite terraform.tfvars se necessário

terraform init
terraform plan
terraform apply
```

Anote os outputs:
- `storage_connection_string` — para o backend e seed
- `azure_maps_key` — para o backend
- `function_app_url` — para o app mobile

### 2. Popular dados de teste

```bash
cd scripts

# Para Azure Storage (usar o nome da storage account do output do Terraform):
./seed-data.sh <storage_account_name>

# Para desenvolvimento local com Azurite:
./seed-data.sh --local
```

Usuários de teste disponíveis:
| userId | Endereço |
|--------|----------|
| `user001` | Av. Paulista, 1000, São Paulo, SP |
| `user002` | Av. Atlântica, 2000, Rio de Janeiro, RJ |
| `user003` | Praça da Liberdade, 1, Belo Horizonte, MG |

### 3. Backend (Azure Functions)

```bash
cd api
cp local.settings.json.example local.settings.json
# Edite local.settings.json com os valores reais:
#   - TableStorageConnectionString
#   - AzureMapsSubscriptionKey

# Restaurar pacotes e rodar localmente
dotnet restore
func start
```

Teste local:
```bash
curl -X POST http://localhost:7071/api/validate-location \
  -H "Content-Type: application/json" \
  -d '{"userId": "user001", "latitude": -23.5634, "longitude": -46.6542}'
```

### 4. Mobile (React Native / Expo)

```bash
cd mobile
npm install

# Edite services/apiService.ts e ajuste a URL do backend se necessário

npx expo start
```

- Abra o **Expo Go** no iPhone
- Escaneie o QR Code exibido no terminal
- Informe um userId (ex: `user001`)
- Tire uma foto → coordenadas GPS são capturadas
- Toque em **Validar Localização** → resultado exibido

## Estrutura do Projeto

```
geoloc/
├── infra/                          # Terraform (4 recursos Azure)
│   ├── main.tf
│   ├── variables.tf
│   ├── outputs.tf
│   └── terraform.tfvars.example
├── api/                            # Azure Functions (.NET 8 Isolated)
│   ├── GeoLoc.Functions.csproj
│   ├── Program.cs
│   ├── Functions/
│   │   └── ValidateLocationFunction.cs
│   ├── Models/
│   │   ├── UserAddress.cs
│   │   └── ValidationResult.cs
│   ├── Services/
│   │   ├── TableStorageService.cs
│   │   ├── AzureMapsService.cs
│   │   └── GeoCalculationService.cs
│   ├── host.json
│   └── local.settings.json.example
├── mobile/                         # React Native (Expo)
│   ├── App.tsx
│   ├── components/
│   │   ├── CameraView.tsx
│   │   └── ResultCard.tsx
│   ├── services/
│   │   └── apiService.ts
│   ├── app.json
│   └── package.json
├── scripts/
│   └── seed-data.sh
├── docs/                           # Documentação
│   ├── arquitetura.md
│   ├── fluxo-logico.md
│   ├── componentes-azure.md
│   └── apresentacao.html           # Apresentação para cliente
└── README.md
```

## Destaques de Código

### Chamada ao Azure Maps (Geocodificação)

O serviço `AzureMapsService` converte um endereço textual em coordenadas via **Azure Maps Search Address API**:

```csharp
// api/Services/AzureMapsService.cs

public async Task<GeoCoordinate?> GeocodeAddressAsync(string address)
{
    var encodedAddress = Uri.EscapeDataString(address);
    var url = $"https://atlas.microsoft.com/search/address/json"
            + $"?api-version=1.0"
            + $"&subscription-key={_subscriptionKey}"
            + $"&query={encodedAddress}&limit=1";

    var response = await _httpClient.GetAsync(url);
    response.EnsureSuccessStatusCode();

    var json = await response.Content.ReadFromJsonAsync<JsonElement>();
    var position = json.GetProperty("results")[0].GetProperty("position");

    return new GeoCoordinate
    {
        Latitude = position.GetProperty("lat").GetDouble(),
        Longitude = position.GetProperty("lon").GetDouble()
    };
}
```

### Validação de Raio (Fórmula de Haversine)

O `GeoCalculationService` calcula a distância geodésica entre duas coordenadas considerando a curvatura da Terra:

```csharp
// api/Services/GeoCalculationService.cs

private const double EarthRadiusMeters = 6_371_000;

public double CalculateDistanceMeters(
    double lat1, double lon1,    // GPS do dispositivo
    double lat2, double lon2)    // Coordenadas do Azure Maps
{
    var dLat = DegreesToRadians(lat2 - lat1);
    var dLon = DegreesToRadians(lon2 - lon1);

    var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
            Math.Cos(DegreesToRadians(lat1)) * Math.Cos(DegreesToRadians(lat2)) *
            Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

    var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

    return EarthRadiusMeters * c;  // Distância em metros
}
```

A validação na Function compara o resultado com o raio de 50 metros:

```csharp
// api/Functions/ValidateLocationFunction.cs

var distanceMeters = _geoCalc.CalculateDistanceMeters(
    request.Latitude, request.Longitude,       // GPS do iPhone
    addressCoords.Latitude, addressCoords.Longitude);  // Azure Maps

var isWithin = distanceMeters <= 50.0;  // Raio de 50 metros
```

## Fluxo da Aplicação

1. Usuário informa seu **userId** no app
2. Tira uma **foto** com a câmera do iPhone
3. O app captura as **coordenadas GPS** do dispositivo naquele momento
4. Ao tocar em "Validar Localização", o app envia `userId + lat/lng` para a Azure Function
5. A Function consulta o **endereço cadastrado** na Table Storage
6. A Function **geocodifica** o endereço via Azure Maps (texto → coordenadas)
7. Calcula a **distância** entre as duas coordenadas (fórmula de Haversine)
8. Retorna se o dispositivo está **dentro ou fora** do raio de 50 metros

## Deploy para Azure

```bash
cd api
dotnet publish -c Release -o ./publish
cd publish
zip -r ../deploy.zip .
az functionapp deployment source config-zip \
  --resource-group rg4geoloc \
  --name <function_app_name> \
  --src ../deploy.zip
```

Após o deploy, atualize `API_BASE_URL` em `mobile/services/apiService.ts` com a URL da Function App.
