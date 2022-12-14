# Introduction 
This project is Version 3.3 of the Chemistry for Word Add-In

## Getting Started
1.	Installation process see $/docs/Chem4Word-Version3-3-Developer-SetUp.docx
2.	Software dependencies Office 2010/2013/2016/2019/365 (Installed on Desktop)
3.	Latest releases

## Build and Test
The Chemistry for Word Add-in (Chem4Word) is contained within a single Visual Studio solution located at src/Chem4Word.V3-3.sln
This solution has two main projects (use Set as Start Up then run the project), from time to time there may be other utility or PoC projects.
1. Chem4Word.V3 is the Add-In
2. WinForms.TestHarness this allows testing of a the Editing subset of operations without starting MS Word

All unit tests are written with XUnit
Azure Devops Build must be set to use VS2022 build agent

## List of NuGet packages
| Package | Version | Licence | Reference Count |
|--|--|--|--|
|Azure.Core|1.22.0|MIT|1|
|Azure.Security.KeyVault.Secrets|4.2.0|MIT|1|
|DocumentFormat.OpenXml|2.15.0|MIT|3|
|DotNetProjects.WpfToolkit.Input|6.1.94|MS-PL|1|
|EntityFramework|6.4.4|Apache-2.0|3|
|Jacobslusser.ScintillaNET|3.6.3|MIT|1|
|Microsoft.Azure.Services.AppAuthentication|1.6.2|MIT|2|
|Microsoft.Bcl.AsyncInterfaces|6.0.0|MIT|1|
|Microsoft.IdentityModel.Clients.ActiveDirectory|5.2.9|MIT|2|
|Microsoft.IdentityModel.JsonWebTokens|6.15.1|MIT|2|
|Microsoft.IdentityModel.Logging|6.15.1|MIT|2|
|Microsoft.IdentityModel.Tokens|6.15.1|MIT|2|
|Microsoft.NETCore.Platforms|6.0.1|MIT|2|
|Microsoft.NETCore.Targets|5.0.0|MIT|2|
|Microsoft.Rest.ClientRuntime.Azure|3.3.19|MIT|1|
|Microsoft.Rest.ClientRuntime|2.3.23|MIT|2|
|Microsoft.Xaml.Behaviors.Wpf|1.1.39|MIT|1|
|Microsoft_VisualStudio_QualityTools_UnitTestFramework.STW|12.0.21005.1|Microsoft|1|
|Newtonsoft.Json|13.0.1|MIT|16|
|Ookii.Dialogs.WinForms|4.0.0|Public Domain|1|
|Stub.System.Data.SQLite.Core.NetFramework|1.0.115.5|Public Domain|3|
|System.Buffers|4.5.1|MIT|2|
|System.Collections.Immutable|6.0.0|MIT|1|
|System.Data.SQLite.Core|1.0.115.5|Public Domain|3|
|System.Data.SQLite.EF6|1.0.115.5|Public Domain|3|
|System.Data.SQLite.Linq|1.0.115.5|Public Domain|3|
|System.Data.SQLite|1.0.115.5|Public Domain|3|
|System.Diagnostics.DiagnosticSource|6.0.0|MIT|1|
|System.IdentityModel.Tokens.Jwt|6.15.1|MIT|2|
|System.IO.FileSystem.Primitives|4.3.0|MS-.NET-Library License|3|
|System.IO.Packaging|6.0.0|MIT|3|
|System.Memory.Data|6.0.0|MIT|1|
|System.Memory|4.5.4|MIT|2|
|System.Net.Http|4.3.4|MS-.NET-Library License|9|
|System.Numerics.Vectors|4.5.0|MIT|2|
|System.Private.Uri|4.3.2|MIT|2|
|System.Resources.Extensions|6.0.0|MIT|1|
|System.Runtime.CompilerServices.Unsafe|6.0.0|MIT|2|
|System.Runtime.InteropServices.RuntimeInformation|4.3.0||1|
|System.Security.Cryptography.Algorithms|4.3.1|MS-.NET-Library License|9|
|System.Security.Cryptography.Encoding|4.3.0|MS-.NET-Library License|9|
|System.Security.Cryptography.Primitives|4.3.0|MS-.NET-Library License|9|
|System.Security.Cryptography.X509Certificates|4.3.2|MS-.NET-Library License|9|
|System.Text.Encodings.Web|6.0.0|MIT|1|
|System.Text.Json|6.0.1|MIT|1|
|System.Threading.Tasks.Extensions|4.5.4|MIT|1|
|System.ValueTuple|4.5.0|MIT|4|
|Unofficial.Ionic.Zip|1.9.1.8|Unknown ?|3|
|VirtualizingWrapPanel|1.5.7|MIT|1|
|WindowsAzure.ServiceBus|6.2.1|MS-.NET-Library License|2|
|xunit.abstractions|2.0.3|Apache-2.0|1|
|xunit.analyzers|0.10.0|Apache-2.0|1|
|xunit.assert|2.4.1|Apache-2.0|1|
|xunit.core|2.4.1|Apache-2.0|1|
|xunit.extensibility.core|2.4.1|Apache-2.0|1|
|xunit.extensibility.execution|2.4.1|Apache-2.0|1|
|xunit.runner.console|2.4.1|Apache-2.0|1|
|xunit.runner.visualstudio|2.4.3|Apache-2.0|1|
|xunit|2.4.1|Apache-2.0|1|

## Acknowledgements
1. [CEVOpen](https://github.com/petermr/CEVOpen) - This data represents about 2100 unique chemical names of volatile plant chemicals (essential oils) from the EssoilDB 1.0 database (compiled from the scientific literature over about 10 years in Dr Yadav's laboratory). They are made available for re-use by anyone for any purpose (CC0). We would appreciate acknowledgement of EssoilDB and the following people who extracted and cleaned the data during 2019. (Gitanjali Yadav, Ambarish Kumar, Peter Murray-Rust).

## Contribute
Please feel free to contribute to the project.
Create your own branch, make your changes then create a Pull Request to initiate a merge into the master branch.
