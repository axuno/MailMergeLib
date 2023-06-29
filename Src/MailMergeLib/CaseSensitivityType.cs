namespace MailMergeLib;

/// <summary>
/// An enumeration of types defining whether string will be processed case-sensitive.
/// </summary>
/// <remarks>
/// Enum added for API compatibility when migrating from SmartFormat 2.7.3 to 3.2.1
/// </remarks>
public enum CaseSensitivityType
{
    /// <summary>String are processed case-sensitive.</summary>
    CaseSensitive,
    /// <summary>String are not processed case-sensitive.</summary>
    CaseInsensitive,
}