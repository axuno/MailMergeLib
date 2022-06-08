using SmartFormat;
using SmartFormat.Core.Extensions;
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
    internal MailSmartFormatter() : base()
    {
        // Register all default extensions here:
        // Add all extensions:
        // Note, the order is important; the extensions
        // will be executed in this order:
        var listFormatter = new ListFormatter(this);
            
        AddExtensions(
            (ISource)listFormatter, // ListFormatter MUST be first
            new DictionarySource(this),
            new ValueTupleSource(this),
            new JsonSource(this),
            //new XmlSource(this),
            new ReflectionSource(this),
            // The DefaultSource reproduces the string.Format behavior:
            new DefaultSource(this)
        );
        AddExtensions(
            (IFormatter)listFormatter,
            new PluralLocalizationFormatter("en"),
            new ConditionalFormatter(),
            new TimeFormatter("en"),
            //new XElementFormatter(),
            new ChooseFormatter(),
            new DefaultFormatter()
        );

        Templates = new TemplateFormatter(this);
        AddExtensions(Templates);
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
        if (sfConfig == null) return;

        Settings.FormatErrorAction = sfConfig.FormatErrorAction;
        Settings.ParseErrorAction = sfConfig.ParseErrorAction;
        Settings.CaseSensitivity = sfConfig.CaseSensitivity;
        Settings.ConvertCharacterStringLiterals = sfConfig.ConvertCharacterStringLiterals;
    }
}