# Traspasos — payloads por paso (API)

Base: `http://localhost:3030/api/v1/traspasos/tramites`

## Flujo front

1. `POST /tramites` → guardar `id`
2. Consultas Verifik (`/consultas/*`) en cliente
3. `PATCH /tramites/{id}/paso/1` … `paso/4` al pulsar **Continuar**
4. `POST /tramites/{id}/enviar` en paso 4

`GET /tramites/{id}` devuelve entidades normalizadas + `wizardState` (backup).

---

## Paso 1 — Consulta + OT + vehículo

```json
PATCH /tramites/{id}/paso/1
{
  "data": {
    "requestedProcessCode": "02",
    "transferSubtype": "Bilateral",
    "trafficSecretaryCode": "25286000",
    "trafficSecretaryName": "STRIA TTOyTTE MCPAL FUNZA",
    "trafficSecretaryCity": "FUNZA",
    "vehicle": {
      "plateLetter": "XXX",
      "plateNumber": "210",
      "plateComplete": "XXX210",
      "vehicleVinNumber": "93Y9SR333RJ563653",
      "vehicleEngineNumber": "1GRA895276",
      "vehicleChassisNumber": "JTEBU3FJ9E5052442",
      "vehicleClass": "CAMPERO",
      "vehicleBrand": "TOYOTA",
      "vehicleLine": "PRADO",
      "vehicleModel": "2014",
      "vehicleColors": "GRIS METALICO",
      "vehicleFuelType": "1",
      "vehicleBodyworkCode": "275",
      "vehicleBodyworkType": "WAGON",
      "vehicleServiceType": "1"
    },
    "verifications": [
      {
        "type": "SOAT",
        "subjectRole": "seller",
        "isValid": false,
        "snapshot": {
          "soatPolicyNumber": "80540576",
          "soatExpiryDate": "2022-03-29",
          "soatStatus": "NO VIGENTE"
        }
      },
      {
        "type": "RTM",
        "isValid": true,
        "snapshot": {
          "rtmCertificateNumber": "153888558",
          "rtmValid": "SI"
        }
      }
    ],
    "consultas": {
      "vehicle": {},
      "conductorSeller": {},
      "rnmcSeller": {}
    },
    "sellerPreview": {
      "documentType": "C",
      "documentNumber": "456456",
      "vehicleOwnerName": "JORMAN AURELIO",
      "vehicleOwnerFirstLastName": "COPETE",
      "vehicleOwnerSecondLastName": "SANCHEZ",
      "emailSeller": "jj@mail.com"
    }
  }
}
```

---

## Paso 2 — Partes

```json
PATCH /tramites/{id}/paso/2
{
  "data": {
    "parties": [
      {
        "role": "Seller",
        "documentType": "C",
        "documentNumber": "456456",
        "vehicleOwnerName": "JORMAN AURELIO",
        "vehicleOwnerFirstLastName": "COPETE",
        "vehicleOwnerSecondLastName": "SANCHEZ",
        "vehicleOwnerAddress": "CL 34 56 67",
        "vehicleOwnerCity": "Medellín - Antioquia",
        "vehicleOwnerPhone": "3131313133",
        "emailSeller": "jj@mail.com",
        "sellerValidationIdentity": false,
        "rnmcSeller": true,
        "paceAndSafeSeller": "NO",
        "inscriptionNumberSeller": "22700402"
      },
      {
        "role": "Buyer",
        "documentType": "C",
        "documentNumber": "789789",
        "vehicleBuyerName": "WILSON",
        "vehicleBuyerFirstLastName": "CASTRO",
        "emailBuyer": "jj@mail.com",
        "buyerValidationIdentity": true,
        "rnmcBuyer": false,
        "paceAndSafeBuyer": "SI"
      },
      {
        "role": "WarrantyCreditor",
        "documentType": "NIT",
        "documentNumber": "232345433",
        "fullNameOrBusinessName": "BANCOLOMBIA"
      }
    ],
    "representatives": {
      "sellerLR": {},
      "buyerLR": {}
    },
    "consultas": {
      "conductorBuyer": {},
      "rnmcBuyer": {}
    }
  }
}
```

---

## Paso 3 — Compraventa, documentos, firmas

```json
PATCH /tramites/{id}/paso/3
{
  "data": {
    "transaction": {
      "priceBuySell": 45000000,
      "dateBuySell": "2025-02-11"
    },
    "pledge": {
      "registeredPledge": false,
      "hasGarmentLifting": false
    },
    "imprints": {
      "engineSeriesImprint": "",
      "chassisSeriesImprint": "",
      "isSignedImprints": false
    },
    "documents": [
      {
        "documentType": "BUY_SELL",
        "storageKey": "s3://bucket/contrato.pdf",
        "fileName": "contrato.pdf"
      }
    ],
    "signatures": [
      {
        "role": "Seller",
        "signatureType": "ON_SCREEN",
        "signatureStorageKey": "s3://bucket/firma-vendedor.png",
        "signatureHashTransaction": "a1de21d2-0460-461b-a19d-06013ed0cc21"
      }
    ]
  }
}
```

Atajos granulares:

- `POST /tramites/{id}/documents`
- `POST /tramites/{id}/signatures`

---

## Paso 4 — Confirmación

```json
PATCH /tramites/{id}/paso/4
{
  "data": {
    "observations": "",
    "observationsBeforeRunt": "",
    "unicusHashTransaction": "uuid"
  }
}
```

Luego: `POST /tramites/{id}/enviar`

---

## Compatibilidad legacy

Los nombres del JSON antiguo (`vehicleOwner*`, `soat*`, `rtm*`, `emailSeller`, etc.) se aceptan en `data` gracias a alias en el mapper del backend.
