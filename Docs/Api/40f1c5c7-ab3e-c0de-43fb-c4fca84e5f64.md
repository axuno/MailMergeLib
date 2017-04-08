# MailMergeSender Class
 

Sends MailMergeMessages to an SMTP server. It uses MailKit.Net.Smtp.SmtpClient for low level operations.


## Inheritance Hierarchy
<a href="http://msdn2.microsoft.com/en-us/library/e5kfa45b" target="_blank">System.Object</a><br />&nbsp;&nbsp;MailMergeLib.MailMergeSender<br />
**Namespace:**&nbsp;<a href="31c6ebbe-d683-7561-7308-5a5ee1f76bf5">MailMergeLib</a><br />**Assembly:**&nbsp;MailMergeLib (in MailMergeLib.dll) Version: 5.2.0.1

## Syntax

**C#**<br />
``` C#
public class MailMergeSender : IDisposable
```

**VB**<br />
``` VB
Public Class MailMergeSender
	Implements IDisposable
```

The MailMergeSender type exposes the following members.


## Constructors
&nbsp;<table><tr><th></th><th>Name</th><th>Description</th></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="2ec9928d-2178-2718-e85a-4012c806fe3b">MailMergeSender</a></td><td>
CTOR</td></tr></table>&nbsp;
<a href="#mailmergesender-class">Back to Top</a>

## Properties
&nbsp;<table><tr><th></th><th>Name</th><th>Description</th></tr><tr><td>![Public property](media/pubproperty.gif "Public property")</td><td><a href="c43b10a6-2eda-443b-7d78-525552ef2733">Config</a></td><td>
The settings for a MailMergeSender.</td></tr><tr><td>![Public property](media/pubproperty.gif "Public property")</td><td><a href="5c5b6ff1-ce03-8ccb-8a48-effc7a32b1d3">IsBusy</a></td><td>
Returns true, while a Send method is pending. Entering a Send method while IsBusy will raise an InvalidOperationException.</td></tr></table>&nbsp;
<a href="#mailmergesender-class">Back to Top</a>

## Methods
&nbsp;<table><tr><th></th><th>Name</th><th>Description</th></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="61eb4abb-34ee-7d06-6566-5d06cd13821a">Dispose</a></td><td>
Releases all resources used by MailMergeSender</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="http://msdn2.microsoft.com/en-us/library/bsc2ak47" target="_blank">Equals</a></td><td>
Determines whether the specified object is equal to the current object.
 (Inherited from <a href="http://msdn2.microsoft.com/en-us/library/e5kfa45b" target="_blank">Object</a>.)</td></tr><tr><td>![Protected method](media/protmethod.gif "Protected method")</td><td><a href="6078be81-5749-e7bf-c149-1d17595756d5">Finalize</a></td><td>
Destructor.
 (Overrides <a href="http://msdn2.microsoft.com/en-us/library/4k87zsw7" target="_blank">Object.Finalize()</a>.)</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="http://msdn2.microsoft.com/en-us/library/zdee4b3y" target="_blank">GetHashCode</a></td><td>
Serves as the default hash function.
 (Inherited from <a href="http://msdn2.microsoft.com/en-us/library/e5kfa45b" target="_blank">Object</a>.)</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="http://msdn2.microsoft.com/en-us/library/dfwy45w9" target="_blank">GetType</a></td><td>
Gets the <a href="http://msdn2.microsoft.com/en-us/library/42892f65" target="_blank">Type</a> of the current instance.
 (Inherited from <a href="http://msdn2.microsoft.com/en-us/library/e5kfa45b" target="_blank">Object</a>.)</td></tr><tr><td>![Protected method](media/protmethod.gif "Protected method")</td><td><a href="http://msdn2.microsoft.com/en-us/library/57ctke0a" target="_blank">MemberwiseClone</a></td><td>
Creates a shallow copy of the current <a href="http://msdn2.microsoft.com/en-us/library/e5kfa45b" target="_blank">Object</a>.
 (Inherited from <a href="http://msdn2.microsoft.com/en-us/library/e5kfa45b" target="_blank">Object</a>.)</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="79e7ddd8-6cee-36d2-25a1-f65ec0cae1a5">Send(MailMergeMessage, Object)</a></td><td>
Sends a single mail merge message.</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="05f2ef60-16e7-113a-39d7-f9aa55217513">Send(T)(MailMergeMessage, IEnumerable(T))</a></td><td>
Sends mail messages syncronously to all recipients supplied in the data source of the mail merge message.</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="cb18e3c5-134b-7468-441a-8b0a77ec609a">SendAsync(MailMergeMessage, Object)</a></td><td>
Sends a single mail message asyncronously.</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="833911b1-d56d-8854-b1bc-5cc371bd9f91">SendAsync(T)(MailMergeMessage, IEnumerable(T))</a></td><td>
Sends mail messages asynchronously to all recipients supplied in the data source of the mail merge message.</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="5b8f691a-d308-05aa-5427-51c7b92e3afe">SendCancel</a></td><td>
Cancel any transactions sending or merging mail.</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="http://msdn2.microsoft.com/en-us/library/7bxwbwt2" target="_blank">ToString</a></td><td>
Returns a string that represents the current object.
 (Inherited from <a href="http://msdn2.microsoft.com/en-us/library/e5kfa45b" target="_blank">Object</a>.)</td></tr></table>&nbsp;
<a href="#mailmergesender-class">Back to Top</a>

## Events
&nbsp;<table><tr><th></th><th>Name</th><th>Description</th></tr><tr><td>![Public event](media/pubevent.gif "Public event")</td><td><a href="e13252a7-62d9-90d3-d7b5-572f9a8d18a6">OnAfterSend</a></td><td>
Event raising after sending a mail message.</td></tr><tr><td>![Public event](media/pubevent.gif "Public event")</td><td><a href="1dc80e13-b36a-39fc-71b6-5efdc18d0233">OnBeforeSend</a></td><td>
Event raising before sending a mail message.</td></tr><tr><td>![Public event](media/pubevent.gif "Public event")</td><td><a href="a0cee064-b4d1-22ab-ce9f-211fc5920b02">OnMergeBegin</a></td><td>
Event raising before starting with mail merge.</td></tr><tr><td>![Public event](media/pubevent.gif "Public event")</td><td><a href="48e78f62-58b4-3c1f-df14-25321990b8f5">OnMergeComplete</a></td><td>
Event raising after completing mail merge.</td></tr><tr><td>![Public event](media/pubevent.gif "Public event")</td><td><a href="5bdc7b78-09d1-2c9a-fa9c-b8ba70f2b6f1">OnMergeProgress</a></td><td>
Event raising during mail merge progress, i.e. after each message sent.</td></tr><tr><td>![Public event](media/pubevent.gif "Public event")</td><td><a href="52e318d0-a5bb-1300-61fd-499b5ab74ee8">OnMessageFailure</a></td><td>
Event raising before sending a mail message.</td></tr><tr><td>![Public event](media/pubevent.gif "Public event")</td><td><a href="d0ade639-f148-624a-05fb-3ae7a585cbeb">OnSendFailure</a></td><td>
Event raising, if an error occurs when sending a mail message.</td></tr></table>&nbsp;
<a href="#mailmergesender-class">Back to Top</a>

## See Also


#### Reference
<a href="31c6ebbe-d683-7561-7308-5a5ee1f76bf5">MailMergeLib Namespace</a><br />