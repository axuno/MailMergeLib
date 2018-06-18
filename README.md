<img src="https://raw.githubusercontent.com/axuno/MailMergeLib/master/MailMergeLlib.png" width="300" alt="Logo">

[![GitHub release](https://img.shields.io/github/release/axuno/mailmergelib.svg)](https://github.com/axuno/MailMergeLib/releases/latest)
[![License: MIT](https://img.shields.io/badge/License-MIT-brightgreen.svg)](https://github.com/axuno/MailMergeLib/blob/master/License.txt)
[![Build status](https://ci.appveyor.com/api/projects/status/9gpm43038riwlbu2/branch/master?svg=true)](https://ci.appveyor.com/project/axuno/mailmergelib/branch/master)
[![AppVeyor tests](https://img.shields.io/appveyor/tests/axunonb/mailmergelib.svg)](https://ci.appveyor.com/project/axuno/mailmergelib/branch/master/tests)



```MailMergeLib``` version 5 is an SMTP mail client library which provides comfortable mail merge capabilities. ```MailMergeLib``` is written in C# and comes with the following features:

### 1. Mail message generation:
* Email templates can be fully individualized in terms of recipients, subject, HTML and/or plain text, attachments and even headers. Placeholders are inserted as variable names from data source between curly braces like so: ```{MailboxAddress.Name}``` or with formatting arguments like ```{Date:yyyy-MM-dd}```.
* HTML text may contain images from local hard disk, which will be automatically inserted as inline attachments.
* For HTML text  ```MailMergeLib ``` can generate a plain text representation.
* Attachment sources can be files, streams or strings.
* The data source for email merge messages to a number of recipients and be any ```IEnumerable``` object as well as ```DataTable```s. The data source for single emails can be any of the following types: ```Dictionary<string,object>```, ```ExpandoObject```, ```DataRow```, any class instances or anonymous types. For class instances it's even allowed to use the name of parameter less methods.
* Placeholders in the email can be formatted with any of the features known from string.Format by using [SmartFormat.NET](https://github.com/scottrippey/SmartFormat.NET/wiki). SmartFormat is a parser coming close to string.Format's speed, but bringing a lot of additional options like easy pluralization for many languages.
* Resulting emails are MimeMessages from [MimeKit](https://github.com/jstedfast/MimeKit), an outstanding tool for creating and parsing emails, covering all relevant MIME standards making sure that emails are not qualified as SPAM.
* Support for international email address format.

### 2. Sending email messages:
* Practically unlimited number of parallel tasks to send out individualized emails to a big number of recipients.
* SmptClients for each task can get their own preconfigured settings, so that e.g. several mail servers can be used for one send job.
* Progress of processing emails can easily be observed with a number of events.
* SMTP failures can automatically be resolved supplying a backup configuration. This fault-tolerance is essential for unattended production systems.
* Emails are sent using the SmtpClient from [MailKit](https://github.com/jstedfast/MailKit), the sister project to MimeKit. SmtpClient is highly flexible and can be configured for literally every scenario you can think of.
* Instead of sending, emails can also be stored in MIME formatted text files, e.g. if a "pickup directory" from IIS or Microsoft Exchange shall be used. If needed, these files can be loaded back into a MimeMessage from MimeKit.

### 3. Save and restore:
* Messages and templates can be saved and loaded to/from XML files.
* Configuration settings for messages and SMTP can be stored to and loaded from an XML file.

### 4. Both:
* Fine grained control over the whole process of email message generation and distribution.
* Clearly out-performs .NET ```System.Net.Mail```.
* RFC standards compliant.
* We aks you not to use ```MailMergeLib``` for sending unsolicited bulk email.

### 5. Supported Frameworks
* .Net Framework 4.5+
* .Net Core

### Get started
[![NuGet](https://img.shields.io/nuget/v/MailMergeLib.svg)](https://www.nuget.org/packages/MailMergeLib/) Install the NuGet package

[![Docs](https://img.shields.io/badge/docs-up%20to%20date-brightgreen.svg)](https://github.com/axuno/MailMergeLib/wiki)
Have a look at the [MailMergeLib Wiki](https://github.com/axuno/MailMergeLib/wiki)

### History
MailMergeLib was introduced back in 2007 on [CodeProject](http://www.codeproject.com/Articles/19546/MailMergeLib-A-NET-Mail-Client-Library). The last version published there is 4.03. It is based on ```System.Net.Mail```. For anyone still using ```System.Net.Mail```, Jeffrey Stedfast's [Code Review](http://jeffreystedfast.blogspot.de/2015/03/code-review-microsofts-systemnetmail.html) might be interesting, although he describes issues more polite than they actually are (especially in terms of RFC violations).

MailMergeLib 5 published on GitHub is a major rewrite, and it is not backwards compatible to prior releases. There is, however, a migration guide included in the ```MailMergeLib``` Wiki.
