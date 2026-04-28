# Fluxo Lógico — GeoLoc

## Fluxo Principal (Happy Path)

```
┌──────────┐     ┌──────────┐     ┌──────────┐     ┌──────────┐     ┌──────────┐
│  Usuário │     │  Mobile  │     │ Azure    │     │  Table   │     │  Azure   │
│ (iPhone) │     │  App     │     │ Function │     │ Storage  │     │  Maps    │
└────┬─────┘     └────┬─────┘     └────┬─────┘     └────┬─────┘     └────┬─────┘
     │                │                │                │                │
     │ 1. Informa     │                │                │                │
     │    userId      │                │                │                │
     │───────────────►│                │                │                │
     │                │                │                │                │
     │ 2. Tira foto   │                │                │                │
     │───────────────►│                │                │                │
     │                │                │                │                │
     │                │ 3. Captura GPS │                │                │
     │                │    (lat/lng)   │                │                │
     │                │───────┐        │                │                │
     │                │◄──────┘        │                │                │
     │                │                │                │                │
     │ 4. Exibe foto  │                │                │                │
     │    + coords    │                │                │                │
     │◄───────────────│                │                │                │
     │                │                │                │                │
     │ 5. Toca em     │                │                │                │
     │    "Validar"   │                │                │                │
     │───────────────►│                │                │                │
     │                │                │                │                │
     │                │ 6. POST        │                │                │
     │                │ /validate      │                │                │
     │                │ {userId,       │                │                │
     │                │  lat, lng}     │                │                │
     │                │───────────────►│                │                │
     │                │                │                │                │
     │                │                │ 7. Query       │                │
     │                │                │ RowKey=userId  │                │
     │                │                │───────────────►│                │
     │                │                │                │                │
     │                │                │ 8. Endereço    │                │
     │                │                │◄───────────────│                │
     │                │                │                │                │
     │                │                │ 9. GET /search/│                │
     │                │                │ address/json   │                │
     │                │                │ ?query=endereço│                │
     │                │                │───────────────────────────────►│
     │                │                │                │                │
     │                │                │ 10. { lat, lon }               │
     │                │                │◄──────────────────────────────│
     │                │                │                │                │
     │                │                │ 11. Haversine  │                │
     │                │                │ distance =     │                │
     │                │                │ calc(GPS, addr)│                │
     │                │                │───────┐        │                │
     │                │                │◄──────┘        │                │
     │                │                │                │                │
     │                │ 12. Response   │                │                │
     │                │ {isWithinRadius│                │                │
     │                │  distanceMeters│                │                │
     │                │  ...}          │                │                │
     │                │◄───────────────│                │                │
     │                │                │                │                │
     │ 13. Exibe      │                │                │                │
     │    ResultCard  │                │                │                │
     │    ✅ ou ❌     │                │                │                │
     │◄───────────────│                │                │                │
     │                │                │                │                │
```

## Detalhamento dos Passos

### Passos 1-4: Captura (Mobile)
1. Usuário digita o **userId** (ex: `user001`)
2. Toca em **"Tirar Foto"** → abre a câmera nativa do iPhone
3. Ao tirar a foto, o app solicita e captura as **coordenadas GPS** do dispositivo via `expo-location` com alta precisão (`Accuracy.High`)
4. A tela exibe a **foto** e as **coordenadas capturadas**

### Passo 5: Ação do Usuário
5. Usuário toca em **"Validar Localização"** → dispara a chamada ao backend

### Passo 6: Requisição HTTP
6. O app faz um `POST` para a Azure Function com:
```json
{
  "userId": "user001",
  "latitude": -15.831677,
  "longitude": -48.054793
}
```

### Passos 7-8: Consulta ao Table Storage
7. A Function consulta a tabela `UserAddresses` buscando por `RowKey = userId`
8. Retorna o registro com o endereço completo: `"Av. Paulista, 1000, São Paulo, SP, 01310-100, Brazil"`

### Passos 9-10: Geocodificação via Azure Maps
9. A Function chama a **Azure Maps Search Address API**:
```
GET https://atlas.microsoft.com/search/address/json
    ?api-version=1.0
    &subscription-key=***
    &query=Av.%20Paulista%2C%201000%2C%20São%20Paulo...
    &limit=1
```
10. Azure Maps retorna as coordenadas do endereço:
```json
{ "position": { "lat": -23.5650242, "lon": -46.6519791 } }
```

### Passo 11: Cálculo de Distância (Haversine)
11. A Function calcula a distância geodésica entre:
    - **Coordenadas do dispositivo** (GPS do iPhone)
    - **Coordenadas do endereço** (retornadas pelo Azure Maps)
    
    Usando a fórmula de Haversine que considera a curvatura da Terra.

### Passos 12-13: Resposta
12. A Function retorna o resultado:
```json
{
  "isWithinRadius": false,
  "distanceMeters": 872333.14,
  "radiusMeters": 50,
  "registeredAddress": "Av. Paulista, 1000, São Paulo, SP, 01310-100, Brazil",
  "deviceCoordinates": { "latitude": -15.831677, "longitude": -48.054793 },
  "addressCoordinates": { "latitude": -23.5650242, "longitude": -46.6519791 }
}
```
13. O app exibe o **ResultCard** com o resultado visual (✅ dentro / ❌ fora)

## Fluxos de Erro

| Cenário | Onde ocorre | Resposta |
|---------|-------------|----------|
| userId não informado | Function | `400 Bad Request: userId is required` |
| Usuário não encontrado | Table Storage | `404 Not Found: No registered address found` |
| Endereço não geocodificável | Azure Maps | `422 Unprocessable: Could not geocode address` |
| Falha de rede (mobile) | App | Alert com mensagem de erro |
