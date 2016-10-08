using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.

[assembly: AssemblyTitle("MailMergeLib")]
[assembly: AssemblyDescription("MailMergeLib is an SMTP mail client library which provides comfortable mail merge capabilities for text, inline images and attachments, as well as good throughput and fault tolerance for sending mail messages.")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("axuno gGmbH")]
[assembly: AssemblyProduct("MailMergeLib 5")]
[assembly: AssemblyCopyright("Copyright © 2007-2016 by axuno gGmbH")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

[assembly: CLSCompliant(false)]

[assembly: InternalsVisibleTo("UnitTests, PublicKey=00240000048000009400000006020000002400005253413100040000010001007d73450c58a30b94409ec6a1ad78af1337bae462b82c80aeda1e0501506db459cd074beb94cb342a3491687e75e0143e6fce3bfa5cc3221b29017e0f4e5b116d0b135405dad460283413f75e63c6db3b1b074a16d780f013433a7b7883e9760079b6d5a41d5d9a3cff80a3b1e42b1d057c94533c978a625c3a0a933cac9ecda9")]
// for above, both assemblies must be signed!
// for unsigned assemblies use: [assembly: InternalsVisibleTo("UnitTests")]

/*
Create PublicKey file:
sn -p d:MailMergeLib.snk d:MailMergeLib.snk.PublicKey
create PublicKey:
sn -tp d:Mailmergelib.snk.PublicKey
*/

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(true)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("493782f8-2007-4316-b72c-28fe39c3180f")]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version 
//      Build Number
//      Revision
//
[assembly: AssemblyInformationalVersion ("5.0.1.0")]
[assembly: AssemblyFileVersion ("5.0.1.0")]
[assembly: AssemblyVersion ("5.0.1.0")]
