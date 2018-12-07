using System;
using System.Net;
using System.Reflection;
using MailKit;
using MailKit.Security;
using System.Security.Cryptography.X509Certificates;
using System.Security.Authentication;
using MailMergeLib.Serialization;
using YAXLib;
#if NET45
using System.Configuration;
using System.Net.Configuration;
#endif

namespace MailMergeLib
{
    /// <summary>
    /// Class which is used by MailMergeSender in order to build preconfigured SmtpClients.
    /// </summary>
    [YAXSerializableType(FieldsToSerialize = YAXSerializationFields.AttributedFieldsOnly)]
    public class SmtpClientConfig
    {
        private int _maxFailures = 2;
        private int _retryDelayTime;
        private string _mailOutputDirectory = null;

        /// <summary>
        /// Creates a new instance of the configuration which is used by MailMergeSender in order to build a preconfigured SmtpClient.
        /// </summary>
        public SmtpClientConfig()
        {
            MailOutputDirectory = System.IO.Path.GetTempPath();
        }

#if NET45
        /// <summary>
        /// If MailMergeLib runs on an IIS web application, it can load the following settings from system.net/mailSettings/smtp configuration section of web.donfig:
        /// DeliveryMethod, MessageOutput, EnableSsl, Network.UserName, Network.Password, Network.Host, Network.Port, Network.ClientDomain
        /// </summary>
        public void ReadSmtpConfigurationFromWebConfig()
        {
            var smtpSection = ConfigurationManager.GetSection("system.net/mailSettings/smtp") as SmtpSection;
            if (smtpSection == null) return;

            switch (smtpSection.DeliveryMethod)
            {
                case System.Net.Mail.SmtpDeliveryMethod.Network:
                    MessageOutput = MessageOutput.SmtpServer;
                    break;
                case System.Net.Mail.SmtpDeliveryMethod.PickupDirectoryFromIis:
                    MessageOutput = MessageOutput.PickupDirectoryFromIis;
                    break;
                case System.Net.Mail.SmtpDeliveryMethod.SpecifiedPickupDirectory:
                    MessageOutput = MessageOutput.Directory;
                    _mailOutputDirectory = string.IsNullOrEmpty(smtpSection.SpecifiedPickupDirectory?.PickupDirectoryLocation) ? null : smtpSection.SpecifiedPickupDirectory.PickupDirectoryLocation;
                    break;
            }

            if (smtpSection.Network.EnableSsl) SecureSocketOptions = SecureSocketOptions.Auto;

            if (!string.IsNullOrEmpty(smtpSection.Network.UserName) && !string.IsNullOrEmpty(smtpSection.Network.Password))
                NetworkCredential = new Credential(smtpSection.Network.UserName, smtpSection.Network.Password);
            
            SmtpPort = smtpSection.Network.Port;
            SmtpHost = smtpSection.Network.Host;
            ClientDomain = smtpSection.Network.ClientDomain;
        }
#endif
        /// <summary>
        /// Get or sets the name of configuration.
        /// It's recommended to choose different names for each configuration.
        /// </summary>
        [YAXSerializableField]
        [YAXAttributeForClass]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the name or IP address of the SMTP host to be used for sending mails.
        /// </summary>
        /// <remarks>Used during SmtpClient connect.</remarks>
        [YAXSerializableField]
        public string SmtpHost { get; set; } = "localhost";

        /// <summary>
        /// Gets or set the port of the SMTP host to be used for sending mails.
        /// </summary>
        /// <remarks>Used during SmtpClient connect.</remarks>
        [YAXSerializableField]
        public int SmtpPort { get; set; } = 25;

        /// <summary>
        /// Gets or sets the name of the local machine sent to the SMTP server with the hello command
        /// of an SMTP transaction. Defaults to the windows machine name.
        /// </summary>
        [YAXSerializableField]
        public string ClientDomain { get; set; }

        /// <summary>
        /// Gets or sets the local IP end point or null to use the default end point.
        /// </summary>
        [YAXSerializableField]
        [YAXCustomSerializer(typeof(IPEndPointSerializer))]
        public IPEndPoint LocalEndPoint { get; set; }

