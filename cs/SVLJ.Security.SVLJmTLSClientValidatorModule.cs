using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Web;

/// <summary>
/// SVLJ.Security – Namespace for certificate-based security infrastructure in .NET applications.
///
/// This namespace defines components for enforcing secure communication using X.509 certificates and mutual TLS (mTLS),
/// focusing on strong client identity validation, cryptographic trust chains, and Zero Trust enforcement at the web tier.
///
/// It is intended for use in high-assurance environments such as municipal, governmental, and critical infrastructure systems,
/// where precise control over certificate validation and policy enforcement is essential.
///
/// Core responsibilities:
/// - Encapsulating reusable logic for certificate inspection and trust evaluation
/// - Managing certificate authority bundles and issuer constraints
/// - Integrating mTLS validation into IIS-hosted .NET applications
/// - Supporting configuration via `appSettings` in `web.config`
///
/// The namespace enforces a "fail-closed" posture and is designed for extensibility, auditability, and policy compliance.
///
/// Typical deployment targets include internal administrative services, PKI-protected APIs, and segmented SDN architectures.
///
/// Maintainer: Svenljunga kommun
/// </summary>

namespace SVLJ.Security
{
    /// <summary>
    /// SVLJmTLSClientValidatorModule – An ASP.NET IHttpModule for strict mutual TLS (mTLS) client certificate validation in IIS.
    ///
    /// This module enforces mTLS by intercepting HTTPS requests and validating client X.509 certificates
    /// against a configurable security policy.
    ///
    /// The module performs the following:
    /// - Ensures the connection is HTTPS and a client certificate is present
    /// - Verifies that the certificate's issuer matches a configured Common Name (CN)
    /// - Loads a PEM-formatted CA bundle from disk and validates the chain against it
    /// - Performs CRL (Certificate Revocation List) checks using X509Chain
    /// - Optionally compares the issuer certificate’s thumbprint against a configured value
    /// - Validates that the client certificate is within its valid time window
    /// - Exposes certificate attributes via custom HTTP headers (`HTTP_SVLJ_*`) to downstream components
    /// - Redirects unauthorized requests to a configured error URL with a reason code
    ///
    /// Configuration is handled via `appSettings` in `web.config`:
    /// - SVLJ_IssuerName           		= (Required) Issuer CN (e.g. "SVLJ ADM Issuing CA v1")
    /// - SVLJ_CABundlePath         		= (Required) Full path to a PEM file containing trusted CA certificates
    /// - SVLJ_ErrorRedirectUrl     		= (Required) URL to redirect unauthorized clients (default: /error/403c.html)
    /// - SVLJ_IssuerThumbprint     		= (Optional) SHA-1 thumbprint of the trusted issuer certificate  (SHA1)
    /// - SVLJ_CertSerialNumbers    		= (Optional) Comma-separated list of allowed client certificate serial numbers
    /// - SVLJ_InternalBypassIPs    		= (Optional) Comma-separated list of IPs to bypass mTLS validation
    /// - SVLJ_AllowedEKUOids	    		= (Optional) Comma-separated list of allowed EKUs
    /// - SVLJ_AllowedSignatureAlgorithms	= (Optional) Comma-separated list of allowed Signature Algorithms
    /// - SVLJ_AllowedClientThumbprints		= (Optional) Comma-separated list of allowed client certificate thumbprints (SHA1)
    ///
    /// Requests that do not meet the policy are rejected before application logic is invoked.
    ///
    /// Design principle: "Fail closed" – only explicitly trusted clients are allowed through.
    ///
    /// Typical use cases:
    /// - Securing internal or administrative web applications
    /// - Enforcing mTLS authentication in environments integrated with AD CS or custom PKI
    /// - Zero Trust infrastructure in segmented SDN networks
    ///
    /// Recommended environment: Windows Server with IIS 10+, .NET Framework 4.7.2+
    ///
    /// Author: Abdulaziz Almazrli / Odd-Arne Haraldsen
    /// Version: 1.4.5
    /// Updated: 2025-07-24
    /// </summary>

