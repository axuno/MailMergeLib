# ![MailMergeLib](https://raw.githubusercontent.com/axuno/MailMergeLib/main/MailMergeLlib_300x52.png)

### What is *MailMergeLib*?

```MailMergeLib``` version 5 is an SMTP mail client library which provides comfortable mail merge capabilities. ```MailMergeLib``` is written in C# and comes with **the following features**:

### Mail message generation
* Email templates can be fully individualized in terms of recipients, subject, HTML and/or plain text, attachments and even headers. Placeholders are inserted as variable names from data source between curly braces like so: ```{MailboxAddress.Name}``` or with formatting arguments like ```{Date:yyyy-MM-dd}```.
* HTML text may contain images from local hard disk, which will be automatically inserted as inline attachments.
* For HTML text  ```MailMergeLib ``` can generate a plain text representation.
* Attachment sources can be files, streams or strings.
* The data source for email merge messages to a number of recipients and be any ```IEnumerable``` object as well as ```DataTable```s. The data source for single emails can be any of the following types: ```Dictionary<string,object>```, ```ExpandoObject```, ```DataRow```, any class instance or anonymous types. For class instances it's even allowed to use the name of parameter less methods in placeholders.
* Placeholders in the email can be formatted much like the features known from `string.Format` by using [SmartFormat.NET](https://github.com/axuno/MailMergeLib/wiki). SmartFormat is a fast and lean string parser and formatter, bringing a lot of additional options like conditional output depending on input data.
* Resulting emails are MimeMessages from [MimeKit](https://github.com/jstedfast/MimeKit), an outstanding tool for creating and parsing emails, covering all relevant MIME standards.
* Support for international email address format.

### Sending email messages
* Practically unlimited number of parallel tasks to send out individualized emails to a big number of recipients.
* SmptClients for each task can get their own preconfigured settings, so that e.g. several mail servers can be used for one send job.
* Progress of processing emails can easily be observed with a number of events.
* SMTP failures can automatically be resolved supplying a backup configuration. This fault-tolerance is essential for unattended production systems.
* Emails are sent using the SmtpClient from [MailKit](https://github.com/jstedfast/MailKit), the sister project to MimeKit. SmtpClient is highly flexible and can be configured for literally every scenario you can think of.
* Instead of sending, emails can also be stored in MIME formatted text files, e.g. if a "pickup directory" from IIS or Microsoft Exchange shall be used. If needed, these files can be loaded back into a MimeMessage from MimeKit.

### Save and restore
* Messages and templates can be saved and loaded to/from XML files.
* Configuration settings for messages and SMTP can be stored to and loaded from an XML file.

### Both
* Fine grained control over the whole process of email message generation and distribution.
* RFC standards compliant.
* We aks you not to use ```MailMergeLib``` for sending unsolicited bulk email.

### Supported Frameworks
* .Net Framework 4.6.2 and later
* .Net Standard 2.1
* NET 6.0 and later

[![Paypal-Donations](https://img.shields.io/badge/Donate-PayPal-important.svg?style=flat-square)](https://www.paypal.com/donate?hosted_button_id=KSC3LRAR26AHN)