        /// <summary>
        /// Gets the collection of certificates the <see cref="MailKit.Net.Smtp.SmtpClient"/> will use.
        /// </summary>
        /// <example>
        /// Add a certificate using a PFX file (i.e. a PKCS#12 archive bag):
        /// ClientCertificates.Add(new X509Certificate2("path_to_cert_file.pfx", "optional password"));
        /// Add a certificate using a cerificate file in PEM format:
        /// ClientCertificates.Add(X509Certificate.CreateFromCertFile("path_to_cert_file.pem"));
        /// </example>
        [YAXDontSerialize]
        public X509CertificateCollection ClientCertificates { get; } = new X509CertificateCollection();

        /// <summary>
        /// Verifies the remote Secure Sockets Layer (SSL) certificate used for authentication.
        /// </summary>
        [YAXDontSerialize]
        public System.Net.Security.RemoteCertificateValidationCallback ServerCertificateValidationCallback { get; set; }

        /// <summary>
        /// Set authentification details for logging into an SMTP server.
        /// Set NetworkCredential to null if no authentification is required.
        /// </summary>
        /// <remarks>Used during SmtpClient connect.</remarks>
        /// <remarks>Will be serialized with attribute yaxlib:realtype="MailMergeLib.Credential" if assigned to an instance of MailMergeLib.Credential.</remarks>
        [YAXSerializableField]
        public ICredentials NetworkCredential { get; set; }

