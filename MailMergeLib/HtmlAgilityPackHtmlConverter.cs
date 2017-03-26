using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace MailMergeLib
{
    /// <summary>
    /// Convert HTML to plain text using the AngleSharp library.
    /// Parsing HTML with AngleSharp es extremely fast, and in addition it occurs only one
    /// per mail merge job and email texts are usually small.
    /// </summary>
    /// <remarks>
    /// Should eventually be improved ;-)
    /// See also http://daringfireball.net/projects/markdown/ - Inspired by Markdown
    /// </remarks>
    public class ParsingHtmlConverter : IHtmlConverter
    {
        private const string CrLf = "\r\n";
        private const string CrLfCrLf = "\r\n\r\n";

        #region IHtmlConverter Members

        /// <summary>
        /// Convert a text file with HTML content to plain text.
        /// </summary>
        /// <param name="html">The HTML string to convert.</param>
        /// <returns>The plain text representation of the HTML content.</returns>
        public string ToPlainText(string html)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);
            return ToPlainText(doc);
        }

        #endregion

        private string ToPlainText(HtmlDocument doc)
        {
            var sw = new StringWriter();
            ConvertToText(doc.DocumentNode, sw);
            sw.Flush();
            return sw.ToString();
        }

        private void ConvertContentToText(HtmlNode node, TextWriter outText)
        {
            foreach (HtmlNode subnode in node.ChildNodes)
            {
                ConvertToText(subnode, outText);
            }
        }

        private void ConvertToText(HtmlNode node, TextWriter outText)
        {
            string html;
            switch (node.NodeType)
            {
                case HtmlNodeType.Comment:
                    // don't output comments
                    break;


                case HtmlNodeType.Document:
                    ConvertContentToText(node, outText);
                    break;


                case HtmlNodeType.Text:
                    string parentName = node.ParentNode.Name;

                    // script, style and title text is ignored
                    if ((parentName == "script") || (parentName == "style") || parentName == "head" || parentName == "title")
                        break;

                    html = ((HtmlTextNode) node).Text;

                    if (parentName != "pre")
                    {
                        // get text with all characters remodes which are not visible in html
                        html = html.Replace("\t", string.Empty).Replace("\r", string.Empty).Replace("\n", string.Empty);
                        var regEx =
                            new Regex(@"\s+", RegexOptions.Compiled);
                        html = regEx.Replace(html, " ");
                    }

                    // gracefully handle overlapping closing elements
                    if (HtmlNode.IsOverlappedClosingElement(html))
                        break;

                    html = HtmlEntity.DeEntitize(html);
                    if (html.Length > 0)
                    {
                        outText.Write(html);
                    }
                    break;


                case HtmlNodeType.Element:
                    var toWrite = new List<string>();

                    switch (node.Name)
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
                            if (node.Attributes["src"] != null && node.Attributes["src"].Value.Trim() != string.Empty)
                                toWrite.Add("[" + HtmlEntity.DeEntitize(node.Attributes["src"].Value + "]"));
                            if (node.Attributes["alt"] != null && node.Attributes["alt"].Value.Trim() != string.Empty)
                                toWrite.Add("[" + HtmlEntity.DeEntitize(node.Attributes["alt"].Value + "]"));
                            if (node.Attributes["title"] != null && node.Attributes["title"].Value.Trim() != string.Empty)
                                toWrite.Add("(\"" + HtmlEntity.DeEntitize(node.Attributes["title"].Value + "\")"));
                            outText.Write("[" + string.Join(" ", toWrite.ToArray()) + "] ");
                            break;
                        case "a":
                            // links
                            toWrite.Clear();
                            if (node.Attributes["href"] != null && node.Attributes["href"].Value.Trim() != string.Empty)
                                toWrite.Add("[" + HtmlEntity.DeEntitize(node.Attributes["href"].Value + "]"));
                            if (node.Attributes["title"] != null && node.Attributes["title"].Value.Trim() != string.Empty)
                                toWrite.Add("(\"" + HtmlEntity.DeEntitize(node.Attributes["title"].Value + "\")"));
                            outText.Write(string.Join(" ", toWrite.ToArray()) + " ");
                            break;
                        case "hr":
                            outText.Write("{0}----------{0}", CrLf);
                            break;
                        case "b":
                        case "strong":
                            node.InnerHtml = "**" + node.InnerHtml + "**";
                            break;
                        case "i":
                        case "u":
                        case "em":
                            node.InnerHtml = "_" + node.InnerHtml + "_";
                            break;
                        case "li":
                            node.InnerHtml = "* " + node.InnerHtml + "<br />";
                            break;
                        case "h1":
                        case "h2":
                        case "h3":
                        case "h4":
                        case "h5":
                        case "h6":
                            // headlines
                            outText.Write(CrLfCrLf);
                            node.InnerHtml = "#######".Substring(0, 7 - int.Parse(node.Name.Substring(1))) + " " + node.InnerHtml + "<br />";
                            break;
                    }

                    if (node.HasChildNodes)
                    {
                        ConvertContentToText(node, outText);
                    }
                    break;
            }
        }
    }
}