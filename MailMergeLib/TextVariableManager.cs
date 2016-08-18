using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace MailMergeLib
{
	/// <summary>
	/// The Variable class used by TextVariableManager.
	/// </summary>
	[Obsolete("Use SmartFormatMail instead.", true)]
	public class Variable
	{
		private const string _defaultFormat = "{0}";

		public Variable()
		{
			ShowNullAs = string.Empty;
			ShowEmptyAs = string.Empty;
			CulturInfo = CultureInfo.CurrentCulture;
		}

		/// <summary>
		/// Gets or sets the name of the variable.
		/// </summary>
		public string Name { get; internal set; }

		/// <summary>
		/// Gets or sets the value of the variable (object).
		/// </summary>
		public object Value { get; internal set; }

		/// <summary>
		/// The format to be used when formatting the value (which is not null or empty).
		/// </summary>
		public string Format { get; internal set; }

		/// <summary>
		/// The value to use in the formatted output when the value is null or System.DBNull.
		/// </summary>
		public string ShowNullAs { get; internal set; }

		/// <summary>
		/// The value to use in the formatted output when the string representation of the value is string.Empty.
		/// </summary>
		public string ShowEmptyAs { get; internal set; }

		internal CultureInfo CulturInfo { get; set; }
		internal IFormatProvider FormatProvider { get; set; }
		internal string MatchingString { get; set; }

		public override string ToString()
		{
			if ((Value != null && Type.GetTypeCode(Value.GetType()) == TypeCode.DBNull) || Value == null)
				return ShowNullAs;

			if ((Value != null && Value.ToString() == string.Empty))
				return ShowEmptyAs;

			return string.Format(CulturInfo, string.IsNullOrEmpty(Format) ? _defaultFormat : Format, Value);
		}
	}

	/// <summary>
	/// Search and replace placeholders in text files with variable values from a data source.
	/// </summary>
	[Obsolete("Use SmartFormatMail instead.", true)]
	public class TextVariableManager : IDisposable, ICloneable
	{
		#region VariableError enum

		/// <summary>
		/// Enumeration of behaviors if the variable for a placeholder is not found int the data source.
		/// </summary>
		public enum VariableError
		{
			ReplaceWithEmptyString,
			ThrowException,
			ShowTextVariable
		}

		#endregion

		// private List<Variable> _textVariables = new List<Variable>();

		// Delimiters and separators of variables in text, like {Age:"{0}"}
		private const string _varLeft = "{";
		private const string _varRight = "}";
		private const string _formatLeft = "{";
		private const string _formatRight = "}";
		private const string _formatSeparator = ":";
		private const string _formatDelimiter = @"""";
		private const string _formatAsFilename = "file";

		// IDisposable
		private bool _disposed;
		private string _fileBaseDir = Environment.CurrentDirectory;

		public TextVariableManager()
		{
			Text = new StringBuilder();
			BadVariables = new List<string>(10);
			BadFiles = new List<string>(10);
			FileVariableErrors = VariableError.ShowTextVariable;
			VariableErrors = VariableError.ShowTextVariable;
			CultureInfo = CultureInfo.CurrentCulture;
			CharacterEncoding = Encoding.Default;
		}

		/// <summary>
		/// Gets or sets the data item which contains the variables to be applied to the Text.
		/// The following types are accepted:
		/// Dictionary&lt;string,object&gt;, ExpandoObject, DataRow, class instances or anonymous types.
		/// The named placeholders can be the name of a Property, Field, or even a parameterless method.
		/// They can also be chained together by using &quot;dot-notation&quot; between properties.
		/// </summary>
	public object DataItem { get; set; }

		/// <summary>
		/// Gets or sets the text to be processed by Process().
		/// </summary>
		public StringBuilder Text { get; set; }

		/// <summary>
		/// Gets or sets the format provider to use for formatting the text variables by values.
		/// </summary>
		public IFormatProvider FormatProvider { get; set; }

		/// <summary>
		/// Gets or sets the Encoding when reading external text files.
		/// </summary>
		public Encoding CharacterEncoding { get; set; }

		/// <summary>
		/// Gets or sets the culture info to apply for any variable formatting (like date, time etc.)
		/// </summary>
		public CultureInfo CultureInfo { get; set; }

		/// <summary>
		/// Get file names included in variables that were not found or could not be read.
		/// </summary>
		public List<string> BadFiles { get; private set; }

		/// <summary>
		/// Get variable names that were found in text, but not in the data source.
		/// </summary>
		public List<string> BadVariables { get; private set; }

		/// <summary>
		/// Gets or sets the local base directory for files, that should be
		/// included into the resulting text (using format name "file", e.g. {filename:"file"})
		/// </summary>
		public string FileBaseDir
		{
			get { return _fileBaseDir; }
			set { _fileBaseDir = value.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar); }
		}

		/// <summary>
		/// Gets or sets the value to use in case a column is TypeCode.DBNull or a value is null.
		/// </summary>
		public string ShowNullAs { get; set; }

		/// <summary>
		/// Gets or sets the value to use in case a column has a string representation of string.Empty.
		/// </summary>
		public string ShowEmptyAs { get; set; }

		/// <summary>
		/// Gets or sets how System.IO exceptions when reading files will be handled.
		/// </summary>
		public VariableError FileVariableErrors { get; set; }

		/// <summary>
		/// Gets or sets illegal variable names will be handled.
		/// </summary>
		public VariableError VariableErrors { get; set; }

		#region ICloneable Members

		object ICloneable.Clone()
		{
			return Clone();
		}

		#endregion

		#region IDisposable Members

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		#endregion

		/// <summary>
		/// Processes the supplied text in a way that all text variables
		/// are replaced by the values of the current data item.
		/// BadFiles and BadVariable lists will be cleared before the process
		/// </summary>
		/// <param name="text">The text with text variables to be processed.</param>
		/// <returns>Returns the text with all text variables replaced by the values of the current data item.</returns>
		public string Process(string text)
		{
			return Process(new StringBuilder(text), true).ToString();
		}

		/// <summary>
		/// Processes the supplied text in a way that all text variables
		/// are replaced by the values of the current data item.
		/// </summary>
		/// <param name="text">The text with text variables to be processed.</param>
		/// <param name="clearBadVarsAndFiles">If true, BadFiles and BadVariable lists will be cleared before the process, else not.</param>
		/// <returns>Returns the text with all text variables replaced by the values of the current data item.</returns>
		public string Process(string text, bool clearBadVarsAndFiles)
		{
			return Process(new StringBuilder(text), clearBadVarsAndFiles).ToString();
		}

		/// <summary>
		/// Processes the text of the Text property in a way that all text variables
		/// are replaced by the values of the current data item.
		/// BadFiles and BadVariable lists will be cleared before the process.
		/// </summary>
		/// <returns>Returns the text with all text variables replaced by the values of the current data item.</returns>
		public StringBuilder Process()
		{
			return Process(Text, true);
		}

		/// <summary>
		/// Processes the supplied text in a way that all text variables
		/// are replaced by the values of the current data item.
		/// BadFiles and BadVariable lists will be cleared before the process.
		/// </summary>
		/// <param name="text">The text with text variables to be processed.</param>
		/// <returns>Returns the text with all text variables replaced by the values of the current data item.</returns>
		public StringBuilder Process(StringBuilder text)
		{
			return Process(text, true);
		}

		/// <summary>
		/// Processes the supplied text in a way that all text variables
		/// are replaced by the values of the current data item.
		/// </summary>
		/// <param name="text">The text with text variables to be processed.</param>
		/// <param name="clearBadVarsAndFiles">If true, BadFiles and BadVariable lists will be cleared before the process, else not.</param>
		/// <returns>Returns the text with all text variables replaced by the values of the current data item.</returns>
		public StringBuilder Process(StringBuilder text, bool clearBadVarsAndFiles)
		{
			if (clearBadVarsAndFiles)
			{
				BadFiles.Clear();
				BadVariables.Clear();
			}

			return ReplaceTextVariablesWithValues(text, SearchTextVariables(text));
		}

		/// <summary>
		/// Processes the text of the Text property in a way that all text variables
		/// are replaced by the values of the current data item. The result is written 
		/// to a file.
		/// BadFiles and BadVariable lists will be cleared before the process
		/// </summary>
		/// <param name="outputFile">FileInfo of the output file. The file name may 
		/// contain text variables.</param>
		/// <param name="append">Determines whether text is to be appended to the file. 
		/// If the file exists and append is false, the file is overwritten. 
		/// If the file exists and append is true, the text is appended to the file. 
		/// Otherwise a new file is created.</param>
		public FileInfo Process(FileInfo outputFile, bool append)
		{
			string filename = Process(MakeFullPath(outputFile.FullName));
			using (var sw = new StreamWriter(Process(filename), append, CharacterEncoding))
			{
				sw.Write(Process().ToString());
				sw.Close();
			}
			return new FileInfo(filename);
		}

		/// <summary>
		/// Reads the content of a text file into the Text property.
		/// </summary>
		/// <param name="filename">The name of the text file to read.</param>
		public void ReadFileToText(string filename)
		{
			using (var sr = new StreamReader(filename, CharacterEncoding, true))
			{
				Text = new StringBuilder(sr.ReadToEnd());
			}
		}

		/// <summary>
		/// Scan the text for text variables, regular formats, null formats and empty formats,
		/// and get their values.
		/// </summary>
		private List<Variable> SearchTextVariables(StringBuilder text)
		{
			var textVariables = new List<Variable>();

			if (DataItem == null || text.Length == 0)
				return textVariables;

			// RegEx:  \{(?<Field>[^:\}]+):?("(?<Format>[^\"]*)")?\}
			// gets patterns like {FieldName} or {FieldName:"SomeFormatStrings"}
			var textVariableRegEx =
				new Regex(
					string.Format(@"\{0}(?<Field>[^{2}\{1}]+){2}?({3}(?<Format>[^\{3}]*){3})?\{1}",
					              _varLeft, _varRight, _formatSeparator, _formatDelimiter),
					RegexOptions.IgnoreCase |
					RegexOptions.CultureInvariant);
			MatchCollection textVariableMatches = textVariableRegEx.Matches(text.ToString());

			foreach (Match match in from Match match in textVariableMatches
			                        let m = match.ToString()
			                        where !textVariables.Exists(v => v.MatchingString == m)
			                        select match)
			{
				textVariables.Add(new Variable());

				Variable variable = textVariables[textVariables.Count - 1];
				variable.CulturInfo = CultureInfo;
				variable.FormatProvider = FormatProvider;
				variable.MatchingString = match.ToString();

				variable.Name = match.Groups["Field"].Success ? match.Groups["Field"].ToString() : string.Empty;
				GetVariableValue(variable);

				// A variable.Name of null means:
				// The variable name does not exist in the data source
				// and VariableErrors is set to VariableError.ShowTextVariable
				if (string.IsNullOrEmpty(variable.Name))
					textVariables.RemoveAt(textVariables.Count - 1);

				// default values for null or empty values
				variable.ShowNullAs = ShowNullAs;
				variable.ShowEmptyAs = ShowEmptyAs;

				// values for null or empty values supplied by columns extended properties
				GetDataTableColumnsExtendedProperties(ref variable);

				// values for format, null and empty values 
				// supplied with the text variable (overrides all formats set before)
				if (match.Groups["Format"].Success)
				{
					GetVariableFormat(variable, match.Groups["Format"].ToString());
				}
			}

			return textVariables;
		}

		/// <summary>
		/// Assigns the Value of the CurrentDataItem for a Variable.
		/// If CurrentDataItem contains hierarchical objects, the values
		/// for the sub-objects can be accessed like that: 
		/// "TopObjectName.SubObject1Name.SubObject11Name".
		/// This will come in handy when using O/R mappers like LLblGenPro or alike.
		/// </summary>
		/// <param name="variable">The Variable for which the Value shall be assigned.</param>
		private void GetVariableValue(Variable variable)
		{
			if (DataItem == null)
			{
				HandleMissingOrUnreadableVariable(variable);
				return;
			}

			// Handle type Dictionary and ExpandoObject (both implement IDictionary<string, object>)
			if (DataItem is IDictionary<string, object>)
			{
				var dict = (IDictionary<string, object>) DataItem;
				object resultData;
				if (!dict.TryGetValue(variable.Name, out resultData))
				{
					HandleMissingOrUnreadableVariable(variable);
					return;
				}
				variable.Value = resultData;
			}
			else if(DataItem is DataRow)
			{
				// Note: The row could as well be converted to a dictionary and then processed like IDictionary<string, object>
				// var dict = row.Table.Columns.Cast<DataColumn>().ToDictionary(c => c.ColumnName, c => row[c]);

				var row = (DataRow) DataItem;
				var column = row.Table.Columns.Cast<DataColumn>().SingleOrDefault(col => col.ColumnName == variable.Name);
				if (column == null)
				{
					HandleMissingOrUnreadableVariable(variable);
					return;
				}
				variable.Value = row[column];
			}
			else
			{
				// Get value by reflection and traversing the property/field/method tree
				string[] hierarchie = variable.Name.Split('.');
				var objectPart = DataItem;

				foreach (var selector in hierarchie)
				{
					if (TryGetValueByReflection(selector, objectPart, out objectPart))
						continue;
					
					HandleMissingOrUnreadableVariable(variable);
					return;
				}
				variable.Value = objectPart;
			}
		}

		private void HandleMissingOrUnreadableVariable(Variable variable)
		{
			// save missing variable name if not already saved
			if (!BadVariables.Contains(variable.Name)) BadVariables.Add(variable.Name);

			switch (VariableErrors)
			{
				case VariableError.ReplaceWithEmptyString:
					variable.Value = string.Empty;
					return;
				case VariableError.ShowTextVariable:
					variable.Name = null;
					return;
				case VariableError.ThrowException:
					throw new ArgumentOutOfRangeException(variable.Name, _varLeft + variable.Name + _varRight, "Variable not found in data source");
			}
		}

		private static bool TryGetValueByReflection(string varName, object dataItem, out object result)
		{
			// credits to Scott Rippey for his SmartFormat.Extensions class "ReflectionSource" (licensed under the MIT License)
			// https://github.com/scottrippey/SmartFormat.NET/wiki

			const BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public;

			var members = dataItem.GetType().GetMember(varName, bindingFlags);

			foreach (var member in members)
			{
				switch (member.MemberType)
				{
					case MemberTypes.Field:
						// Selector is a Field; retrieve the value
						var field = (FieldInfo) member;
						result = field.GetValue(dataItem);
						return true;
					case MemberTypes.Property:
					case MemberTypes.Method:
						MethodInfo method;
						if (member.MemberType == MemberTypes.Property)
						{
							// Selector is a Property
							var prop = (PropertyInfo) member;
							//  Make sure the property is not a pure setter
							if (prop.CanRead)
							{
								method = prop.GetGetMethod();
							}
							else
							{
								continue;
							}
						}
						else
						{
							// Selector is a method
							method = (MethodInfo) member;
						}

						// Check that this method is valid:
						// It needs to return a value (i.e. not a void type) and must be parameterless
						if (method.GetParameters().Any())
						{
							continue;
						}

						// method is not void
						if (method.ReturnType == typeof(void))
						{
							continue;
						}

						// Retrieve the value
						result = method.Invoke(dataItem, null);
						return true;
				}
			}

			// failure:
			result = null;
			return false;
		}


		/// <summary>
		/// Reads the regular format, and (if any) the values to show for
		/// null or empty values.
		/// </summary>
		/// <param name="variable">The variable to use for setting format properties.</param>
		/// <param name="formatString">The string containg format information.</param>
		private static void GetVariableFormat(Variable variable, string formatString)
		{
			// A format of "file" will be interpreted as a text file name
			if (_formatAsFilename == formatString.Trim())
			{
				variable.Format = _formatAsFilename;
				return;
			}
			
			// RegEx:  (?<Format>\{\d:[^\}]*\}|\{\d[^\}]*\})?(?:\{null:(?<ShowNullAs>[^\}]*)\})?(?:\{empty:(?<ShowEmptyAs>[^\}]*)\})?
			// gets patterns like {0:dd MMMM} or {0} or {null:NullValue} or {empty:EmptyValue} or any combination of them
			var textVariableRegEx =
				new Regex(
					string.Format(
						@"(?<Format>\{0}\d:[^\{1}]*\{1}|\{0}\d[^\{1}]*\{1})?(?:\{0}null:(?<ShowNullAs>[^\{1}]*)\{1})?(?:\{0}empty:(?<ShowEmptyAs>[^\{1}]*)\{1})?",
						_formatLeft, _formatRight),
					RegexOptions.IgnoreCase |
					RegexOptions.CultureInvariant);
			MatchCollection formatMatches = textVariableRegEx.Matches(formatString);

			foreach (Match match in formatMatches)
			{
				if (match.Groups["Format"].Success)
				{
					variable.Format = match.Groups["Format"].ToString();
				}
				if (match.Groups["ShowNullAs"].Success)
				{
					variable.ShowNullAs = match.Groups["ShowNullAs"].ToString();
				}
				if (match.Groups["ShowEmptyAs"].Success)
				{
					variable.ShowEmptyAs = match.Groups["ShowEmptyAs"].ToString();
				}
			}
		}

		/// <summary>
		/// Reads extended properties for all columns of a data table which 
		/// can be used for variable formatting.
		/// </summary>
		/// <param name="variable">The variable to use for setting format properties.</param>
		private void GetDataTableColumnsExtendedProperties(ref Variable variable)
		{
			// Use the format property provided programmatically
			DataTable dt = null;
			if (DataItem is DataRow)
				dt = ((DataRow) DataItem).Table;

			if (dt == null || string.IsNullOrEmpty(variable.Name)) return;

			if (!dt.Columns.Contains(variable.Name))
				return;

			if (dt.Columns[variable.Name].ExtendedProperties.ContainsKey("null"))
				variable.ShowNullAs = dt.Columns[variable.Name].ExtendedProperties["null"] as string ??
				                      ShowNullAs;

			if (dt.Columns[variable.Name].ExtendedProperties.ContainsKey("empty"))
				variable.ShowNullAs = dt.Columns[variable.Name].ExtendedProperties["empty"] as string ??
				                      ShowEmptyAs;

			if (dt.Columns[variable.Name].ExtendedProperties.ContainsKey("format"))
				variable.Format = dt.Columns[variable.Name].ExtendedProperties["format"] as string ??
				                  "{0}";
		}

		/// <summary>
		/// Replaces the text variables by their formatted values.
		/// The original text in the argument remains unchanged.
		/// </summary>
		private StringBuilder ReplaceTextVariablesWithValues(StringBuilder text, List<Variable> textVariables)
		{
			// work on a copy of the orginal text
			var textResult = new StringBuilder(text.ToString());

			if (textResult.Length == 0)
				return textResult;

			// first read in all external files - this is done only once - no recursion.
			bool textFileWasIncluded = false;
			foreach (Variable variable in textVariables.Where(variable => !string.IsNullOrEmpty(variable.Format) && _formatAsFilename == variable.Format.ToLower()))
			{
				// If a format name called "file" is found, the variable's value is interpreted as a file name
				// The file name may contain variables as well.
				using (TextVariableManager tvm = Clone())
				{
					string filename = tvm.Process(variable.Value as string ?? string.Empty);
					// add all new bad variable names to the list
					foreach (string badVar in tvm.BadVariables.Where(badVar => !BadVariables.Contains(badVar)))
					{
						BadVariables.Add(badVar);
					}
					filename = MakeFullPath(filename);
					string content = ReadFile(filename);

					if (content == null)
					{
						if (FileVariableErrors == VariableError.ReplaceWithEmptyString)
							textResult = textResult.Replace(variable.MatchingString, string.Empty);
					}
					else
					{
						textResult = textResult.Replace(variable.MatchingString, content);
					}

					textFileWasIncluded = true;
				}
			}

			// if a text file was included, re-search and replace the text for variables
			if (textFileWasIncluded)
			{
				foreach (Variable variable in SearchTextVariables(textResult).Where(variable => variable.Format != _formatAsFilename || FileVariableErrors != VariableError.ShowTextVariable))
				{
					textResult = textResult.Replace(variable.MatchingString, variable.ToString());
				}
			}

			// Next replace variables for the whole text (including any text files read before).
			// If a text file was included, it could again contain variables - but do NOT process
			// external text files recursively.

			return textVariables.Aggregate(textResult, (current, variable) => current.Replace(variable.MatchingString, variable.ToString()));
		}


		/// <summary>
		/// Reads the contents of a text file.
		/// </summary>
		/// <param name="filename">The name of the file to read.</param>
		/// <returns>Reads the contents of a text file as a string.</returns>
		private string ReadFile(string filename)
		{
			try
			{
				using (var sr = new StreamReader(filename, CharacterEncoding, true))
				{
					return sr.ReadToEnd();
				}
			}
			catch (FileNotFoundException)
			{
				if (!BadFiles.Contains(filename)) BadFiles.Add(filename);
				if (FileVariableErrors == VariableError.ThrowException) throw;
			}
			catch (IOException)
			{
				if (!BadFiles.Contains(filename)) BadFiles.Add(filename);
				if (FileVariableErrors == VariableError.ThrowException) throw;
			}
			catch (UnauthorizedAccessException)
			{
				if (!BadFiles.Contains(filename)) BadFiles.Add(filename);
				if (FileVariableErrors == VariableError.ThrowException) throw;
			}

			// returning null indicates an IO error
			return null;
		}


		/// <summary>
		/// The list of text variables which were found and used while processing the text.
		/// </summary>
		public List<Variable> GetTextVariables(StringBuilder text)
		{
			return SearchTextVariables(text);
		}

		/// <summary>
		/// Combines the specified filename with the basename of 
		/// to form a full path to file or directory.
		/// </summary>
		/// <param name="filename">The filename, which may include a relative path.</param>
		/// <returns>
		/// A rooted path.
		/// </returns>
		private string MakeFullPath(string filename)
		{
			// adjust separators, so the same as with _fileBaseDir are used
			filename = filename.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);

			if (!string.IsNullOrEmpty(filename))
			{
				if (!Path.IsPathRooted(filename))
				{
					filename = Path.GetFullPath(Path.Combine(_fileBaseDir, filename));
				}
			}
			return filename;
		}

		/// <summary>
		/// Destructor.
		/// </summary>
		~TextVariableManager()
		{
			Dispose(false);
		}

		private void Dispose(bool disposing)
		{
			if (! _disposed)
			{
				if (disposing)
				{
					// Dispose managed resources.
					BadFiles.Clear();
					BadVariables.Clear();
				}
			}
			_disposed = true;
		}

		/// <summary>
		/// Clones all members of the current instance except for Text property.
		/// </summary>
		/// <returns>Returns a copy of the current instance except for Text property.</returns>
		public TextVariableManager Clone()
		{
			return new TextVariableManager
			       	{
			       		CultureInfo = CultureInfo,
			       		DataItem = DataItem,
			       		ShowEmptyAs = ShowEmptyAs,
			       		ShowNullAs = ShowNullAs,
			       		FileVariableErrors = FileVariableErrors,
			       		VariableErrors = VariableErrors,
			       		FormatProvider = FormatProvider,
			       		FileBaseDir = FileBaseDir
			       	};
		}
	}
}