        /// <summary>
        /// Gets or sets the name of the output directory of sent mail messages
        /// (only used if messages are not sent to SMTP server)
        /// </summary>
        [YAXSerializableField]
        public string MailOutputDirectory
        {
            get
            {
                switch (MessageOutput)
                {
                    case MessageOutput.None:
                    case MessageOutput.SmtpServer:
                        return null;
                    case MessageOutput.Directory:
                        return _mailOutputDirectory ?? System.IO.Path.GetTempPath();
#if NET45
                    case MessageOutput.PickupDirectoryFromIis:
                        return GetPickDirectoryFromIis();
#endif
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            set { _mailOutputDirectory = value; }
        }

        /// <summary>
        /// Gets or sets the location where to send mail messages.
        /// </summary>
        [YAXSerializableField]
        public MessageOutput MessageOutput { get; set; } = MessageOutput.None;

        /// <summary>
        /// Gets or sets the SSL/TLS protocols the <see cref="MailKit.Net.Smtp.SmtpClient"/> is allowed to use.
        /// </summary>
        [YAXSerializableField]
        public SslProtocols SslProtocols { get; set; } = SslProtocols.None;

        /// <summary>
        /// Gets or sets the SecureSocketOptions the <see cref="MailKit.Net.Smtp.SmtpClient"/> will use (e.g. SSL or STARTLS
        /// In case a secure socket is needed, setting options to SecureSocketOptions.Auto is recommended.
        /// </summary>
        /// <remarks>Used during <see cref="MailKit.Net.Smtp.SmtpClient"/> connect.</remarks>
        [YAXSerializableField]
        public SecureSocketOptions SecureSocketOptions { get; set; } = SecureSocketOptions.None;

        /// <summary>
        /// Gets or sets the timeout for sending a message, after which a time-out exception will raise.
        /// Timeout value in milliseconds. The default value is 100,000 (100 seconds). 
        /// </summary>
        [YAXSerializableField]
        public int Timeout { get; set; } = 100000;

        /// <summary>
        /// The delegate for an <see cref="IProtocolLogger"/> that <see cref="MailKit.Net.Smtp.SmtpClient"/> will use to log the dialogue with the SMTP server.
        /// This logger is dedicated to debugging, not for production use.
        /// </summary>
        /// <remarks>
        /// Have in mind that MailMergeLib may use several <see cref="MailKit.Net.Smtp.SmtpClient"/> concurrently.
        /// The delegate is called when creating a new instance of an <see cref="MailKit.Net.Smtp.SmtpClient"/> is created.
        /// </remarks>
        /// <example>
        /// ProtocolLoggerDelegate = () =&gt; new ProtocolLogger(System.IO.Path.Combine("targetDirectory", "Smtp-" + System.IO.Path.GetRandomFileName() + ".log"));
        /// </example>
        [YAXDontSerialize]
        public Func<IProtocolLogger> ProtocolLoggerDelegate;

        /// <summary>
        /// Gets or sets the delay time in milliseconds (0-10000) between the messages.
        /// In case more than one SmtpClient will be used concurrently, the delay will be used per thread.
        /// Mainly used for debug purposes.
        /// </summary>
        [YAXSerializableField]
        public int DelayBetweenMessages { get; set; }

        /// <summary>
        /// Gets or sets the number of failures (1-10) for which a retry to send will be performed.
        /// </summary>
        [YAXSerializableField]
        public int MaxFailures
        {
            get { return _maxFailures; }
            set { _maxFailures = (value >= 1 && value <= 10) ? value : 1; }
        }

        /// <summary>
        /// Gets or sets the delay time in milliseconds (0-10000) to elaps between retries to send the message.
        /// </summary>
        [YAXSerializableField]
        public int RetryDelayTime
        {
            get { return _retryDelayTime; }
            set { _retryDelayTime = (value >= 0 && value <= 10000) ? value : 0; }
        }

#if NET45
        private static string GetPickDirectoryFromIis()
        {
            try
            {
                var internalType = typeof(System.Net.Mail.SmtpClient).Assembly.GetType("System.Net.Mail.IisPickupDirectory");
                if (internalType == null)
                {
                    throw new NotImplementedException("Assembly does not contain type System.Net.Mail.IisPickupDirectory");
                }
                var smtpClient = new System.Net.Mail.SmtpClient();

                var methodInfo = internalType.GetMethod("GetPickupDirectory", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                if (methodInfo == null)
                {
                    throw new NotImplementedException("System.Net.Mail.IisPickupDirectory does not contain a method GetPickupDirectory()");
                }
                var obj = methodInfo.Invoke(smtpClient, null);

                return obj as string;
            }
            catch (TargetInvocationException ex)
            {
                // most likely an SmtpException will throw
                throw ex.InnerException ?? ex;
            }
        }
#endif

        #region *** Equality ***

        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        /// <remarks>
        /// Excluding those properties which are not serialized:
        /// ClientCertificates, ServerCertificateValidationCallback, NetworkCredential, ProtocolLoggerDelegate
        /// </remarks>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((SmtpClientConfig)obj);
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        /// <remarks>
        /// Excluding those properties which are not serialized:
        /// ClientCertificates, ServerCertificateValidationCallback, NetworkCredential, ProtocolLoggerDelegate
        /// </remarks>
        protected bool Equals(SmtpClientConfig other)
        {
            return MaxFailures == other.MaxFailures && RetryDelayTime == other.RetryDelayTime &&
                   string.Equals(MailOutputDirectory, other.MailOutputDirectory) &&
                   string.Equals(Name, other.Name) &&
                   string.Equals(SmtpHost, other.SmtpHost) && SmtpPort == other.SmtpPort &&
                   string.Equals(ClientDomain, other.ClientDomain) && Equals(LocalEndPoint, other.LocalEndPoint) &&
                   MessageOutput == other.MessageOutput &&
                   SslProtocols == other.SslProtocols && SecureSocketOptions == other.SecureSocketOptions &&
                   Timeout == other.Timeout && DelayBetweenMessages == other.DelayBetweenMessages;
        }

        /// <summary>
        ///  Returns the hastcode for this object.
        /// </summary>
        /// <returns></returns>
        /// <remarks>
        /// Excluding those properties which are not serialized:
        /// ClientCertificates, ServerCertificateValidationCallback, NetworkCredential, ProtocolLoggerDelegate
        /// </remarks>
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = MaxFailures;
                hashCode = (hashCode * 397) ^ RetryDelayTime;
                hashCode = (hashCode * 397) ^ (MailOutputDirectory != null ? MailOutputDirectory.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Name != null ? Name.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (SmtpHost != null ? SmtpHost.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ SmtpPort;
                hashCode = (hashCode * 397) ^ (ClientDomain != null ? ClientDomain.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (LocalEndPoint != null ? LocalEndPoint.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (int) MessageOutput;
                hashCode = (hashCode * 397) ^ (int) SslProtocols;
                hashCode = (hashCode * 397) ^ (int) SecureSocketOptions;
                hashCode = (hashCode * 397) ^ Timeout;
                hashCode = (hashCode * 397) ^ DelayBetweenMessages;
                return hashCode;
            }
        }

        #endregion
    }
}
