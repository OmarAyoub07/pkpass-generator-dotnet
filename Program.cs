using Passkit;

// === Config (dev-only; move to env/appsettings for public repos) ===
var BASE = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..")); // project root
var AppleWWDR = @$"{BASE}\certs\AppleWWDRCAG2.cer";      // Apple WWDR intermediate cert
var PASS_PATH = @$"{BASE}\assets.pass";                  // pass folder (pass.json, images, etc.)

// From .pfx/.p12 via OpenSSL:
//   cert:  openssl pkcs12 -in pkcs_file.pfx -clcerts -nokeys -out pass_cert.pem
//   key :  openssl pkcs12 -in pkcs_file.pfx -nocerts -nodes -out pass_key.pem
var CERT_PATH = @$"{BASE}\certs\pass_cert.pem";          // leaf Pass Type ID cert (PEM)
var KEY_PATH  = @$"{BASE}\certs\pass_key.pem";           // private key (PEM) — keep secret
var OUTPUT_MANIFEST = @$"{BASE}\assets.pass\manifest.json"; // manifest output path
var OUTPUT_PK = @$"{BASE}\out\test.pkpass";     // final .pkpass path

// 1) Build manifest.json (SHA-1 hashes of pass files)
var manifest = PassManifest.Build(PASS_PATH, OUTPUT_MANIFEST);

// 2) Sign manifest (detached CMS/PKCS#7) with Pass cert + key, include WWDR
var signature = PassSigner.Sign(PASS_PATH, CERT_PATH, KEY_PATH, AppleWWDR);

// 3) Bundle as .pkpass (ZIP with files at root)
var pkpass = PassBundler.CreatePkPass(PASS_PATH, OUTPUT_PK, overwrite: true);

// Logs
Console.WriteLine($"PKPass:    {pkpass}");
Console.WriteLine($"Manifest:  {manifest}");
Console.WriteLine($"Signature: {signature}");
