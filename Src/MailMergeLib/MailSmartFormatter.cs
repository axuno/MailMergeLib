using SmartFormat;
using SmartFormat.Core.Settings;
using SmartFormat.Extensions;

namespace MailMergeLib;

/// <summary>
/// The formatter used by MailMergeLib for replacing placeholders with variables' content.
/// </summary>
/// <example>
/// Assume an object x with property "Name"
/// a) Format depending on Name is null or string.Empty or other: Format("{Name:choose(null|):N/A|empty|{Name}}", x);
/// b) Format DateTime requires the "named formatter" with name "default" because of the additional colons in the time format: SmartFormatter.Format("{Now:default:dd.MM.yyyy hh:mm:ss}", DateTime.Now);
/// </example>
public class MailSmartFormatter : SmartFormatter
{
    internal MailSmartFormatter()
    {
        Templates = new TemplateFormatter();
        
        // Default sources from Smart.CreateDefaultSmartFormat() v3.2.1
        AddExtensions(
            new StringSource(), 
            new ListFormatter(), 
            new DictionarySource(),
            new ValueTupleSource(), 
            new ReflectionSource(), 
            new DefaultSource(),
            new KeyValuePairSource())

            // Default formatters from Smart.CreateDefaultSmartFormat() v3.2.1
            .AddExtensions(new PluralLocalizationFormatter(),
                new ConditionalFormatter(), 
                new IsMatchFormatter(), 
                new NullFormatter(),
                new ChooseFormatter(),
                new SubStringFormatter(),
                // The DefaultSource reproduces the string.Format behavior:
                new DefaultFormatter())

            // Extensions to keep API compatibility with MailMergeLib v5.x
            .AddExtensions(
                new NewtonsoftJsonSource())
            .AddExtensions(
                new TimeFormatter { CanAutoDetect = false },
                Templates);
    }

    /// <summary>
    /// CTOR.
    /// Create an instance which loads the Formatters and Source extensions required by MailMergeLib.
    /// Error actions are SmartFormatters defaults.
    /// </summary>
    /// <param name="config"></param>
    internal MailSmartFormatter(SmartFormatterConfig config) : this()
    {
        SetConfig(config);
    }

    /// <summary>
    /// Gets or sets the <see cref="TemplateFormatter"/> where the templates can be registered later on.
    /// </summary>
    internal TemplateFormatter? Templates { get; set; }
    
    internal void SetConfig(SmartFormatterConfig sfConfig)
    {
        Settings.Formatter.ErrorAction = (FormatErrorAction) sfConfig.FormatErrorAction;
        Settings.Parser.ErrorAction = (ParseErrorAction) sfConfig.ParseErrorAction;
        Settings.CaseSensitivity = (SmartFormat.Core.Settings.CaseSensitivityType) sfConfig.CaseSensitivity;
        Settings.Parser.ConvertCharacterStringLiterals = sfConfig.ConvertCharacterStringLiterals;
    }
}