    public class SVLJmTLSClientValidatorModule : IHttpModule
    {
    	// Set global parameters
	private static string RequiredCertSerialNumbers;
        private static string RequiredIssuerName;
        private static string RequiredIssuerThumbprint;
        private static string CABundlePath;
        private static string ErrorPageUrl;
	private static string bypassList;
 	private static string ekuConfig;
  	private static string signatureAlgsSetting;
   	private static string allowedClientThumbprintsSetting;
	private static readonly HashSet<string> AllowedCertSerials = new HashSet<string>();
 	private static readonly HashSet<string> InternalBypassIPs = new HashSet<string>();
        private static readonly List<X509Certificate2> TrustedIssuers = new List<X509Certificate2>();
	private static readonly HashSet<string> AllowedEkuOids = new HashSet<string>();
 	private static readonly HashSet<string> AllowedSignatureAlgs = new HashSet<string>();
  	private static readonly HashSet<string> AllowedClientThumbprints = new HashSet<string>();
        private static readonly object IssuerLock = new object();
        public void Init(HttpApplication context)
        {
	    RequiredCertSerialNumbers		= ConfigurationManager.AppSettings["SVLJ_CertSerialNumbers"];
            RequiredIssuerName			= ConfigurationManager.AppSettings["SVLJ_IssuerName"];
            RequiredIssuerThumbprint		= ConfigurationManager.AppSettings["SVLJ_IssuerThumbprint"]?.Replace(" ", "").ToUpperInvariant();
            CABundlePath			= ConfigurationManager.AppSettings["SVLJ_CABundlePath"];
            ErrorPageUrl			= ConfigurationManager.AppSettings["SVLJ_ErrorRedirectUrl"] ?? "/error/403c.html";
	    bypassList				= ConfigurationManager.AppSettings["SVLJ_InternalBypassIPs"];
	    ekuConfig				= ConfigurationManager.AppSettings["SVLJ_AllowedEKUOids"];
     	    signatureAlgsSetting		= ConfigurationManager.AppSettings["SVLJ_AllowedSignatureAlgorithms"];
	    allowedClientThumbprintsSetting 	= ConfigurationManager.AppSettings["SVLJ_AllowedClientThumbprints"];

            // Enumerate RequiredCertSerialNumbers
            if (!string.IsNullOrWhiteSpace(RequiredCertSerialNumbers))
            {
                AllowedCertSerials.Clear();
                AllowedCertSerials.UnionWith(
                    RequiredCertSerialNumbers
                        .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => s.Trim().ToUpperInvariant())
                );
            }

            /// Enumerate bypassList
	    if (!string.IsNullOrWhiteSpace(bypassList))
            {
                InternalBypassIPs.UnionWith(
                    bypassList.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                              .Select(ip => ip.Trim()));
            }

            /// Enumerate ekuConfig
	     if (!string.IsNullOrWhiteSpace(ekuConfig))
	     {
		 AllowedEkuOids.UnionWith(
		        ekuConfig.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
		                    .Select(s => s.Trim()));
	     }

            /// Enumerate signatureAlgsSetting
	     if (!string.IsNullOrWhiteSpace(signatureAlgsSetting))
	     {
		    AllowedSignatureAlgs.UnionWith(
		        signatureAlgsSetting
		            .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
		            	.Select(a => a.Trim().ToLowerInvariant()));
	     }

            /// Enumerate allowedClientThumbprintsSetting
	     if (!string.IsNullOrWhiteSpace(allowedClientThumbprintsSetting))
	     {
		    AllowedClientThumbprints.UnionWith(
		        allowedClientThumbprintsSetting
		            .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
		            .Select(tp => tp.Trim().ToUpperInvariant()));
	     }
     
