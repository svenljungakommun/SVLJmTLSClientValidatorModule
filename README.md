# SVLJmTLSClientValidatorModule v1.4.5

**Mutual TLS (mTLS) enforcement module for ASP.NET/IIS**  
Maintainer: Svenljunga kommun  

---

## Overview

`SVLJmTLSClientValidatorModule` is a .NET `IHttpModule` that enforces mutual TLS (mTLS) client certificate validation in IIS-hosted web applications.

It validates client X.509 certificates against configurable trust policies, including issuer verification, certificate chain validation using a local CA bundle, CRL checking, and optional thumbprint enforcement. Built for secure municipal and public sector infrastructure in Zero Trust architectures.

**🔗 SVLJmTLSClientValidator**  
SVLJmTLSClientValidator is available for .NET (IIS), Java (Tomcat), and Lua (Apache2), offering identical fail-closed mTLS validation across platforms.  
[`SVLJmTLSClientValidatorModule`](https://github.com/svenljungakommun/SVLJmTLSClientValidatorModule) – .NET `IHttpModule` implementation for IIS  
[`SVLJmTLSClientValidatorFilter`](https://github.com/svenljungakommun/SVLJmTLSClientValidatorFilter) – Java Servlet Filter for Tomcat  
[`SVLJmTLSClientValidatorLUA`](https://github.com/svenljungakommun/SVLJmTLSClientValidatorLUA) – `mod_lua` implementation for Apache2

---

## Features

- 🔐 Strict mTLS enforcement on all incoming HTTPS requests
- ✅ Validation logic:
  - Ensures HTTPS and client certificate presence
  - Matches Issuer CN
  - Validates chain against PEM bundle
  - Performs CRL check via `X509Chain`
  - Enforces NotBefore and NotAfter date validity
  - Optional issuer thumbprint
  - Optional strict client certificate serial whitelist
  - Optional IP whitelist/bypass
  - Optional EKU validation
  - Optional Signature Algorithms validation
  - Optional client certificate thumbprint validation
- 📤 Certificate attributes exposed as HTTP headers:
  - `HTTP_SVLJ_SUBJECT`
  - `HTTP_SVLJ_THUMBPRINT`
  - `HTTP_SVLJ_ISSUER`
  - `HTTP_SVLJ_SERIAL`
  - `HTTP_SVLJ_VALIDFROM`
  - `HTTP_SVLJ_VALIDTO`
  - `HTTP_SVLJ_SIGNATUREALG`
- ⚙️ Configuration via `appSettings` in `web.config`
- 🚫 Fail-closed design: unauthenticated clients are redirected

---

## Compliance Alignment

This module supports security controls required by:

- **NIS2 Directive**
- **ISO/IEC 27001 & 27002**
- **GDPR (Art. 32 – Security of processing)**
- **CIS Benchmarks**
- **STIGs (US DoD)**

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
    <add key="SVLJ_InternalBypassIPs" value="127.0.0.1,::1" />
    <add key="SVLJ_AllowedEKUOids" value="1.3.6.1.5.5.7.3.2" />
    <add key="SVLJ_AllowedSignatureAlgorithms" value="sha256RSA, ecdsaWithSHA256" />
    <add key="SVLJ_AllowedClientThumbprints" value="ABC123DEF456..., ..." />
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

## Using Different `appSettings` for Subfolders or Applications

In scenarios where multiple parts of an IIS site require different mTLS policies — for example, `/admin/` and `/api/` needing different client certificate rules — you can apply separate configurations by placing individual `web.config` files in each application root or subfolder that is configured as an IIS application.

### Example Structure

```
C:\inetpub\wwwroot\mtls-site\
├── web.config                  <-- Base configuration
├── admin\
│   └── web.config              <-- Separate appSettings for /admin/
├── api\
│   └── web.config              <-- Separate appSettings for /api/
```

### Example `web.config` for `/admin/`

```xml
<configuration>
  <appSettings>
    <add key="SVLJ_IssuerName" value="SVLJ Admin CA" />
    <add key="SVLJ_CertSerialNumbers" value="1234567890ABCDEF,AA11BB22CC33" />
    <add key="SVLJ_ErrorRedirectUrl" value="/admin/error/403c.html" />
  </appSettings>
</configuration>
```

> 🧩 Make sure each folder is defined as an IIS **application** (not just a physical directory) for its `web.config` to be processed correctly.

---

### Notes

* Each IIS application runs in its own app domain and initializes the module independently.
* Global settings from the root `web.config` do **not cascade** to sub-applications.
* Shared CA bundles and binaries can still be reused across applications.

---

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

| Code                               | Description                         |
|------------------------------------|-------------------------------------|
| `missing-cert`                     | No certificate presented            |
| `issuer-name-mismatch`             | Issuer CN does not match            |
| `issuer-not-trusted`               | Issuer thumbprint mismatch          |
| `crl-check-failed`                 | Revocation check failed             |
| `cert-expired`                     | Certificate is expired              |
| `cert-notyetvalid`                 | Certificate is not yet valid        |
| `validation-error`                 | Internal error during validation    |
| `serial-mismatch`                  | Serial number mismatch              |
| `eku-missing`                      | EKU was required but none found     |
| `eku-not-allowed`                  | EKU was required but none matched   |
| `sigalg-not-allowed`               | Signature algorithm is not allowed  |
| `client-thumbprint-not-allowed`    | Client thumbprint mismatch          |
| `insecure-connection`              | Request was not made over HTTPS     |

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

### Curl

```bash
curl --cert client.crt --key client.key https://your-app
```
