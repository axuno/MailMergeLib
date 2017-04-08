# MailSenderMergeCompleteEventArgs Constructor 
 

Initializes a new instance of the <a href="0a8d161d-b3a3-2c48-f9bd-c7dc53d88ca1">MailSenderMergeCompleteEventArgs</a> class

**Namespace:**&nbsp;<a href="31c6ebbe-d683-7561-7308-5a5ee1f76bf5">MailMergeLib</a><br />**Assembly:**&nbsp;MailMergeLib (in MailMergeLib.dll) Version: 5.2.0.1

## Syntax

**C#**<br />
``` C#
internal MailSenderMergeCompleteEventArgs(
	DateTime startTime,
	DateTime endTime,
	int totalMsg,
	int sentMsg,
	int errorMsg,
	int numOfSmtpClientsUsed
)
```

**VB**<br />
``` VB
Friend Sub New ( 
	startTime As DateTime,
	endTime As DateTime,
	totalMsg As Integer,
	sentMsg As Integer,
	errorMsg As Integer,
	numOfSmtpClientsUsed As Integer
)
```


#### Parameters
&nbsp;<dl><dt>startTime</dt><dd>Type: <a href="http://msdn2.microsoft.com/en-us/library/03ybds8y" target="_blank">System.DateTime</a><br /></dd><dt>endTime</dt><dd>Type: <a href="http://msdn2.microsoft.com/en-us/library/03ybds8y" target="_blank">System.DateTime</a><br /></dd><dt>totalMsg</dt><dd>Type: <a href="http://msdn2.microsoft.com/en-us/library/td2s409d" target="_blank">System.Int32</a><br /></dd><dt>sentMsg</dt><dd>Type: <a href="http://msdn2.microsoft.com/en-us/library/td2s409d" target="_blank">System.Int32</a><br /></dd><dt>errorMsg</dt><dd>Type: <a href="http://msdn2.microsoft.com/en-us/library/td2s409d" target="_blank">System.Int32</a><br /></dd><dt>numOfSmtpClientsUsed</dt><dd>Type: <a href="http://msdn2.microsoft.com/en-us/library/td2s409d" target="_blank">System.Int32</a><br /></dd></dl>

## See Also


#### Reference
<a href="0a8d161d-b3a3-2c48-f9bd-c7dc53d88ca1">MailSenderMergeCompleteEventArgs Class</a><br /><a href="31c6ebbe-d683-7561-7308-5a5ee1f76bf5">MailMergeLib Namespace</a><br />