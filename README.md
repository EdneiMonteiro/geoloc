# GeoLoc — Validação de Localização com Azure Maps

## Visão Geral

Este repositório contém código de exemplo / prova de conceito (PoC) com o objetivo de demonstrar como implementar validação de geolocalização com Azure Maps, utilizando Azure Functions (.NET 8), React Native (Expo), Terraform e Azure Table Storage.

Este projeto foi criado para fins de aprendizado, avaliação e experimentação.

## Aviso Importante

Este repositório contém **código de exemplo e não é destinado para uso em produção**.

Antes de utilizar qualquer parte deste projeto em um ambiente produtivo ou crítico, é essencial revisar, validar, proteger e adaptar o código conforme os requisitos da sua organização, incluindo:

- Segurança
- Escalabilidade
- Confiabilidade
- Monitoramento
- Observabilidade
- Custos
- Conformidade

Leia também:

- [DISCLAIMER.md](./DISCLAIMER.md)
- [SUPPORT.md](./SUPPORT.md)

## O que este exemplo demonstra

- Geocodificação de endereços via Azure Maps Search Address API
- Validação de proximidade (raio de 50m) usando fórmula de Haversine
- App mobile (React Native / Expo) com captura de foto e GPS
- Backend serverless com Azure Functions (.NET 8 Isolated)
- Armazenamento de endereços cadastrados via Azure Table Storage
- Infraestrutura como código com Terraform
- Autenticação com Shared Key (PoC) e orientações para Managed Identity (produção)

## Pré-requisitos

- Azure CLI instalado e autenticado (`az login`)
- Terraform >= 1.5
- .NET 8 SDK
- Node.js >= 18
- Expo CLI (`npm install -g expo-cli`)
- Expo Go no iPhone (disponível na App Store)
- Azurite (opcional, para desenvolvimento local)

## Como iniciar

1. Clone este repositório
2. Provisione a infraestrutura:
   ```bash
   cd infra
   cp terraform.tfvars.example terraform.tfvars
   terraform init && terraform apply
   ```
3. Popular dados de teste:
   ```bash
   cd scripts
   ./seed-data.sh <storage_account_name>
   ```
4. Configure e execute o backend:
   ```bash
   cd api
   cp local.settings.json.example local.settings.json
   dotnet restore && func start
   ```
5. Configure e execute o mobile:
   ```bash
   cd mobile
   npm install && npx expo start
   ```
6. Execute em ambiente não produtivo
7. Valide o comportamento antes de qualquer adaptação

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

| Recurso | Finalidade |
|---------|-----------|
| **Resource Group** (`rg4geoloc`) | Container lógico para todos os recursos |
| **Storage Account** (Table Storage) | Armazena tabela `UserAddresses` com endereços cadastrados |
| **Azure Maps Account** | Geocodifica endereço textual → coordenadas (lat/lng) |
| **Azure Functions** (Consumption, .NET 8) | Backend serverless — endpoint `POST /api/validate-location` |

## Documentação

| Documento | Descrição |
|-----------|-----------|
| [docs/arquitetura.md](docs/arquitetura.md) | Diagrama e descrição da arquitetura |
| [docs/fluxo-logico.md](docs/fluxo-logico.md) | Fluxo detalhado passo a passo |
| [docs/componentes-azure.md](docs/componentes-azure.md) | Detalhes dos 4 recursos Azure |
| [docs/apresentacao.html](docs/apresentacao.html) | Apresentação para cliente (abrir no browser) |

## Suporte

Este projeto **não possui SLA nem suporte oficial**.

Veja [SUPPORT.md](./SUPPORT.md) para detalhes.

## Aviso Legal

O uso deste projeto está sujeito aos termos descritos em [DISCLAIMER.md](./DISCLAIMER.md).

## Contribuições

Contribuições podem ser aceitas a critério do mantenedor.

## Marcas Registradas (Trademarks)

Os nomes e serviços da Microsoft são utilizados apenas para fins descritivos.

Este projeto **não é afiliado, endossado ou suportado oficialmente pela Microsoft**.

O uso de marcas da Microsoft não deve sugerir qualquer tipo de parceria ou suporte oficial.
