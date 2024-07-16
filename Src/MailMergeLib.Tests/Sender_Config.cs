using NUnit.Framework;

namespace MailMergeLib.Tests;

[TestFixture]
public class Sender_Config
{
    [Test]
    public void Equality()
    {
        var sc1 = new SenderConfig();
        var sc2 = new SenderConfig();

        Assert.Multiple(() =>
        {
            Assert.That(sc1.Equals(sc2), Is.True);
            Assert.That(sc1.Equals(new object()), Is.False);
        });
    }

    [Test]
    public void NotEqual()
    {
        var sc1 = new SenderConfig();
        var sc2 = new SenderConfig {MaxNumOfSmtpClients = 99999 };

        Assert.Multiple(() =>
        {
            Assert.That(sc1.Equals(sc2), Is.False);
            Assert.That(sc1.Equals(new object()), Is.False);
        });
    }
}