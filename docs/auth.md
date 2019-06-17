# Login and authentication methods

**These features are available through HTTPS only. Credentials are sent using *HTTP Basic authentication*.**

## Retrieve sources

`GET https://wom.social/api/v1/auth/sources`

Retrieves a list of sources associated with the user. Output format is as follows:

```json
{
  "sources": [
    {
      "id": 123,
      "name": "Source name",
      "url": "http://example.org",
      "publicKey": "-----BEGIN PUBLIC KEY-----\nABCDEFG\n-----END PUBLIC KEY-----\n",
      "privateKey": "-----BEGIN RSA PRIVATE KEY-----\nABCDEFG\n-----END RSA PRIVATE KEY-----\n"
    }
  ]
}
```

**Note:** `privateKey` field is set only for sources with user registration.

## Retrieve POS

`GET https://wom.social/api/v1/auth/pos`

Retrieves a list of POS instances associated with the user. Output format is as follows:

```json
{
  "pos": [
    {
      "id": 123,
      "name": "POS name",
      "url": "http://example.org",
      "publicKey": "-----BEGIN PUBLIC KEY-----\nABCDEFG\n-----END PUBLIC KEY-----\n",
      "privateKey": "-----BEGIN RSA PRIVATE KEY-----\nABCDEFG\n-----END RSA PRIVATE KEY-----\n"
    }
  ]
}
```

**Note:** `privateKey` field is set only for POS with user registration.
