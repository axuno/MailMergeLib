# Upcoming 5.7.0.0
* Encryption of Credential in Settings can now be disabled. Breaking change: disabled is the default.
* SMTP settings can now be read from app.config/web.config
* Path checks (e.g. inline images, attachments) now respect Linux and MacOsX platform rules beside Windows
* Classes in namespace 'MailMergLib.SmartFormatMail' were obsolete and are now removed.
* Updated string parser to [SmartFormat.Net v2.5](https://github.com/axuno/SmartFormat)
  * **Sources**
  * *New:* Added ```ValueTupleSource``` for ```ValueTuple```s
  * *Changed:* ```SmartObjects``` and ```SmartObjectsSource``` are depreciated in favor of ```ValueTupleSource```
  * **Settings**
  * *Breaking Change:* Internal string comparisons (i.e. for placeholder names) are no more culture-specific, but ```Ordinal``` or ```OrdinalIgnoreCase``` respectively. See discussion [under this issue](https://github.com/axuno/SmartFormat/issues/122).
  * *Breaking Change:* Default ```ErrorAction``` is now ```ThrowError``` for parser and formatter, instead of ```Ignore```
  * **Other**
  * *Changed:* Removed all members which were flagged obsolete since more than a year.
* Updated versions of other dependencies

# 5.6.1.0
* Reverted back to v5.5.0 behavior: MessageConfig.FileBaseDirectory must be a full path only before the MailMergeMessage is processed (not already, when the property is set).
* Closes https://github.com/axuno/MailMergeLib/issues/18
* Classes in namespace 'MailMergLib.SmartFormatMail' are obsolete. Use namespace 'SmartFormat' from dependency 'SmartFormat.Net' instead.
* This is the last minor version which supports netstandard1.6
* Updated versions of dependencies
* More unit tests

# 5.6.0.0
* Classes in namespace 'MailMergLib.SmartFormatMail' are obsolete. Use namespace 'SmartFormat' from new dependency 'SmartFormat.Net' instead.
* This is the last version which supports netstandard1.6
* Updated versions of dependencies

# 5.5.0.0
* [Support for JSON](https://github.com/scottrippey/SmartFormat.NET/wiki/Data-Sources) (JObject, JArray)
* [IsMatchFormatter](https://github.com/scottrippey/SmartFormat.NET/wiki/IsMatch) for evaluation of regular expressions
* Support for NetStandard 2.0

# 5.4.1.0
New feature: Within a MailMergeSender.OnMessageFailure delegate the cause of the failure can be removed, so that the message can still be sent successfully. See details in https://github.com/axuno/MailMergeLib/wiki/Message-Error-Handling

# 5.4.0.0
**Changes:**
* Refactored raising events in ```MailMergeSender``` and ```MailMergeMessage```
* Integrated [SmartFormat.NET v.2.2.0](https://github.com/scottrippey/SmartFormat.NET/) which resolves a rare issue, when ```null``` is a parameter of ```Send...``` methods of ```MailMergeSender```.
* Exceptions in ```MailSmartFormatter``` are all caught (i.e. not propagated to ```MailMergeSender```), never mind the exception settings in SmartFormatMail modules. This way they do no more have influence when generating the email message. Format and parse errors always end up in a ```MailMergeMessageException```.
* Added new exception of type ```ParseException``` for parsing issues in the template. ```ParseException``` are included as inner exceptions of ```MailMergeMessageException``` when building a message fails.
* Updated dependency: MailKit v2.0,1 MimeKit v2.0.1 (.NetStandard, .Net 4.5) and v1.22.0 (.Net 4.0)
* Updated dependency: AngleSharp 0.9.9.1
* Updated dependencies in UnitTests: NUnit 3.9.0 and NUnit3TestAdapter 3.9.0
* Added unit tests for raising events in ```MailMergeSender``` and ```MailMergeMessage```
* Updated the [API documentation](https://axuno.net/mailmergelib/docs/)

**Note:**
As MailKit and MileKit no longer support .Net 4.0, this is expected to become the last release which supplies packages for .Net 4.0.

# 5.3.0.0
**New components:**
* All relevant classes can be serialized and deserialized
* ```MessageStore``` for saving and loading ```MailMergeMessages```
* Integrated SmartFormat.Net 2.1.0.2 for handling templates and ```{placeholders}```. Character string literals read from files or other resources (outside the code) are now treated like with ```string.Format``` inside of code. However, backslashes in filenames are not treated as escape characters.
* Templates: ```MailMergeMessage```s may contain text and/or html Templates which are inserted depending on certain conditions. ```Template```s may contain ```{placeholders}```
* ```Credential``` class implementing already existing ```ICredentials```
* New unit test for added and changed components

**Changes:**
* Classes contain Equality methods
* Moved from System.Xml.Serialization to YAXLib v2.15
* Updated dependency for MailKit v1.18.0, MimeKit v1.18.0
* Moved solution to Visual Studio 2017
* Migrated MailMergeLib.NetCore to the VS2017 xml project file format
* Added method ```MailMergeMessage.GetMimeMessages(IEnumerable data)```
* ```MailMergeMessage.ConvertHtmlToPlainText()``` writes directly to ```MailMergeMessage.PlainText```

**Removed obsolete components:**
* HtmlTagHelper
* TextVariableManager
* HtmlAgilityPackHtmlConverter
* RegExHtmlConverter
* HtmlBodyBuilderRegEx

**Documentation;**
* Wiki substantially extended
* API documentation updated
