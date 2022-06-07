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

        Assert.IsTrue(sc1.Equals(sc2));
        Assert.IsFalse(sc1.Equals(null));
        Assert.IsFalse(sc1.Equals(new object()));
    }

    [Test]
    public void NotEqual()
    {
        var sc1 = new SenderConfig();
        var sc2 = new SenderConfig {MaxNumOfSmtpClients = 99999 };

        Assert.IsFalse(sc1.Equals(sc2));
        Assert.IsFalse(sc1.Equals(null));
        Assert.IsFalse(sc1.Equals(new object()));
    }
}