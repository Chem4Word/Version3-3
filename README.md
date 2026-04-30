# Introduction 
This project is Version 3.3 of the Chemistry for Word Add-In

## Code of Conduct
This project has adopted the code of conduct defined by the Contributor Covenant to clarify expected behavior in our community.<br>
For more information see the [.NET Foundation Code of Conduct](https://dotnetfoundation.org/code-of-conduct). 

## Getting Started
1. Installation process see $/docs/Chem4Word-Version3-3-Developer-SetUp.docx
2. Software dependencies Office 2010/2013/2016/2019/2021/2024/365 (Installed on Desktop)
3. Recommended screen resolution 1920x1080 (minimum 1366x768)
4. Git Clone of main branch

## Build and Test
The Chemistry for Word Add-in (Chem4Word) is contained within a single Visual Studio solution located at src/Chem4Word.V3-3.sln
This solution has two main projects (use Set as Start Up then run the project), from time to time there may be other utility or PoC projects.
1. Chem4Word.V3 is the Add-In
2. WinForms.TestHarness this allows testing of a the Editing subset of operations without starting MS Word

All unit tests are written with XUnit
Azure Devops Build must be set to use VS2022 build agent

## List of NuGet packages
| Package | Version | License | Reference Count |
|--|--|--|--|
|Azure.Core.Amqp|1.3.1|MIT|3|
|Azure.Core|1.54.0|MIT|3|
|Azure.Messaging.ServiceBus|7.20.1|MIT|3|
|BouncyCastle.Cryptography|2.6.2|MIT|1|
|DocumentFormat.OpenXml.Framework|3.5.1|MIT|2|
|DocumentFormat.OpenXml|3.5.1|MIT|2|
|DotNetProjects.WpfToolkit.Input|6.1.94|MS-PL|1|
|EntityFramework|6.5.1|Apache-2.0|3|
|Google.Protobuf|3.34.1|BSD-3-Clause|2|
|Grpc.Tools|2.80.0|Apache-2.0|1|
|Jacobslusser.ScintillaNET|3.6.3|MIT|1|
|Meziantou.Framework.Win32.CredentialManager|1.7.18|MIT|1|
|Microsoft.Azure.Amqp|2.7.2|MIT|3|
|Microsoft.Bcl.AsyncInterfaces|10.0.7|MIT|3|
|Microsoft.Bcl.HashCode|6.0.0|MIT|1|
|Microsoft.Extensions.Configuration.Abstractions|10.0.7|MIT|3|
|Microsoft.Extensions.DependencyInjection.Abstractions|10.0.7|MIT|3|
|Microsoft.Extensions.Diagnostics.Abstractions|10.0.7|MIT|3|
|Microsoft.Extensions.FileProviders.Abstractions|10.0.7|MIT|3|
|Microsoft.Extensions.Hosting.Abstractions|10.0.7|MIT|3|
|Microsoft.Extensions.Logging.Abstractions|10.0.7|MIT|3|
|Microsoft.Extensions.Options|10.0.7|MIT|3|
|Microsoft.Extensions.Primitives|10.0.7|MIT|3|
|Microsoft.Identity.Client.Extensions.Msal|4.83.3|MIT|3|
|Microsoft.Identity.Client|4.83.3|MIT|3|
|Microsoft.IdentityModel.Abstractions|8.17.0|MIT|3|
|Microsoft.NETCore.Platforms|7.0.4|MIT|2|
|Microsoft.Xaml.Behaviors.Wpf|1.1.142|MIT|1|
|Newtonsoft.Json|13.0.4|MIT|14|
|Ookii.Dialogs.WinForms|4.0.0|BSD-3-Clause|1|
|Standard.Licensing|1.2.2|MIT|1|
|Stub.System.Data.SQLite.Core.NetFramework|1.0.119.0|Public Domain|3|
|System.Buffers|4.6.1|MIT|6|
|System.ClientModel|1.10.0|MIT|3|
|System.Collections.Immutable|10.0.7|MIT|2|
|System.Data.SQLite.Core|1.0.119.0|Public Domain|3|
|System.Data.SQLite.EF6|1.0.119.0|Public Domain|3|
|System.Data.SQLite.Linq|1.0.119.0|Public Domain|3|
|System.Data.SQLite|1.0.119.0|Public Domain|3|
|System.Diagnostics.DiagnosticSource|10.0.7|Public Domain|3|
|System.Formats.Asn1|10.0.7|MIT|3|
|System.Formats.Nrbf|10.0.7|MIT|1|
|System.IO.Compression.ZipFile|4.3.0|MS-.NET-Library|1|
|System.IO.FileSystem.AccessControl|5.0.0|MIT|3|
|System.IO.FileSystem.Primitives|4.3.0|MS-.NET-Library|2|
|System.IO.Packaging|10.0.7|MIT|2|
|System.IO.Pipelines|10.0.7|MIT|3|
|System.Memory.Data|10.0.7|MIT|3|
|System.Memory|4.6.3|MIT|6|
|System.Net.Http|4.3.4|MS-.NET-Library|5|
|System.Numerics.Vectors|4.6.1|MIT|6|
|System.Reflection.Metadata|10.0.7|MIT|1|
|System.Resources.Extensions|10.0.7|MIT|1|
|System.Runtime.CompilerServices.Unsafe|6.1.2|MIT|6|
|System.Runtime.InteropServices.RuntimeInformation|4.3.0|MS-.NET-Library|3|
|System.Security.AccessControl|6.0.1|MIT|3|
|System.Security.Cryptography.Algorithms|4.3.1|MS-.NET-Library|5|
|System.Security.Cryptography.Encoding|4.3.0|MS-.NET-Library|5|
|System.Security.Cryptography.Primitives|4.3.0|MS-.NET-Library|5|
|System.Security.Cryptography.ProtectedData|10.0.7|MIT|3|
|System.Security.Cryptography.X509Certificates|4.3.2|MS-.NET-Library|5|
|System.Security.Principal.Windows|5.0.0|MIT|3|
|System.Text.Encodings.Web|10.0.7|MIT|3|
|System.Text.Json|10.0.7|MIT|3|
|System.Threading.Tasks.Extensions|4.6.3|MIT|3|
|System.ValueTuple|4.6.2|MIT|13|
|VirtualizingWrapPanel|2.5.1|MIT|1|
|WixToolset.Dtf.CustomAction|7.0.0|OSI|1|
|WixToolset.Dtf.WindowsInstaller|7.0.0|OSI|1|
|WixToolset.NetFx.wixext|7.0.0|OSI|1|
|WixToolset.UI.wixext|7.0.0|OSI|1|
|WixToolset.Util.wixext|7.0.0|OSI|1|

Please note that the following NuGet packages can't be updated

'System.Data.SQLite.* 1.0.119.0' is the last supported version which will run in a VSTO
'Perfolizer 0.7.1' is not compatible with 'BenchmarkDotNet 0.15.8 constraint: Perfolizer (= 0.6.1)'.


## Acknowledgements
1. [CEVOpen](https://github.com/petermr/CEVOpen) - This data represents about 2100 unique chemical names of volatile plant chemicals (essential oils) from the EssoilDB 1.0 database (compiled from the scientific literature over about 10 years in Dr Yadav's laboratory). They are made available for re-use by anyone for any purpose (CC0). We would appreciate acknowledgement of EssoilDB and the following people who extracted and cleaned the data during 2019. (Gitanjali Yadav, Ambarish Kumar, Peter Murray-Rust).

## Contribute
Please feel free to contribute to the project.
Create your own branch, make your changes then create a Pull Request to initiate a merge into the master branch.

### .NET Foundation
This project is supported by the [.NET Foundation](https://dotnetfoundation.org).
