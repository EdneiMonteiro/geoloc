# Arquitetura da Solução — GeoLoc

## Visão Geral

A solução GeoLoc é composta por três camadas: **mobile**, **backend serverless** e **serviços Azure**. O app captura coordenadas GPS do dispositivo, o backend consulta e geocodifica o endereço cadastrado, e calcula se o usuário está dentro do raio de 50 metros.

## Diagrama de Arquitetura

```
┌─────────────────────────────────────────────────────────────────┐
│                        iPhone (Expo Go)                         │
│                                                                 │
│  ┌──────────────┐  ┌──────────────┐  ┌───────────────────────┐  │
│  │   Câmera     │  │   GPS        │  │   UI (React Native)   │  │
│  │ (expo-camera)│  │(expo-location│  │  - Input userId       │  │
│  │              │  │              │  │  - Foto + Coordenadas │  │
│  │  Tira foto   │  │ Captura      │  │  - Resultado          │  │
│  └──────┬───────┘  │ lat/lng      │  └──────────┬────────────┘  │
│         │          └──────┬───────┘             │               │
│         └─────────────────┼─────────────────────┘               │
│                           │                                     │
│                    POST /api/validate-location                  │
│                    { userId, latitude, longitude }               │
└───────────────────────────┼─────────────────────────────────────┘
                            │ HTTPS
                            ▼
┌───────────────────────────────────────────────────────────────────┐
│                     Azure Functions (C# .NET 8)                   │
│                     Consumption Plan (Serverless)                 │
│                                                                   │
│  ┌─────────────────────────────────────────────────────────────┐  │
│  │              ValidateLocationFunction                        │  │
│  │                                                              │  │
│  │  1. Recebe userId + coordenadas GPS                          │  │
│  │  2. Consulta endereço ──────────────────────► Table Storage  │  │
│  │  3. Geocodifica endereço ───────────────────► Azure Maps     │  │
│  │  4. Calcula distância (Haversine)                            │  │
│  │  5. Retorna { isWithinRadius, distanceMeters, ... }          │  │
│  └─────────────────────────────────────────────────────────────┘  │
└────────────────┬───────────────────────────┬──────────────────────┘
                 │                           │
                 ▼                           ▼
┌────────────────────────┐    ┌──────────────────────────────┐
│   Azure Table Storage  │    │       Azure Maps Account     │
│                        │    │                              │
│  Tabela: UserAddresses │    │  Search Address API          │
│  ┌──────────────────┐  │    │  GET /search/address/json    │
│  │ PK: BR-SP        │  │    │                              │
│  │ RK: user001      │  │    │  Input:  "Av. Paulista, 1000"│
│  │ Addr: Av. Paul.. │  │    │  Output: { lat, lon }        │
│  └──────────────────┘  │    │                              │
└────────────────────────┘    └──────────────────────────────┘
```

## Camadas

### 1. Mobile (React Native / Expo)
- **Framework:** Expo SDK 54 com TypeScript
- **Responsabilidades:** Interface do usuário, captura de foto, obtenção de coordenadas GPS, chamada HTTP ao backend
- **Componentes:** `CameraView` (câmera + GPS), `ResultCard` (exibição do resultado), `apiService` (cliente HTTP)

### 2. Backend (Azure Functions)
- **Runtime:** .NET 8 Isolated Worker, Azure Functions v4
- **Hospedagem:** Consumption Plan (serverless, pay-per-execution)
- **Responsabilidades:** Orquestrar o fluxo de validação — consultar Table Storage, geocodificar via Azure Maps, calcular distância
- **Serviços:** `TableStorageService`, `AzureMapsService`, `GeoCalculationService`

### 3. Serviços Azure
- **Table Storage:** Banco NoSQL para endereços cadastrados dos usuários
- **Azure Maps:** Geocodificação (endereço textual → coordenadas geográficas)

## Comunicação entre Camadas

| Origem | Destino | Protocolo | Dados |
|--------|---------|-----------|-------|
| Mobile → Functions | HTTPS POST | JSON | `{ userId, latitude, longitude }` |
| Functions → Table Storage | Azure SDK | Azure.Data.Tables | Query por RowKey (userId) |
| Functions → Azure Maps | HTTPS GET | REST API | `/search/address/json?query=...` |
| Functions → Mobile | HTTPS Response | JSON | `{ isWithinRadius, distanceMeters, ... }` |

## Segurança

- **CORS** configurado na Function App (aceita requests do Expo em dev)
- **Azure Maps key** armazenada como app setting (não exposta ao mobile)
- **Sem autenticação** na demo — em produção, recomenda-se Azure AD/Entra ID
- **Credenciais sensíveis** (connection strings, keys) em arquivos `.gitignore`d (`local.settings.json`, `terraform.tfvars`)
