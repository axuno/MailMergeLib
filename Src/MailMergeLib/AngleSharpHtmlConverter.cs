using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using AngleSharp.Dom;

namespace MailMergeLib;

/// <summary>
/// Convert HTML to plain text using the AngleSharp library.
/// Parsing HTML with AngleSharp es extremely fast, and in addition it occurs only one
/// per mail merge job and email texts are usually small.
/// </summary>
/// <remarks>
/// If someone prefers HtmlAgilityPack as a parser instead of AngleSharp, you could reference HtmlAgilityPack
/// and include HtmlAgilityPackHtmlConverter.cs in the project.
/// Should eventually be improved ;-)
/// See also http://daringfireball.net/projects/markdown/ - Inspired by Markdown
/// </remarks>
public class AngleSharpHtmlConverter : IHtmlConverter
{
    private const string CrLf = "\r\n";
    private const string CrLfCrLf = CrLf + CrLf;

    #region IHtmlConverter Members

    /// <summary>
    /// Convert a text file with HTML content to plain text.
    /// </summary>
    /// <param name="html">The HTML string to convert.</param>
    /// <returns>The plain text representation of the HTML content.</returns>
    public string ToPlainText(string html)
    {
        var parser = new AngleSharp.Html.Parser.HtmlParser();
        var document = parser.ParseDocument(html);

        using var sw = new StringWriter();
        ConvertContentToText(document.ChildNodes, sw);
        sw.Flush();
        var text = sw.ToString();

        // strip leading white space and more than 2 consecutive line breaks
        return text.Trim();
    }

    #endregion

    private void ConvertContentToText(INodeList node, TextWriter outText)
    {
        foreach (var subnode in node)
        {
            ConvertToText(subnode, outText);
        }
    }

    private void ConvertToText(INode node, TextWriter outText)
    {
        string html;

        switch (node.NodeType)
        {
            case NodeType.Comment: // don't output comments
            case NodeType.Document: // child node cannot be a document
                break;

            case NodeType.Text:
                var parentName = node.ParentElement.NodeName.ToLower();

                // script, style and title text is ignored
                if ((parentName == "script") || (parentName == "style") || parentName == "head" || parentName == "title" || parentName == "meta")
                    break;

                html = node.TextContent;

                if (parentName != "pre")
                {
                    // get text with all characters remodes which are not visible in html
                    html = html.Replace("\t", string.Empty).Replace("\r", string.Empty).Replace("\n", string.Empty);
                    var regEx =
                        new Regex(@"\s+", RegexOptions.Compiled);
                    html = regEx.Replace(html, " ");
                }

                if (html.Length > 0)
                {
                    outText.Write(html);
                }
                break;


            case NodeType.Element:
                var toWrite = new List<string>();
                var element = (IElement) node;

                switch (node.NodeName.ToLower())
                {
                    case "p":
                        outText.Write(CrLfCrLf);
                        break;
                    case "br":
                    case "td":
                    case "ul":
                    case "ol":
                        outText.Write(CrLf);
                        break;
                    case "img":
                        // images
                        toWrite.Clear();
                        if (element.Attributes["src"] != null && element.Attributes["src"].Value.Trim() != string.Empty)
                            toWrite.Add("[" + element.Attributes["src"].Value + "]");
                        if (element.Attributes["alt"] != null && element.Attributes["alt"].Value.Trim() != string.Empty)
                            toWrite.Add("[" + element.Attributes["alt"].Value + "]");
                        if (element.Attributes["title"] != null && element.Attributes["title"].Value.Trim() != string.Empty)
                            toWrite.Add("(\"" + element.Attributes["title"].Value + "\")");
                        outText.Write("[" + string.Join(" ", toWrite.ToArray()) + "] ");
                        break;
                    case "a":
                        // links
                        toWrite.Clear();
                        if (element.Attributes["href"] != null && element.Attributes["href"].Value.Trim() != string.Empty)
                            toWrite.Add("[" + element.Attributes["href"].Value + "]");
                        if (element.Attributes["title"] != null && element.Attributes["title"].Value.Trim() != string.Empty)
                            toWrite.Add("(\"" + element.Attributes["title"].Value + "\")");
                        outText.Write(string.Join(" ", toWrite.ToArray()) + " ");
                        break;
                    case "hr":
                        outText.Write("{0}----------{0}", CrLf);
                        break;
                    case "b":
                    case "strong":
                        element.InnerHtml = "**" + element.InnerHtml + "**";
                        break;
                    case "i":
                    case "u":
                    case "em":
                        element.InnerHtml = "_" + element.InnerHtml + "_";
                        break;
                    case "li":
                        element.InnerHtml = "* " + element.InnerHtml + "<br />";
                        break;
                    case "h1":
                    case "h2":
                    case "h3":
                    case "h4":
                    case "h5":
                    case "h6":
                        // headlines
                        outText.Write(CrLfCrLf);
                        element.InnerHtml = "#######".Substring(0, 7 - int.Parse(element.TagName.Substring(1))) + " " + element.InnerHtml + "<br />";
                        break;
                }

                if (node.HasChildNodes)
                {
                    ConvertContentToText(node.ChildNodes, outText);
                }
                break;
        }
    }
}