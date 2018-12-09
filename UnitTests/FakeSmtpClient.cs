using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using MailMergeLib;
using MimeKit;

namespace UnitTests
{
    /// <inheritdoc />
    public class FakeSmtpClient : SmtpClient
    {
        public FakeSmtpClient()
        {
            // force the SmtpClient to "authenticate"
            var prop = typeof(SmtpClient).GetField("capabilities", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            prop?.SetValue(this, SmtpCapabilities.Authentication);
        }

        public Exception AuthenticateException { get; set; } = null;
        public Exception ConnectException { get; set; } = null;
        public Exception SendException { get; set; } = null;

        public override Task AuthenticateAsync(SaslMechanism mechanism, CancellationToken cancellationToken = new CancellationToken())
        {
            if (AuthenticateException != null) throw AuthenticateException;

            return Task.CompletedTask;
        }

        public override Task AuthenticateAsync(Encoding encoding, ICredentials credentials,
            CancellationToken cancellationToken = new CancellationToken())
        {
            if (AuthenticateException != null) throw AuthenticateException;

            return Task.CompletedTask;
        }

        public override Task ConnectAsync(string host, int port = 0, SecureSocketOptions options = SecureSocketOptions.Auto,
            CancellationToken cancellationToken = new CancellationToken())
        {
            if (ConnectException != null) throw ConnectException;

            return Task.CompletedTask;
        }

        public override Task ConnectAsync(Socket socket, string host, int port = 0, SecureSocketOptions options = SecureSocketOptions.Auto,
            CancellationToken cancellationToken = new CancellationToken())
        {
            if (ConnectException != null) throw ConnectException;

            return Task.CompletedTask;
        }

        public override Task ConnectAsync(Stream stream, string host, int port = 0, SecureSocketOptions options = SecureSocketOptions.Auto,
            CancellationToken cancellationToken = new CancellationToken())
        {
            if (ConnectException != null) throw ConnectException;

            return Task.CompletedTask;
        }

        public override Task DisconnectAsync(bool quit, CancellationToken cancellationToken = new CancellationToken())
        {
            return Task.CompletedTask;
        }

        public override Task NoOpAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            return Task.CompletedTask;
        }

        public override Task SendAsync(FormatOptions options, MimeMessage message,
            CancellationToken cancellationToken = new CancellationToken(), ITransferProgress progress = null)
        {
            throw new NotImplementedException();
        }

        public override Task SendAsync(FormatOptions options, MimeMessage message, MailboxAddress sender, IEnumerable<MailboxAddress> recipients,
            CancellationToken cancellationToken = new CancellationToken(), ITransferProgress progress = null)
        {
            throw new NotImplementedException();
        }

        protected override void OnNoRecipientsAccepted(MimeMessage message)
        {
            base.OnNoRecipientsAccepted(message);
        }

        public override void Authenticate(SaslMechanism mechanism, CancellationToken cancellationToken = new CancellationToken())
        {
            if (AuthenticateException != null) throw AuthenticateException;
        }

        public override void Authenticate(Encoding encoding, ICredentials credentials,
            CancellationToken cancellationToken = new CancellationToken())
        {
            if (AuthenticateException != null) throw AuthenticateException; // in use
        }

        public override void Connect(string host, int port = 0, SecureSocketOptions options = SecureSocketOptions.Auto,
            CancellationToken cancellationToken = new CancellationToken())
        {
            if (ConnectException != null) throw ConnectException; // in use
        }

        public override void Connect(Socket socket, string host, int port = 0, SecureSocketOptions options = SecureSocketOptions.Auto,
            CancellationToken cancellationToken = new CancellationToken())
        {
            if (ConnectException != null) throw ConnectException;
        }

        public override void Connect(Stream stream, string host, int port = 0, SecureSocketOptions options = SecureSocketOptions.Auto,
            CancellationToken cancellationToken = new CancellationToken())
        {
            if (ConnectException != null) throw ConnectException;
        }

        public override void Disconnect(bool quit, CancellationToken cancellationToken = new CancellationToken())
        {
            return;
        }

        public override void NoOp(CancellationToken cancellationToken = new CancellationToken())
        {
            return;
        }

        protected override void OnSenderAccepted(MimeMessage message, MailboxAddress mailbox, SmtpResponse response)
        {
            base.OnSenderAccepted(message, mailbox, response);
        }

        protected override void OnSenderNotAccepted(MimeMessage message, MailboxAddress mailbox, SmtpResponse response)
        {
            base.OnSenderNotAccepted(message, mailbox, response);
        }

        protected override string GetEnvelopeId(MimeMessage message)
        {
            return base.GetEnvelopeId(message);
        }

        protected override void OnRecipientAccepted(MimeMessage message, MailboxAddress mailbox, SmtpResponse response)
        {
            base.OnRecipientAccepted(message, mailbox, response);
        }

        protected override void OnRecipientNotAccepted(MimeMessage message, MailboxAddress mailbox, SmtpResponse response)
        {
            base.OnRecipientNotAccepted(message, mailbox, response);
        }

        protected override DeliveryStatusNotification? GetDeliveryStatusNotifications(MimeMessage message, MailboxAddress mailbox)
        {
            return base.GetDeliveryStatusNotifications(message, mailbox);
        }

        public override void Send(FormatOptions options, MimeMessage message, CancellationToken cancellationToken = new CancellationToken(),
            ITransferProgress progress = null)
        {
            if (SendException != null) throw SendException;
        }

        public override void Send(FormatOptions options, MimeMessage message, MailboxAddress sender, IEnumerable<MailboxAddress> recipients,
            CancellationToken cancellationToken = new CancellationToken(), ITransferProgress progress = null)
        {
            if (SendException != null) throw SendException;
        }

        /*
        public override object SyncRoot { get; }
        protected override string Protocol { get; }
        public override HashSet<string> AuthenticationMechanisms { get; }
        public override int Timeout { get; set; }
        public override bool IsConnected { get; }
        public override bool IsSecure { get; }
        public override bool IsAuthenticated { get; }
        */

        public override void Send(MimeMessage message, CancellationToken cancellationToken = new CancellationToken(),
            ITransferProgress progress = null)
        {
            if (SendException != null) throw SendException;  // in use
        }

        public override Task SendAsync(MimeMessage message, CancellationToken cancellationToken = new CancellationToken(),
            ITransferProgress progress = null)
        {
            if (SendException != null) throw SendException;

            return Task.CompletedTask;
        }

        public override void Send(MimeMessage message, MailboxAddress sender, IEnumerable<MailboxAddress> recipients,
            CancellationToken cancellationToken = new CancellationToken(), ITransferProgress progress = null)
        {
            if (SendException != null) throw SendException;
        }

        public override Task SendAsync(MimeMessage message, MailboxAddress sender, IEnumerable<MailboxAddress> recipients,
            CancellationToken cancellationToken = new CancellationToken(), ITransferProgress progress = null)
        {
            if (SendException != null) throw SendException;

            return Task.CompletedTask;
        }

        protected override void OnMessageSent(MessageSentEventArgs e)
        {
            base.OnMessageSent(e);
        }

        protected override void OnConnected(string host, int port, SecureSocketOptions options)
        {
            base.OnConnected(host, port, options);
        }

        protected override void OnDisconnected(string host, int port, SecureSocketOptions options, bool requested)
        {
            base.OnDisconnected(host, port, options, requested);
        }

        protected override void OnAuthenticated(string message)
        {
            base.OnAuthenticated(message);
        }
    }
}