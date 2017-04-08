# MailSenderSendFailureEventArgs Constructor 
 

Initializes a new instance of the <a href="340e57c6-df15-faf4-e6ae-2ac6dec6020b">MailSenderSendFailureEventArgs</a> class

**Namespace:**&nbsp;<a href="31c6ebbe-d683-7561-7308-5a5ee1f76bf5">MailMergeLib</a><br />**Assembly:**&nbsp;MailMergeLib (in MailMergeLib.dll) Version: 5.2.0.1

## Syntax

**C#**<br />
``` C#
internal MailSenderSendFailureEventArgs(
	Exception error,
	int failureCounter,
	SmtpClientConfig smtpClientConfig,
	MimeMessage mimeMessage
)
```

**VB**<br />
``` VB
Friend Sub New ( 
	error As Exception,
	failureCounter As Integer,
	smtpClientConfig As SmtpClientConfig,
	mimeMessage As MimeMessage
)
```


#### Parameters
&nbsp;<dl><dt>error</dt><dd>Type: <a href="http://msdn2.microsoft.com/en-us/library/c18k6c59" target="_blank">System.Exception</a><br /></dd><dt>failureCounter</dt><dd>Type: <a href="http://msdn2.microsoft.com/en-us/library/td2s409d" target="_blank">System.Int32</a><br /></dd><dt>smtpClientConfig</dt><dd>Type: <a href="de5f993a-a891-84f4-006c-23e52c27ab88">MailMergeLib.SmtpClientConfig</a><br /></dd><dt>mimeMessage</dt><dd>Type: MimeMessage<br /></dd></dl>

## See Also


#### Reference
<a href="340e57c6-df15-faf4-e6ae-2ac6dec6020b">MailSenderSendFailureEventArgs Class</a><br /><a href="31c6ebbe-d683-7561-7308-5a5ee1f76bf5">MailMergeLib Namespace</a><br />