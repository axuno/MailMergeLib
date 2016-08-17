using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using MailMergeLib.SmartFormatMail;
using MailMergeLib.SmartFormatMail.Core.Extensions;
using MailMergeLib.SmartFormatMail.Core.Settings;
using MailMergeLib.SmartFormatMail.Extensions;

namespace MailMergeLib
{
	/// <summary>
	/// The formatter used by MailMergeLib for replacing placeholders with variables' content.
	/// </summary>
	/// <remarks>
	/// MailSmartFormatter derives from SmartFormatter.
	/// SmartFormatter was extend with public HashSet&lt;string&gt; properties MissingVariables and MissingFormatters.
	/// This way we can set formatErrorAction = ErrorAction.Ignore, track missing variables and decide for throwing an exception later when generating the MimeMessage.
	/// </remarks>
	/// <example>
	/// Assume an object x with property "Name"
	/// a) Format depending on Name is null or string.Empty or other: Format("{Name:choose(null|):N/A|empty|{Name}}", x);
	/// b) Format DateTime requires the "named formatter" with name "default" because of the addition colons in the time format: SmartFormatter.Format("{Now:default:dd.MM.yyyy hh:mm:ss}", DateTime.Now);
	/// </example>
	public class MailSmartFormatter : SmartFormatter
	{
		/// <summary>
		/// CTOR.
		/// Create an instance which loads the Formatters and Source extensions required by MailMergeLib.
		/// Error actions are SmartFormatters defaults.
		/// </summary>
		public MailSmartFormatter() : base()
		{
			// Register all default extensions here:
			// Add all extensions:
			// Note, the order is important; the extensions
			// will be executed in this order:
			var listFormatter = new ListFormatter(this);

			base.AddExtensions(
				(ISource)listFormatter,
				new ReflectionSource(this),
				new DictionarySource(this),
				new XmlSource(this),
				// These default extensions reproduce the String.Format behavior:
				new DefaultSource(this)
				);
			base.AddExtensions(
				(IFormatter)listFormatter,
				new PluralLocalizationFormatter("en"),
				new ConditionalFormatter(),
				new TimeFormatter("en"),
				new XElementFormatter(),
				new ChooseFormatter(),
				new DefaultFormatter()
				);
		}

		/// <summary>
		/// CTOR.
		/// Create an instance which loads the Formatters and Source extensions required by MailMergeLib.
		/// </summary>
		/// <param name="parseErrorAction">Error behavior if parsing fails.</param>
		/// <param name="formatErrorAction">Error behavior if formatting fails.</param>
		public MailSmartFormatter(ErrorAction parseErrorAction = ErrorAction.ThrowError, ErrorAction formatErrorAction = ErrorAction.Ignore) : this()
		{
			base.Parser.ErrorAction = parseErrorAction;
			base.ErrorAction = formatErrorAction;
		}
	}
}
