# SVLJmTLSClientValidatorModule v1.4.1

**Mutual TLS (mTLS) enforcement module for ASP.NET/IIS**  
Maintainer: Svenljunga kommun  

---

## Overview

`SVLJmTLSClientValidatorModule` is a .NET `IHttpModule` that enforces mutual TLS (mTLS) client certificate validation in IIS-hosted web applications.

It validates client X.509 certificates against configurable trust policies, including issuer verification, certificate chain validation using a local CA bundle, CRL checking, and optional thumbprint enforcement. Built for secure municipal and public sector infrastructure in Zero Trust architectures.

---

## Features

- 🔐 Strict mTLS enforcement on all incoming HTTPS requests
- ✅ Validation logic:
  - Ensures HTTPS and client certificate presence
  - Matches Issuer CN (`SVLJ_IssuerName`)
  - Optional issuer thumbprint (`SVLJ_IssuerThumbprint`)
  - Validates chain against PEM bundle (`SVLJ_CABundlePath`)
  - Performs CRL check via `X509Chain`
  - Enforces NotBefore and NotAfter date validity
  - Optional strict client certificate serial whitelist (SVLJ_CertSerialNumbers)
- 📤 Certificate attributes exposed as HTTP headers:
  - `HTTP_SVLJ_SUBJECT`
  - `HTTP_SVLJ_ISSUER`
  - `HTTP_SVLJ_SERIAL`
  - `HTTP_SVLJ_VALIDFROM`
  - `HTTP_SVLJ_VALIDTO`
  - `HTTP_SVLJ_SIGNATUREALG`
- ⚙️ Configuration via `appSettings` in `web.config`
- 🚫 Fail-closed design: unauthenticated clients are redirected

---

## Directory Structure

```

C:\inetpub\wwwroot\mtls-site
├── bin
│   └── SVLJ.Security.dll
├── error
│   └── 403c.html
├── Web.config
C:\inetpub\wwwroot\mTLSBundles
└── ca-bundle.pem

````

---

## Example Configuration (`web.config`)

```xml
<configuration>
  <appSettings>
    <add key="SVLJ_CertSerialNumbers" value="12AB34CD56EF7890,ABCDE12345FEDCBA" />
    <add key="SVLJ_IssuerName" value="Some CA" />
    <add key="SVLJ_IssuerThumbprint" value="ABCDEF123456..." />
    <add key="SVLJ_CABundlePath" value="C:\inetpub\wwwroot\mTLSBundles\ca-bundle.pem" />
    <add key="SVLJ_ErrorRedirectUrl" value="/error/403c.html" />
  </appSettings>

  <system.webServer>
    <modules runAllManagedModulesForAllRequests="true">
      <add name="SVLJmTLSClientValidatorModule"
           type="SVLJ.Security.SVLJmTLSClientValidatorModule" />
    </modules>
  </system.webServer>

</configuration>
````

---

## Enabling Client Certificate Negotiation

**Option 1 – Registry**

```reg
[HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\HTTP\Parameters\SslBindingInfo\0.0.0.0:443]
"DefaultFlags"=dword:00000002
```

**Option 2 – netsh**

```bash
netsh http update sslcert ipport=0.0.0.0:443 certstorename=MY certhash=<CERTTHUMBPRINT> appid="{00112233-4455-6677-8899-AABBCCDDEEFF}" clientcertnegotiation=enable
```

---

## IIS Configuration

```bash
appcmd set config "mtls-site" -section:system.webServer/security/access /sslFlags:"Ssl,SslNegotiateCert" /commit:apphost
```

---

## Error Handling

Redirects unauthorized requests to:

```
/error/403c.html?reason=<code>
```

### Reason codes

| Code                   | Description                      |
| ---------------------- | -------------------------------- |
| `missing-cert`         | No certificate presented         |
| `issuer-name-mismatch` | Issuer CN does not match         |
| `issuer-not-trusted`   | Thumbprint mismatch              |
| `crl-check-failed`     | Revocation check failed          |
| `expired-cert`         | Certificate is expired           |
| `cert-notyetvalid`     | Certificate is not yet valid     |
| `validation-error`     | Internal error during validation |
| `serial-mismatch`      | Serial number mismatch           |
| `insecure-connection`  | Request was not made over HTTPS  |

---

## Testing

### PowerShell

```powershell
Invoke-WebRequest -Uri "https://your-app" -Certificate (Get-Item Cert:\CurrentUser\My\<THUMBPRINT>)
```

### OpenSSL

```bash
openssl s_client -connect your-app:443 -cert client.crt -key client.key -CAfile ca-bundle.pem
```
