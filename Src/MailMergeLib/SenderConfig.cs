using System.Linq;
using YAXLib.Attributes;
using YAXLib.Enums;

namespace MailMergeLib;

/// <summary>
/// Configuration for MailMergeSender.
/// </summary>
[YAXSerializableType(FieldsToSerialize = YAXSerializationFields.AttributedFieldsOnly)]
public class SenderConfig
{
    /// <summary>
    /// CTOR for MailMergeSender configuration.
    /// </summary>
    public SenderConfig()
    {}

    /// <summary>
    /// Gets or sets the maximum number of SmtpClient to send messages concurrently.
    /// Valid numbers are 1 to 50, defaults to 5.
    /// </summary>
    [YAXSerializableField]
    public int MaxNumOfSmtpClients
    {
        get => field;
        set
        {
            if (value <= 0) field = 1;
            else if (value > 50) field = 50;
            else field = value;
        }
    } = 5;

    /// <summary>
    /// Gets or sets the array of configurations the SmtpClients will use.
    /// The first SmtpClientConfig is the "standard", any second is the "backup".
    /// Other instances of SmtpClientConfig in the array are used for parallel sending messages.
    /// </summary>
    [YAXSerializableField]
    [YAXSerializeAs("SmtpClients")]
    public SmtpClientConfig[] SmtpClientConfig { get; set; } = [new SmtpClientConfig()];

    #region *** Equality ***

    /// <summary>
    /// Checks for equality
    /// </summary>
    protected bool Equals(SenderConfig other)
    {
        if (MaxNumOfSmtpClients != other.MaxNumOfSmtpClients || SmtpClientConfig.Length != other.SmtpClientConfig.Length)
            return false;

        return !SmtpClientConfig.Where((t, i) => !t.Equals(other.SmtpClientConfig[i])).Any();
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((SenderConfig) obj);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        unchecked
        {
            return (MaxNumOfSmtpClients * 397) ^ (SmtpClientConfig?.GetHashCode() ?? 0);
        }
    }

    #endregion
}