            LoadCABundle(CABundlePath);
            context.BeginRequest += OnBeginRequest;
        }
        /// <summary>
        /// Loads all certificates from a PEM bundle (one or more certificates in text format).
        /// </summary>
        private void LoadCABundle(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                throw new FileNotFoundException("CA bundle file not found: " + path);
            string pem = File.ReadAllText(path);
            var certs = System.Text.RegularExpressions.Regex.Split(pem, "-----END CERTIFICATE-----")
                .Where(s => s.Contains("-----BEGIN CERTIFICATE-----"))
                .Select(s => s.Trim() + "\n-----END CERTIFICATE-----");
            foreach (var certPem in certs)
            {
                string base64 = System.Text.RegularExpressions.Regex.Replace(certPem, "-----.*?-----", "", System.Text.RegularExpressions.RegexOptions.Singleline)
                                     .Replace("\n", "").Replace("\r", "");
                byte[] raw = Convert.FromBase64String(base64);
                lock (IssuerLock)
                {
                    TrustedIssuers.Add(new X509Certificate2(raw));
                }
            }
        }
        /// <summary>
        /// Executed on every HTTP request. Validates the client certificate if present.
        /// </summary>
        private void OnBeginRequest(object sender, EventArgs e)
        {
            var app = (HttpApplication)sender;
            var request = app.Context.Request;
	    string clientIp = request.UserHostAddress;

     	    // Internal bypass (e.g. localhost, 127.0.0.1)
            if (InternalBypassIPs.Contains(clientIp))
                return;
		
            if (!request.IsSecureConnection)
            {
                Redirect(app, "insecure-connection");
                return;
            }
            var path = request.Url.AbsolutePath.ToLowerInvariant();
            if (path.StartsWith("/error")) return; // Skip redirect loops
            var cert = request.ClientCertificate;
            if (!cert.IsPresent || cert.Certificate == null)
            {
                Redirect(app, "missing-cert");
                return;
            }
            try
            {
                var clientCert = new X509Certificate2(cert.Certificate);
				var now = DateTime.UtcNow;
                // Step 1: Expose cert info via HTTP headers
                var sv = app.Context.Request.ServerVariables;
                sv.Set("HTTP_SVLJ_SUBJECT", clientCert.Subject);
		sv.Set("HTTP_SVLJ_THUMBPRINT", clientCert.Thumbprint?.Replace(" ", "").ToUpperInvariant());
                sv.Set("HTTP_SVLJ_ISSUER", clientCert.Issuer);
                sv.Set("HTTP_SVLJ_SERIAL", clientCert.SerialNumber);
                sv.Set("HTTP_SVLJ_VALIDFROM", clientCert.NotBefore.ToString("s"));
                sv.Set("HTTP_SVLJ_VALIDTO", clientCert.NotAfter.ToString("s"));
                sv.Set("HTTP_SVLJ_SIGNATUREALG", clientCert.SignatureAlgorithm.FriendlyName);
 
                // Step 2: Check expected Issuer CN
                var issuerName = new X500DistinguishedName(clientCert.Issuer).Name;
		if (!string.Equals(issuerName, $"CN={RequiredIssuerName}", StringComparison.OrdinalIgnoreCase))
                {
                    Redirect(app, "issuer-name-mismatch");
                    return;
                }
                // Step 3: Validate certificate chain including CRL
                if (!ValidateChain(clientCert, out var issuerCert))
                {
                    Redirect(app, "crl-check-failed");
                    return;
                }
                // Step 4: Check optional issuer thumbprint
                if (!string.IsNullOrWhiteSpace(RequiredIssuerThumbprint) &&
                    !issuerCert.Thumbprint.Replace(" ", "").ToUpperInvariant().Equals(RequiredIssuerThumbprint))
                {
                    Redirect(app, "issuer-not-trusted");
                    return;
                }
                // Step 5: Check certificate validity window
                if (clientCert.NotAfter.ToUniversalTime() < now)
                {
                    Redirect(app, "expired-cert");
                    return;
                }
 
                if (clientCert.NotBefore.ToUniversalTime() > now)
                {
                    Redirect(app, "cert-notyetvalid");
                    return;
                }

		// Step 6: Check optional strict SerialNumber whitelist
  		if (AllowedCertSerials.Count > 0)
		{
		    var serial = clientCert.SerialNumber?.Trim().ToUpperInvariant();
		    if (string.IsNullOrWhiteSpace(serial) || !AllowedCertSerials.Contains(serial))
		    {
		        Redirect(app, "serial-mismatch");
		        return;
		    }
		}

  		// Step 7: Optional EKU enforcement
		if (AllowedEkuOids.Count > 0)
		{
		    var ekuExt = clientCert.Extensions
		        .OfType<X509EnhancedKeyUsageExtension>()
		        .FirstOrDefault();
		
		    if (ekuExt == null || ekuExt.EnhancedKeyUsages == null || ekuExt.EnhancedKeyUsages.Count == 0)
		    {
		        Redirect(app, "eku-missing");
		        return;
		    }
		
		    var certOids = ekuExt.EnhancedKeyUsages
		        .Cast<Oid>()
		        .Select(oid => oid.Value)
		        .ToHashSet();
		
		    if (!certOids.Overlaps(AllowedEkuOids))
		    {
		        Redirect(app, "eku-not-allowed");
		        return;
		    }
		}

  		// Step 8: Optional Signature Algorithms enforcement
  		if (AllowedSignatureAlgs.Count > 0)
		{
		    var sigAlg = clientCert.SignatureAlgorithm?.FriendlyName?.ToLowerInvariant();
		    if (string.IsNullOrWhiteSpace(sigAlg) || !AllowedSignatureAlgs.Contains(sigAlg))
		    {
		        Redirect(app, "sigalg-not-allowed");
		        return;
		    }
		}

  		// Step 9: Optional Client Thumbprint enforcement
		if (AllowedClientThumbprints.Count > 0)
		{
		    var thumbprint = clientCert.Thumbprint?.Replace(" ", "").ToUpperInvariant();
		    if (string.IsNullOrWhiteSpace(thumbprint) || !AllowedClientThumbprints.Contains(thumbprint))
		    {
		        Redirect(app, "client-thumbprint-not-allowed");
		        return;
		    }
		}
 
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError("mTLS validation error: " + ex);
                Redirect(app, "validation-error");
            }
        }
        /// <summary>
        /// Validates the certificate chain using the locally loaded CA bundle.
        /// </summary>
        private bool ValidateChain(X509Certificate2 clientCert, out X509Certificate2 issuerCert)
        {
            issuerCert = null;
            using (var chain = new X509Chain())
            {
                chain.ChainPolicy.VerificationFlags = X509VerificationFlags.NoFlag;
                chain.ChainPolicy.RevocationMode = X509RevocationMode.Online;
                chain.ChainPolicy.RevocationFlag = X509RevocationFlag.EntireChain;
                chain.ChainPolicy.ExtraStore.AddRange(TrustedIssuers.ToArray());
                bool valid = chain.Build(clientCert);
                if (!valid || chain.ChainElements.Count < 2)
                    return false;
                issuerCert = chain.ChainElements[1].Certificate;
                return true;
            }
        }
        /// <summary>
        /// Redirects the user to an error page with an optional reason code.
        /// </summary>
        private void Redirect(HttpApplication app, string reasonCode)
        {
            string url = ErrorPageUrl;
            if (!string.IsNullOrWhiteSpace(reasonCode))
                url += $"?reason={HttpUtility.UrlEncode(reasonCode)}";
            app.Context.Response.Clear();
            app.Context.Response.StatusCode = 302;
            app.Context.Response.RedirectLocation = url;
            app.CompleteRequest();
        }
        public void Dispose() { }
    }
}
