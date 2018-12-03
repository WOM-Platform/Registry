# Protocol details

The registry API protocol is composed of two major parts: voucher creation and payments.
Both parts are based on 2&nbsp;API methods.

## Voucher creation

### Voucher creation request

Request from *instrument* to *registry*, generating a new voucher generation request.

`POST /api/v1/voucher/create`

#### Payload

```json
{
    "SourceId": int,
    "Nonce": string,
    "Payload": string
}
```

* `SourceId` is the unique ID of the instrument,
* `Nonce` TBD
* `Payload` content (see below) encoded in JSON, as UTF-8 string, signed with the source's private key and encrypted with the registry's public key.

Content:

```json
{
    "SourceId": int, // same as above
    "Nonce": string, // same as above
    "Vouchers": [
        {
            "Latitude": double,
            "Longitude": double,
            "Timestamp": string // ISO 8601 format
        }
    ]
}
```

#### Result

```json
{
    "Payload": string
}
```

Payload encoded in JSON, as UTF-8 string, signed with the registry's public key and encrypted with the source's public key.
See below:

```json
{
    "Source": string, // URL to the source
    "NextNonce": string,
    "Otc": string
}
```
