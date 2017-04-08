# MailMergeSender.IsBusy Property 
 

Returns true, while a Send method is pending. Entering a Send method while IsBusy will raise an InvalidOperationException.

**Namespace:**&nbsp;<a href="31c6ebbe-d683-7561-7308-5a5ee1f76bf5">MailMergeLib</a><br />**Assembly:**&nbsp;MailMergeLib (in MailMergeLib.dll) Version: 5.2.0.1

## Syntax

**C#**<br />
``` C#
public bool IsBusy { get; }
```

**VB**<br />
``` VB
Public ReadOnly Property IsBusy As Boolean
	Get
```


#### Property Value
Type: <a href="http://msdn2.microsoft.com/en-us/library/a28wyd50" target="_blank">Boolean</a>

## See Also


#### Reference
<a href="40f1c5c7-ab3e-c0de-43fb-c4fca84e5f64">MailMergeSender Class</a><br /><a href="31c6ebbe-d683-7561-7308-5a5ee1f76bf5">MailMergeLib Namespace</a><br />