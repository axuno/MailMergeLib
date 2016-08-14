![logo](https://github.com/axuno/MailMergeLib/blob/master/MailMergeLlib.png)
# MailMergeLib 5.0
```MailMergeLib``` is an SMTP mail client library which provides comfortable mail merge capabilities. ```MailMergeLib``` comes with the following features:

### 1. Mail message generation:
* Email templates can be fully personalized in terms of recipients, subject, HTML and/or plain text, attachments and even headers. Placeholders are inserted as variable names from data source within curly braces like so: ```{MailboxAddress.Name}```
* HTML text may contain Images from local hard disk, which be automatically inserted as inline attachments.
* Attachments sources can be files, streams or strings.
* The data source for email merge messages to a number of recipients and be any ```IEnumerable``` object as well as DataTables. The data source for single emails can be any of the following types: ```Dictionary<string,object>```, ```ExpandoObject```, ```DataRow```, any class instances, anonymous types. For class instances it's even allowed to use the name of parameterless methods.
* Placeholders in the email can be formatted with any of the features know from string.Format by using [SmartFormat.NET](https://github.com/scottrippey/SmartFormat.NET/wiki). SmartFormat is a parser coming close to string.Format's speed, but bringing a lot of additional options like easy pluralization for many languages.
* Resulting emails are MimeMessages from [MimeKit](https://github.com/jstedfast/MimeKit), an outstanding tool for sending and receiving emails, covering all relevant MIME standards making sure that emails are not qualified as SPAM.
* Support for international email address format.

### 2. Sending email messages:
* Practically unlimited number of parallel tasks to send out individualized emails to a big number of recipients.
* SmptClients for each task can get their own preconfigured settings, so that e.g. several mail servers can be used for one send job.
* Progress of processing emails can easily be observed with a number of delegates.
* SMTP failures are automatically resolved using a backup configuration. This fault-tolerance is essential for unattended production systems.
* SMTP configuration can be stored/restored to/from standard XML files.
* Emails are sent using the SmtpClient from [MailKit](https://github.com/jstedfast/MailKit), the sister project to MimeKit. SmtpClient is highly flexible and can be configured or literally every scenario you can think of.
* Instead of sending, emails can also be stored in MIME formatted text files, e.g. if a "pickup directory" from IIS or Microsoft Exchange shall be used. If needed, these files can be loaded into a MimeMessage from MimeKit.


### 3. Both:
* Fine grained control over the whole process of email message generation and distribution.
* By far better performance than with .NET ```System.Net.Mail```.

#### History
MailMergeLib was introduced back in 2007 on [CodeProject](http://www.codeproject.com/Articles/19546/MailMergeLib-A-NET-Mail-Client-Library). The last version published there is 4.0. It is based on System.Net.Mail. Version 5 published on GitHub is a major rewrite, and it is not backwards compatible.
