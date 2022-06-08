using System;
using SmartFormat.Core.Parsing;
using SmartFormat.Core.Settings;

namespace MailMergeLib;

/// <summary>
/// SmartFormatter configuration.
/// </summary>
public class SmartFormatterConfig
{
    private ErrorAction _parseErrorAction = ErrorAction.ThrowError;
    private ErrorAction _formatErrorAction = ErrorAction.Ignore;
    private CaseSensitivityType _caseSensitivity = CaseSensitivityType.CaseSensitive;
    private bool _convertCharacterStringLiterals = true;

    /// <summary>
    /// Behavior of the parser in case of errors.
    /// </summary>
    public ErrorAction ParseErrorAction
    {
        get => _parseErrorAction;
        set
        {
            _parseErrorAction = value;
            OnConfigChanged?.Invoke(this);
        }
    }

    /// <summary>
    /// Behavior of the formatter in case of errors.
    /// </summary>
    public ErrorAction FormatErrorAction
    {
        get => _formatErrorAction;
        set
        {
            _formatErrorAction = value;
            OnConfigChanged?.Invoke(this);
        }
    }

    /// <summary>
    /// Determines whether placeholders are case-sensitive or not.
    /// Default is case-sensitive.
    /// </summary>
    public CaseSensitivityType CaseSensitivity
    {
        get => _caseSensitivity;
        set
        {
            _caseSensitivity = value;
            OnConfigChanged?.Invoke(this);
        }
    }

    /// <summary>
    /// This setting is relevant for the <see cref="LiteralText"/>.
    /// If true (the default), character string literals are treated like in "normal" string.Format:
    ///    string.Format("\t")   will return a "TAB" character
    /// If false, character string literals are not converted, just like with this string.Format:
    ///    string.Format(@"\t")  will return the 2 characters "\" and "t"
    /// </summary>
    public bool ConvertCharacterStringLiterals
    {
        get => _convertCharacterStringLiterals;
        set
        {
            _convertCharacterStringLiterals = value;
            OnConfigChanged?.Invoke(this);
        }
    }

    /// <summary>
    /// Event raising when the <see cref="SmartFormatterConfig"/> configuration has changed.
    /// </summary>
    public event Action<SmartFormatterConfig>? OnConfigChanged;

    #region *** Equality ***

    private bool Equals(SmartFormatterConfig other)
    {
        return ParseErrorAction == other.ParseErrorAction && FormatErrorAction == other.FormatErrorAction &&
               CaseSensitivity == other.CaseSensitivity;
    }

    /// <summary>
    /// Determines whether this instance is equal to another instance.
    /// </summary>
    /// <param name="obj"></param>
    /// <returns>Returns true, if both SmartFormatterConfigs are equal, else false.</returns>
    public override bool Equals(object obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((SmartFormatterConfig) obj);
    }

    /// <summary>
    /// The HashCode for the SmartFormatterConfig.
    /// </summary>
    /// <returns>Returns the HashCode for the SmartFormatterConfig.</returns>
    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = (int) ParseErrorAction;
            hashCode = (hashCode * 397) ^ (int) FormatErrorAction;
            hashCode = (hashCode * 397) ^ (int) CaseSensitivity;
            return hashCode;
        }
    }

    #endregion
}