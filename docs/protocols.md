# Protocol details

The registry API protocol is composed of two major parts: voucher generation and payments.
Both parts are based on 2&nbsp;API methods.

## Voucher generation and redemption

### Voucher creation request

Request from *instrument* to *registry*, generating a new voucher generation request.

`POST /api/v1/voucher/create`

#### Payload

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

#### Result

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

### Voucher redemption

Request from *pocket* to *registry*, completing the voucher generation request and returning instances of vouchers to the pocket, which can use them to process payments.

`POST /api/v1/voucher/redeem`

#### Payload

```json
{
    "Payload": "string, see below"
}
```

`Payload` content encoded in JSON, as UTF-8 string, encrypted with the registry's public key.
Contents below:

```json
{
    "Otc": "string, represents the redemption ID",
    "Password": "string, short user-provided code [4–8 characters, numeric]",
    "SessionKey": "string, base64-encoded session key [32 bytes]"
}
```

#### Result

```json
{
    "Payload": "string"
}
```

The payload is encoded in JSON, as a UTF-8 string, encrypted with the *session key* specified in the request.
Encryption makes use of the 256-bit AES&nbsp;algorithm, with PKCS#7 padding.

```json
{
    "Vouchers": [
        {
            "Id": "integer, unique",
            "Secret": "string, base64-encoded random bytes",
            "Aim": "string, URL to the voucher aim",
            "Latitude": "double",
            "Longitude": "double",
            "Timestamp": "string, in ISO 8601 format"
        }
    ]
}
```

## Payment generation and processing

Coming soon.
