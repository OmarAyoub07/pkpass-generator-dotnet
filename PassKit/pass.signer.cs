using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;

namespace Passkit
{
    public static class PassSigner
    {
        /// <summary>
        /// Create a detached PKCS#7 (CMS) signature over manifest.json and write it as "signature".
        /// Uses built-in PEM loaders only (no helpers), and includes Apple WWDR intermediate.
        /// </summary>
        public static string Sign(
            string passFolderPath,
            string certPemPath,   // e.g., ./secrets/pass_cert.pem
            string keyPemPath,    // e.g., ./secrets/pass_key.pem
            string wwdrPemPath)   // e.g., ./certs/Apple_WWDR.pem
        {
            if (!Directory.Exists(passFolderPath))
                throw new DirectoryNotFoundException(passFolderPath);

            var manifestPath = Path.Combine(passFolderPath, "manifest.json");
            if (!File.Exists(manifestPath))
                throw new FileNotFoundException("manifest.json not found", manifestPath);

            // Leaf certificate + private key (built-in overload binds them together)
            X509Certificate2 certificate = X509Certificate2.CreateFromPemFile(certPemPath, keyPemPath);
            if (!certificate.HasPrivateKey)
                throw new CryptographicException("Loaded certificate does not have a private key.");

            // Apple WWDR intermediate
            X509Certificate2 wwdr = X509CertificateLoader.LoadCertificateFromFile(wwdrPemPath);

            // Prepare detached CMS over manifest.json
            byte[] manifestBytes = File.ReadAllBytes(manifestPath);
            var content = new ContentInfo(manifestBytes);
            var signedCms = new SignedCms(content, detached: true);

            // Signer: include end-entity (leaf) only; we’ll add WWDR explicitly
            var signer = new CmsSigner(SubjectIdentifierType.IssuerAndSerialNumber, certificate)
            {
                IncludeOption = X509IncludeOption.EndCertOnly,
                DigestAlgorithm = new Oid("2.16.840.1.101.3.4.2.1") // SHA-256 for CMS
            };
            signer.SignedAttributes.Add(new Pkcs9SigningTime(DateTime.UtcNow));

            // Add WWDR so iOS can build the chain: leaf (from IncludeOption) + WWDR (here)
            signedCms.Certificates.Add(wwdr);

            // Compute and write signature
            signedCms.ComputeSignature(signer);
            var signaturePath = Path.Combine(passFolderPath, "signature");
            File.WriteAllBytes(signaturePath, signedCms.Encode());

            return signaturePath;
        }
    }
}
