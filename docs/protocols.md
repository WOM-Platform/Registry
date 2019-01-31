# Protocol details

The registry API protocol is composed of two major parts: voucher generation and payments.
Both parts are based on 2&nbsp;API methods.

## Voucher creation request

Request from *instrument* to *registry*, generating a new voucher generation request.

`POST /api/v1/voucher/create`

### Payload

```json
{
    "SourceId": "integer, unique ID of the instrument",
    "Nonce": "string, TBD",
    "Payload": "string, see below"
}
```

`Payload` is encoded in JSON, as UTF-8 string, signed with the source's private key and encrypted with the registry's public key.
Contents as follows:

```json
{
    "SourceId": "integer, same as above",
    "Nonce": "string, same as above",
    "Vouchers": [
        {
            "Latitude": "double",
            "Longitude": "double",
            "Timestamp": "string, in ISO 8601 format"
        }
    ]
}
```

### Result

```json
{
    "Payload": "string, see below"
}
```

Payload encoded in JSON, as UTF-8 string, signed with the registry's public key and encrypted with the source's public key.
Contents below:

```json
{
    "Source": "string, URL to the source",
    "NextNonce": "string, TBD",
    "Otc": "string, represents a GUID"
}
```

## Voucher redemption

Request from *pocket* to *registry*, completing the voucher generation request and returning instances of vouchers to the pocket, which can use them to process payments.

`POST /api/v1/voucher/redeem`

### Payload

```json
{
    "Payload": ""
}
```

`Payload` is a JSON-encoded object, encrypted with the *registry's public key*.
Contents of the object are shown below:

| Property | Type | Description |
| --- | --- | --- |
| `Otc` | string | Uniquely represents the voucher generation instance. Is a GUID in the current implementation. |
| `Password` | string | Short user-provided code that secures transmission of the `Otc`. 4 to 8 numeric characters. |
| `SessionKey` | string | 256-bit session key used to encrypt the response. Encoded in base64. |

### Result

```json
{
    "Payload": ""
}
```

`Payload` is a JSON-encoded object, encrypted with the *session key* specified in the request.
Encryption makes use of the 256-bit AES&nbsp;algorithm, with PKCS#7 padding.
Contents of the object are shown below:

```json
{
    "Vouchers": [ { } ]
}
```

Each voucher object has the following properties:

| Property | Type | Description |
| --- | --- | --- |
| `Id` | integer | Unique voucher ID. |
| `Secret` | string | Sequence of random bytes (16&nbsp;in current implementation), encoded in base64. |
| `Aim` | string | URL representing the voucher’s aim. |
| `Latitude` | number | |
| `Longitude` | number | |
| `Timestamp` | string | Time and date, ISO&nbsp;8601 format. |

# Payment generation

Coming soon.

# Payment processing

Coming soon.
