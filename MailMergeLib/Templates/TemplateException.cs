using System;

namespace MailMergeLib.Templates
{
    /// <summary>
    /// Represents errors that occur during changing the <see cref="Templates"/> object graph programmatically, including during deserialization.
    /// </summary>
    public class TemplateException : Exception
    {
        public TemplateException(string message, Part part, Parts parts, Template template, Templates templates) : base (message)
        {
            Part = part;
            Parts = parts;
            Template = template;
            Templates = templates;
        }

        /// <summary>
        /// Gets the <see cref="Part"/> causing the exception, if not null.
        /// </summary>
        public Part Part { get; private set; }

        /// <summary>
        /// Gets the <see cref="Parts"/> causing the exception, if not null.
        /// </summary>
        public Parts Parts { get; private set; }

        /// <summary>
        /// Gets the <see cref="Template"/> causing the exception, if not null.
        /// </summary>
        public Template Template { get; private set; }

        /// <summary>
        /// Gets the <see cref="Templates"/> causing the exception, if not null.
        /// </summary>
        public Templates Templates { get; private set; }
    }
}