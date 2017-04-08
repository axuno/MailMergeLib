# SenderConfig Properties
 

The <a href="73aa3de0-d281-a929-3ce3-ceec3337bc3b">SenderConfig</a> type exposes the following members.


## Properties
&nbsp;<table><tr><th></th><th>Name</th><th>Description</th></tr><tr><td>![Public property](media/pubproperty.gif "Public property")</td><td><a href="d3eb2f94-f281-25e2-c0c8-b76373e44853">MaxNumOfSmtpClients</a></td><td>
Gets or sets the maximum number of SmtpClient to send messages concurrently. Valid numbers are 1 to 50, defaults to 5.</td></tr><tr><td>![Public property](media/pubproperty.gif "Public property")</td><td><a href="a6dfb929-43b8-3b75-40f7-431f07b76649">SmtpClientConfig</a></td><td>
Gets or sets the array of configurations the SmtpClients will use. The first SmtpClientConfig is the "standard", any second is the "backup". Other instances of SmtpClientConfig in the array are used for parallel sending messages.</td></tr></table>&nbsp;
<a href="#senderconfig-properties">Back to Top</a>

## See Also


#### Reference
<a href="73aa3de0-d281-a929-3ce3-ceec3337bc3b">SenderConfig Class</a><br /><a href="31c6ebbe-d683-7561-7308-5a5ee1f76bf5">MailMergeLib Namespace</a><br />