using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace MailMergeLib
{
	/// <summary>
	/// Utility class that provides very basic ways to manipulate HTML tags.
	/// </summary>
	internal class HtmlTagHelper
	{
		private const string CStartTagMatch = @"(<\s*{0})([^>]*)(>)";
		private const string CAttrMatch = @"({0}\s*=\s*[""'])([^\""']*)([""'])";
		private const string CStartTagTextEndTagMatch = @"(<\s*{0})([^>]*)(>)(.*)(<\s*/{0}\s*>)";

		private const char CDelimiter = '"';
		private readonly FileInfo _file;
		private string _tagName;

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="tagName">HTML tag to work on.</param>
		/// <param name="file">FileInfo for file with HTML text.</param>
		public HtmlTagHelper(string tagName, FileInfo file)
		{
			StartTagsTextEndTags = new List<string>(100);
			StartTags = new List<string>(100);
			_tagName = tagName;
			_file = file;
			LoadHtmlFile();
			MakeStartTagCollection();
			MakeStartTagTextEndTagCollection();
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="tagName">HTML tag to work on.</param>
		/// <param name="html">HTML text.</param>
		public HtmlTagHelper(string tagName, string html)
		{
			StartTagsTextEndTags = new List<string>(100);
			StartTags = new List<string>(100);
			_tagName = tagName;
			HtmlText = new StringBuilder(html);
			MakeStartTagCollection();
			MakeStartTagTextEndTagCollection();
		}

		/// <summary>
		/// Gets or sets the HTML tag that shall be applied to the HTML text.
		/// </summary>
		public string TagName
		{
			get { return _tagName; }

			set
			{
				_tagName = value;
				MakeStartTagCollection();
				MakeStartTagTextEndTagCollection();
			}
		}

		/// <summary>
		/// Returns HTML start tags and also standalone tags,
		/// like &lt;a href="abc.html"&gt; and &lt;img src="abc.gif" /&gt; 
		/// </summary>
		public List<string> StartTags { get; private set; }

		/// <summary>
		/// Returns HTML start tags with all text and including their closing tags
		/// like &lt;a href="abc.html"&gt;My Link&lt;/a&gt;
		/// </summary>
		public List<string> StartTagsTextEndTags { get; private set; }

		/// <summary>
		/// Returns the HTML text.
		/// </summary>
		public StringBuilder HtmlText { get; private set; }

		/// <summary>
		/// Loads a HTML file into memory.
		/// </summary>
		private void LoadHtmlFile()
		{
			HtmlText = new StringBuilder();
			using (StreamReader sr = File.OpenText(_file.FullName))
			{
				HtmlText.Append(sr.ReadToEnd());
			}
		}

		/// <summary>
		/// Generates a list containing all the start tags and standalone tags of the TagName,
		/// like all &lt;title&gt; or &lt;img&gt; tags.
		/// </summary>
		private void MakeStartTagCollection()
		{
			StartTags.Clear();

			if (string.IsNullOrEmpty(_tagName)) return;

			var reTag = new Regex(string.Format(CStartTagMatch, _tagName),
			                      RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
			foreach (Match match in reTag.Matches(HtmlText.ToString()))
			{
				StartTags.Add(match.Value);
			}
		}

		/// <summary>
		/// Generates a list containing all the end tags of the TagName,
		/// like all &lt;/title&gt; tags.
		/// </summary>
		private void MakeStartTagTextEndTagCollection()
		{
			StartTagsTextEndTags.Clear();

			if (string.IsNullOrEmpty(_tagName)) return;

			var reTag = new Regex(string.Format(CStartTagTextEndTagMatch, _tagName),
			                      RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
			foreach (Match match in reTag.Matches(HtmlText.ToString()))
			{
				StartTagsTextEndTags.Add(match.Value);
			}
		}


		/// <summary>
		/// Gets the value of a specific attribute of the HTML tag.
		/// </summary>
		/// <param name="tag">Complete HTML tag, e.g. &lt;img src="abc.jpg"&lt;</param>
		/// <param name="attribute">Attribute of the tag which values shall be retrieved, like "src"</param>
		/// <returns></returns>
		public string GetAttributeValue(string tag, string attribute)
		{
			var reSrc = new Regex(string.Format(CAttrMatch, attribute), RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
			MatchCollection mc = reSrc.Matches(tag);
			if (mc.Count == 1 && mc[0].Groups.Count >= 3)
			{
				// Groups[0] = whole matching string, 1 = attribute, 2 = value, 3 = >
				return mc[0].Groups[2].Value;
			}
			return null;
		}

		/// <summary>
		/// Sets the value of a specific attribute of the HTML tag.
		/// </summary>
		/// <param name="tag">Complete HTML tag, e.g. &lt;img src="abc.jpg"&lt;</param>
		/// <param name="attribute">Attribute of the tag which values shall be set, like "src"</param>
		/// <param name="value">Value of the attribute, e.g. "abc.jpg"</param>
		/// <returns>The complete HTML tag with the attribute set.</returns>
		public string SetAttributeValue(string tag, string attribute, string value)
		{
			if (string.IsNullOrEmpty(tag) || string.IsNullOrEmpty(attribute) || string.IsNullOrEmpty(tag) || !tag.EndsWith(">"))
				return tag;

			var reSrc = new Regex(string.Format(CAttrMatch, attribute), RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
			MatchCollection mc = reSrc.Matches(tag);
			if (mc.Count == 1 && mc[0].Groups.Count >= 3)
			{
				// Groups[0] = whole matching string, 1 = attribute, 2 = value, 3 = >
				return tag.Replace(mc[0].Groups[0].Value, mc[0].Groups[1].Value + value + mc[0].Groups[3].Value);
			}

			// attribute does not yet exist
			tag = tag.TrimEnd();
			string tagEnd = tag.EndsWith("/>") ? "/>" : ">";
			return tag.Substring(0, tag.Length - tagEnd.Length) +
			       string.Format(" {0}={1}{2}{3} {4}", attribute, CDelimiter, value, CDelimiter, tagEnd);
		}

		/// <summary>
		/// Gets the text value between the starting and the ending HTML tag,
		/// like "This is the title" in &lt;title&gt;This is the title&lt;/title&gt;
		/// </summary>
		/// <param name="startValueEnd">A string with the start tag, text value, and end tag, e.g. &lt;title&gt;MyTitle&lt;/title&gt;</param>
		/// <returns>Text value between the starting and the ending HTML tag</returns>
		public string GetValueBetweenStartAndEndTag(string startValueEnd)
		{
			var reText = new Regex(string.Format(CStartTagTextEndTagMatch, _tagName),
			                       RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
			MatchCollection mc = reText.Matches(startValueEnd);
			if (mc.Count == 1 && mc[0].Groups.Count >= 6)
			{
				// Groups[0] = whole matching string, 1 = attribute, 2 = value, 3 = >
				return mc[0].Groups[4].Value;
			}
			return null;
		}

		/// <summary>
		/// Sets the text value between the starting and the ending HTML tag,
		/// like "This is the title" in &lt;title&gt;This is the title&lt;/title&gt;
		/// </summary>
		/// <param name="startValueEnd">A string with the start tag, text value, and end tag, e.g. &lt;title&gt;MyTitle&lt;/title&gt;</param>
		/// <param name="value">New value to be set.</param>
		/// <returns>A string with the starting tag, the new text value and the ending tag.</returns>
		public string SetValueBetweenStartAndEndTag(string startValueEnd, string value)
		{
			if (string.IsNullOrEmpty(startValueEnd) || string.IsNullOrEmpty(startValueEnd))
				return startValueEnd;

			var reText = new Regex(string.Format(CStartTagTextEndTagMatch, _tagName),
			                       RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
			MatchCollection mc = reText.Matches(startValueEnd);
			if (mc.Count == 1 && mc[0].Groups.Count == 6)
			{
				// Groups[0] = whole matching string, 1 = start tag <tag, 2 = attributes, 3 = >, 4 = value between 5 = end tag </tag>
				return startValueEnd.Replace(mc[0].Groups[0].Value,
				                             mc[0].Groups[1].Value + mc[0].Groups[2].Value + mc[0].Groups[3].Value + value +
				                             mc[0].Groups[5].Value);
			}
			return startValueEnd;
		}


		/// <summary>
		/// Replaces the old HTML tag with the new tag.
		/// </summary>
		/// <param name="oldValue">old tag</param>
		/// <param name="newValue">new tag</param>
		public void ReplaceTag(string oldValue, string newValue)
		{
			HtmlText = HtmlText.Replace(oldValue, newValue);
		}

		/// <summary>
		/// Returns the plain text representation of the HTML text.
		/// <param name="converter">The converter to use. If converter is null, RegExHtmlConverter will be used.</param>
		/// </summary>
		public StringBuilder GetPlainText(IHtmlConverter converter)
		{
			return converter == null
			       	? new StringBuilder(((IHtmlConverter) new RegExHtmlConverter()).ToPlainText(HtmlText.ToString()))
			       	: new StringBuilder(converter.ToPlainText(HtmlText.ToString()));
		}
	}
}