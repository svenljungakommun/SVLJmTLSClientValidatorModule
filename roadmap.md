# ROADMAP – SVLJmTLSClientValidatorModule

This document outlines upcoming features, planned improvements, and architectural goals for future releases of the SVLJ mutual TLS validation module.

---

## ✅ Design Note

All upcoming features will be **optional** and **disabled by default**.  
They can be enabled explicitly via `web.config` (`appSettings`) to maintain compatibility and operational control.

---

## ✅ Under Consideration (next minor releases)

- [ ] KeyUsage bit enforcement  
  _(Block certificates missing digitalSignature or with invalid bitmask)_

- [ ] Signature algorithm enforcement  
  _(Reject certs signed with SHA1, MD5, or other insecure algorithms)_

- [ ] Local fallback cache for CRL  
  _(Optional file-based cache if online revocation fails)_

- [ ] Configuration validation on startup  
  _(Fail-fast if required `appSettings` are missing or malformed)_

- [ ] JSON-formatted logging for SIEM/SOC  
  _(Emit validation results in structured format to logs or EventLog)_

- [ ] Structured `X-SVLJ-*` headers  
  _(Expose base64-encoded thumbprint, serial, SAN etc. in consistent format)_

- [ ] Cipher suite validation  
  _(Optional rejection of clients using insecure TLS ciphers like 3DES, RC4, export-grade suites)_

---

## 📆 Tentative Release Targets

| Feature                          | Target Version |
|----------------------------------|----------------|
| Signature algorithm checks       | 1.5            |
| JSON-formatted logging           | 1.6            |
| CRL local fallback / caching     | 1.6            |
| Configuration validation         | 1.6            |
| Cipher suite validation          | 1.7            |
