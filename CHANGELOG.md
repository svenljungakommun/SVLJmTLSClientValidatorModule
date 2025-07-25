# Changelog – SVLJmTLSClientValidatorModule

All notable changes to this module will be documented in this file.

This project adheres to a **fail-closed Zero Trust model** and is used in IIS-hosted .NET environments requiring mutual TLS authentication.

---

## [1.4.5] – 2025-07-24

### Added
- Optional validation of client certificate thumbprint via `SVLJ_AllowedClientThumbprints`.
- New rejection reason `client-thumbprint-not-allowed` displayed via `403c.html`.

### Changed
- Updated `403c.html` to include explanation for `client-thumbprint-not-allowed`.
- Extended XML documentation in source code to reflect thumbprint validation feature.

## [1.4.4] – 2025-07-23

### Added
- Support for signature algorithm validation

## [1.4.3] – 2025-07-23

### Added
- Support for Extended Key Usage (EKU) validation via `SVLJ_AllowedEKUOids` in `web.config`.
- Parsing of EKU OIDs into `AllowedEkuOids` during `Init()`.
- Runtime validation of client certificate EKUs in `OnBeginRequest`.

### Changed
- Issuer CN comparison now uses `string.Equals(..., StringComparison.OrdinalIgnoreCase)` instead of `ToUpperInvariant()` for improved correctness.
- Documentation updated to include `SVLJ_AllowedEKUOids` and reflect current behavior.

## [1.4.2] – 2025-07-22

### Added
- Support for **configurable IP-based internal bypass** using `SVLJ_InternalBypassIPs`
- Internal requests (e.g., from `127.0.0.1`) can now skip mTLS validation if explicitly allowed

### Changed
- Certificate serial whitelist (`SVLJ_CertSerialNumbers`) is now parsed once during `Init()` instead of per request

---

## [1.4.1] – 2025-07-21

### Fixed
- Improved internal comments and formatting for clarity
- Minor formatting corrections and cleanup

### Added
- Initial implementation of `SVLJ_CertSerialNumbers` for client cert whitelist
- Strict matching of serial numbers (fail if missing or not in list)

---

## [1.4.0] – 2025-07-17

### Added
- Issuer thumbprint validation (`SVLJ_IssuerThumbprint`)
- Certificate validity window check (`NotBefore`, `NotAfter`)
- Improved CRL validation via `X509Chain`

---

## [1.3.x and earlier] – 2025 Q1–Q2

- Initial development versions under internal revision
- Core functionality: issuer CN match, PEM-based CA bundle, mTLS enforcement
- Not publicly documented

---

