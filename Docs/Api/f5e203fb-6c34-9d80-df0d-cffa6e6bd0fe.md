# RegExHtmlConverter.ToPlainText Method 
 

Converts HTML to text as good as a simple RegEx based function can. Html entities are not (!) converted to text. Not very useful for complex HTML, because regular expressions are not the right means to deal with HTML. Use parsers like AngleSharp or HtmlAgilityPack instead of this "poor man's HTML converter".

**Namespace:**&nbsp;<a href="31c6ebbe-d683-7561-7308-5a5ee1f76bf5">MailMergeLib</a><br />**Assembly:**&nbsp;MailMergeLib (in MailMergeLib.dll) Version: 5.2.0.1

## Syntax

**C#**<br />
``` C#
public string ToPlainText(
	string html
)
```

**VB**<br />
``` VB
Public Function ToPlainText ( 
	html As String
) As String
```


#### Parameters
&nbsp;<dl><dt>html</dt><dd>Type: <a href="http://msdn2.microsoft.com/en-us/library/s1wwdcbf" target="_blank">System.String</a><br />Html text</dd></dl>

#### Return Value
Type: <a href="http://msdn2.microsoft.com/en-us/library/s1wwdcbf" target="_blank">String</a><br />Text without html tags

#### Implements
<a href="1c7f0645-ac53-8068-828b-4e57ab56d3c1">IHtmlConverter.ToPlainText(String)</a><br />

## See Also


#### Reference
<a href="1176c414-3ff1-d7bf-649a-08c500dd4548">RegExHtmlConverter Class</a><br /><a href="31c6ebbe-d683-7561-7308-5a5ee1f76bf5">MailMergeLib Namespace</a><br />