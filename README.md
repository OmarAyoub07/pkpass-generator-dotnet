# Apple Wallet .pkpass Generator — Windows 11 Guide (PassKit, .NET 9)

Generate valid Apple Wallet passes (`.pkpass`) from a local folder using .NET 9.  
This repo follows a minimal, step-by-step flow: **manifest → signature (detached CMS) → bundle (.pkpass)**.

---

## 1) Introduction & Requirements

To build and sign Apple Wallet passes on Windows, you’ll need:

- An **Apple Developer** account.
- An **Apple Pass Type ID** (create it here: <https://developer.apple.com/account/resources/identifiers/add/bundleId>).
- **Windows 10/11** (this guide targets Windows only).
- **OpenSSL** (to export PEM files from a `.pfx`).
- **.NET SDK 9**.

---

## 2) Set Up the Apple Certificate (Windows)

1. Press **Windows + R**, type `certmgr.msc`, and press **Enter**.  
2. Expand **Personal** → right-click **Certificates** → **All Tasks** → **Advanced Operations** → **Create Custom Request**.  
3. **Next** → **Next** → choose **Legacy key** (or “(No template) CNG key” if legacy isn’t available) and set **Request format** to **PKCS #10**.  
4. **Next** → in **Certificate Information**, expand **Details** → click **Properties**.  
5. **General** tab: set a **Friendly name** (recommended: exactly your Apple **Pass Type ID**).  
6. **Subject** tab:  
   - Add **Common name (CN)** and **Email** under **Subject**.  
   - Add **Email** under **Alternative name**.  
   - Recommendation: use your Pass Type ID as **CN**, and your Apple-registered email for both fields.  
7. **Private Key** tab → **Cryptographic Service Provider**: select **Microsoft Enhanced RSA and AES Cryptographic Provider (Signature)**.  
8. Still in **Private Key**: set **Key size = 2048**, **Key usage/type = Signature**, and **Mark private key as exportable**.  
9. Save the **CSR** to a file (Base-64).  
10. Submit the CSR at: <https://developer.apple.com/account/resources/certificates/list>.  
    After Apple issues the certificate, **open it on the same device** that generated the CSR and **install** it.  
11. Open **certmgr.msc** again → **Personal** → **Certificates** → find your new cert → right-click → **All Tasks** → **Export**.  
12. Export **with the private key** (**Yes, export the private key**) → set a **password** → choose **TripleDES-SHA1** encryption → save as **.pfx**.  
13. Convert the `.pfx` to PEMs with OpenSSL and download WWDR:  
    ```bash
    # Leaf certificate (public)
    openssl pkcs12 -in pkcs_file.pfx -clcerts -nokeys -out pass_cert.pem

    # Private key (PEM; unencrypted)
    openssl pkcs12 -in pkcs_file.pfx -nocerts -nodes -out pass_key.pem
    ```  
    Download the Apple **WWDR** intermediate from: <https://www.apple.com/certificateauthority/>.  
    Example: **Worldwide Developer Relations – G2 (Expiring 05/06/2029)**.

---

## 3) How It Works (Run Guide)

1. Place your generated files in the **`certs/`** folder:  
   - `pass_cert.pem` (leaf Pass Type ID certificate)  
   - `pass_key.pem` (private key)  
   - `AppleWWDR…` (WWDR intermediate, `.cer` or `.pem`)  
2. Ensure `assets.pass/pass.json` contains the correct **`passTypeIdentifier`** and **`teamIdentifier`**.  
3. If creating a custom `pass.json`, ensure assets (`icon.png`, `logo.png`, etc.) meet Apple’s specs.  
   - Design reference: <https://developer.apple.com/library/archive/documentation/UserExperience/Conceptual/PassKit_PG/Creating.html>  
4. Configure paths in the `Program.cs` **Config section**.  
5. Run:  
    ```bash
    dotnet run
    ```  
6. Test the `.pkpass` on **iOS** or **macOS**.  
   When hosting, serve with MIME type: `application/vnd.apple.pkpass`.

---

### Notes

- Flow:  
  1) Generate `manifest.json` (SHA-1 hashes of pass files)  
  2) Create detached **PKCS#7 (CMS)** `signature` over the manifest  
  3) Zip into `.pkpass`  
- Do **not** commit `.pfx` or private key PEMs.  
- If iOS rejects the pass:  
  - Check identifiers in `pass.json` match the certificate.  
  - Ensure WWDR is correct.  
  - Regenerate manifest and signature after any file change.
