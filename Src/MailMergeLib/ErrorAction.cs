namespace MailMergeLib;

/// <summary>
/// Determines how format and parsing errors are handled.
/// </summary>
/// <remarks>
/// Enum added for API compatibility when migrating from SmartFormat 2.7.3 to 3.2.1
/// </remarks>
public enum ErrorAction
{
    /// <summary>Throws an exception.  This is only recommended for debugging, so that formatting errors can be easily found.</summary>
    ThrowError,

    /// <summary>Includes an issue message in the output</summary>
    OutputErrorInResult,

    /// <summary>Ignores errors and tries to output the data anyway</summary>
    Ignore,

    /// <summary>Leaves invalid tokens unmodified in the text.</summary>
    MaintainTokens
}