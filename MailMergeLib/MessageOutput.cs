namespace MailMergeLib
{
    /// <summary>
    /// Enumeration of message output types
    /// </summary>
    public enum MessageOutput
    {
        /// <summary>
        /// Will process all messages but discard them just before sending / writing to disk.
        /// </summary>
        None,
        /// <summary>
        /// Send messages through an SMTP server
        /// </summary>
        SmtpServer,
        /// <summary>
        /// Writes messages to the specified MailOutputDirectory.
        /// </summary>
        Directory
#if NETFRAMEWORK
            ,
        
        /// <summary>
        /// Think twice about using the option &quot;IIS Pickup Directory&quot;. Then make sure that:
        /// 1. SMTP is installed
        /// 2. SMTP is configured
        /// 3. Firewall is open
        /// 4. IIS has access to the metabase
        /// 5. IIS has access to the pickup directory
        /// Otherwise you'll expect an SmtpException while method GetPickDirectoryFromIis() is called 
        /// </summary>
        PickupDirectoryFromIis
#endif
    }
}