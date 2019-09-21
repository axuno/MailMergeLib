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
using NUnit.Framework;

namespace UnitTests
{
    [TestFixture]
    public class Sender_SendMimeMessage
    {
        [TestCase(typeof(SmtpCommandException))]
        [TestCase(typeof(SmtpProtocolException))]
        [TestCase(typeof(IOException))]
        [TestCase(typeof(Exception))]
        public void With_ConnectException(Type connectException)
        {
            var sender = GetMailMergeSender();
            var msg = GetMimeMessage();
            var config = new SmtpClientConfig { MessageOutput = MessageOutput.SmtpServer, NetworkCredential = new Credential()};
            sender.Config.SmtpClientConfig[0] = config;

            Exception exception;

            if (connectException == typeof(SmtpCommandException))
            {
                exception = new SmtpCommandException(SmtpErrorCode.UnexpectedStatusCode, SmtpStatusCode.CommandNotImplemented, "unitTest");
            } else if (connectException == typeof(SmtpProtocolException))
            {
                exception = new SmtpProtocolException();
            } else if (connectException == typeof(IOException))
            {
                exception = new IOException();
            } else if (connectException == typeof(Exception))
            {
                exception = new Exception();
            }
            else
            {
                throw new ArgumentOutOfRangeException();
            }

            var smtpClient = new FakeSmtpClient {ConnectException = exception};

            Assert.Throws(connectException, () => sender.SendMimeMessage(smtpClient, msg, config));
            Assert.ThrowsAsync(connectException, async () => await sender.SendMimeMessageAsync(smtpClient, msg, config));
        }

        [TestCase(typeof(AuthenticationException))]
        [TestCase(typeof(SmtpCommandException))]
        [TestCase(typeof(SmtpProtocolException))]
        [TestCase(typeof(Exception))]
        public void With_AuthenticateException(Type authenticateException)
        {
            var sender = GetMailMergeSender();
            var msg = GetMimeMessage();
            var config = new SmtpClientConfig { MessageOutput = MessageOutput.SmtpServer, NetworkCredential = new Credential() };
            sender.Config.SmtpClientConfig[0] = config;

            Exception exception;

            if (authenticateException == typeof(AuthenticationException))
            {
                exception = new AuthenticationException();
            }
            else if (authenticateException == typeof(SmtpCommandException))
            {
                exception = new SmtpCommandException(SmtpErrorCode.UnexpectedStatusCode, SmtpStatusCode.CommandNotImplemented, "unitTest");
            }
            else if (authenticateException == typeof(SmtpProtocolException))
            {
                exception = new SmtpProtocolException();
            }
            else if (authenticateException == typeof(IOException))
            {
                exception = new IOException();
            }
            else if (authenticateException == typeof(Exception))
            {
                exception = new Exception();
            }
            else
            {
                throw new ArgumentOutOfRangeException();
            }

            var smtpClient = new FakeSmtpClient { AuthenticateException = exception };

            Assert.Throws(authenticateException, () => sender.SendMimeMessage(smtpClient, msg, config));
            Assert.ThrowsAsync(authenticateException, async () => await sender.SendMimeMessageAsync(smtpClient, msg, config));
        }

        [TestCase(typeof(SmtpCommandException))]
        [TestCase(typeof(SmtpCommandException), SmtpErrorCode.RecipientNotAccepted)]
        [TestCase(typeof(SmtpCommandException), SmtpErrorCode.SenderNotAccepted)]
        [TestCase(typeof(SmtpCommandException), SmtpErrorCode.MessageNotAccepted)]
        [TestCase(typeof(SmtpProtocolException))]
        [TestCase(typeof(Exception))]
        public void With_SendException(Type sendException, SmtpErrorCode smtpErrorCode = SmtpErrorCode.UnexpectedStatusCode)
        {
            var sender = GetMailMergeSender();
            var msg = GetMimeMessage();
            var config = new SmtpClientConfig { MessageOutput = MessageOutput.SmtpServer, NetworkCredential = new Credential() };
            sender.Config.SmtpClientConfig[0] = config;

            Exception exception;

            if (sendException == typeof(AuthenticationException))
            {
                exception = new AuthenticationException();
            }
            else if (sendException == typeof(SmtpCommandException))
            {
                exception = new SmtpCommandException(smtpErrorCode, SmtpStatusCode.CommandNotImplemented, "unitTest");
            }
            else if (sendException == typeof(SmtpProtocolException))
            {
                exception = new SmtpProtocolException();
            }
            else if (sendException == typeof(IOException))
            {
                exception = new IOException();
            }
            else if (sendException == typeof(Exception))
            {
                exception = new Exception();
            }
            else
            {
                throw new ArgumentOutOfRangeException();
            }

            var smtpClient = new FakeSmtpClient { SendException = exception };

            Assert.Throws(sendException, () => sender.SendMimeMessage(smtpClient, msg, config));
            Assert.ThrowsAsync(sendException, async () => await sender.SendMimeMessageAsync(smtpClient, msg, config));
        }

        private MailMergeSender GetMailMergeSender()
        {
            var sender = new MailMergeSender {GetInitializedSmtpClientDelegate = config => new FakeSmtpClient()};
            return sender;
        }

        private MimeMessage GetMimeMessage()
        {
            var mmm = new MailMergeMessage("subject", "plain text");
            mmm.MailMergeAddresses.Add(new MailMergeAddress(MailAddressType.To, "test@example.org"));
            mmm.MailMergeAddresses.Add(new MailMergeAddress(MailAddressType.From, "test2@example.org"));
            return mmm.GetMimeMessage();
        }
    }
}