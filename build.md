# BUILD Instructions – SVLJmTLSClientValidatorModule

This document describes how to build the `SVLJ.Security.dll` module from source using Visual Studio.  
The module implements strict mTLS (mutual TLS) client certificate validation for IIS applications running on .NET Framework 4.8.

---

## 📦 Prerequisites

- Windows 10/11 or higher
- [Visual Studio 2019 or 2022](https://visualstudio.microsoft.com/)
  - Workload: **.NET desktop development**
- .NET Framework 4.8 Developer Pack (included in recent VS versions)

---

## 📁 Project Structure

You will be creating this structure manually:

```

SVLJmTLSClientValidatorModule
├── SVLJmTLSClientValidatorModule.cs
├── SVLJ.Security.csproj
├── SVLJ.Security.sln
├── Properties
│   └── AssemblyInfo.cs

````

---

## 🧰 Build Steps

### 1. Create a Class Library project

1. Launch **Visual Studio**
2. Click `File → New → Project`
3. Select: **Class Library (.NET Framework)**
4. Name the project: `SVLJ.Security`
5. Select framework version: `.NET Framework 4.8`
6. Finish and create the solution

### 2. Add the source file

1. Download or copy `SVLJmTLSClientValidatorModule.cs`
2. Place it in the project directory
3. In Solution Explorer:
   - Right-click the project → `Add → Existing Item`
   - Select `SVLJmTLSClientValidatorModule.cs`

### 3. Optional: Add `AssemblyInfo.cs`

Create `Properties\AssemblyInfo.cs` with content like:

```csharp
using System.Reflection;
[assembly: AssemblyTitle("SVLJ.Security")]
[assembly: AssemblyDescription("mTLS Validation Module for IIS")]
[assembly: AssemblyVersion("1.4.0.0")]
[assembly: AssemblyFileVersion("1.4.0.0")]
````

### 4. Build the DLL

1. Set build configuration to `Release`
2. Press `Ctrl+Shift+B` or go to `Build → Build Solution`
3. Output file will be:

```
<project path>\bin\Release\SVLJ.Security.dll
```

---

## 🚀 Deployment

1. Copy `SVLJ.Security.dll` to your IIS application's `bin\` directory
2. Ensure you have a valid `web.config` (see README)
3. Place any supporting files (e.g. `403c.html`, `ca-bundle.pem`) in the appropriate directories
4. Restart IIS:

   ```
   iisreset
   ```

---

## 🔍 Verifying the DLL

You can check if the module loads correctly by:

* Viewing the request headers (`HTTP_SVLJ_*`)
* Forcing an error (e.g., missing cert) and confirming redirect to `/error/403c.html?reason=missing-cert`
* Checking `System.Diagnostics.Trace` output

---

## 📝 Notes

* The project has **no NuGet dependencies**
* It is designed for self-hosted, air-gapped or public sector environments
* You may strong-name the DLL if required for GAC or binding redirects
