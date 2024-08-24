using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using RegionKit.Modules.Atmo.Body;
using RegionKit.Modules.Atmo.Data;

namespace RegionKit.Modules.Atmo.Gen
{
	internal static class NewParser
	{
		#region trigger parsing
		public static HappenTrigger? ParseTriggerLine(Happen happen, string line)
		{ 
			var tokens = GetTokens(line, NextTriggerToken);
			var ts = tokens.GetEnumerator();
			//LogMessage(string.Join(", ", tokens.Select(x => x.Type)));
			//LogMessage(string.Join(", ", tokens.Select(x => x.Value)));
			ts.MoveNext();
			return ParseTriggers(happen, ts);
		}

		static HappenTrigger? ParseTriggers(Happen happen, IEnumerator<Token> ts)
		{
			HappenTrigger? trigger = null;

			if (ts.Current.Type == TokenType.BeginBracket)
			{
				ts.MoveNext();
				trigger = ParseTriggers(happen, ts);
			}

			else if (ts.Current.Type == TokenType.Trigger)
			{
				trigger = ParseTrigger(happen, ts);
			}

			else if(ts.Current.Type != TokenType.Operator)
			{
				LogMessage("unexpected token! expected Trigger, Operator, or BeginBracket. Instead got " + ts.Current.Value);
			}

			if (ts.Current.Type == TokenType.Operator)
			{
				return ParseTriggerOperator(happen, trigger, ts);
			}

			else if (ts.Current.Type == TokenType.EndBracket)
			{
				ts.MoveNext();
			}

			return trigger;
		}
		static HappenTrigger? ParseTrigger(Happen happen, IEnumerator<Token> ts)
		{
			string triggerName = ts.Current.Value;
			API.Backing.Create_NamedTriggerFactory builder = API.Backing.__namedTriggers[ts.Current.Value];
			ts.MoveNext();
			List<string> rawArgs = new();
			while (ts.Current.Type == TokenType.Arg)
			{
				rawArgs.Add(ts.Current.Value);
				ts.MoveNext();
			}
			//LogMessage($"found trigger [{triggerName}] with args [{string.Join(", ", rawArgs)}]");
			return builder(new ArgSet(rawArgs.ToArray(), happen.Set.world), happen);
		}

		static HappenTrigger ParseTriggerOperator(Happen happen, HappenTrigger? previousTrigger, IEnumerator<Token> ts)
		{
			var literal = ts.Current.Value;
			ts.MoveNext();
			HappenTrigger? nextTrigger = ParseTriggers(happen, ts);

			return OperatorTrigger.args[literal](previousTrigger!, nextTrigger!);
		}

		static Token NextTriggerToken(string text, int i)
		{
			var t = new Token();
			t.Start = i;
			t.Length = 1;
			char c = text[i];

			switch (c)
			{
			case '(': t.Type = TokenType.BeginBracket; break;
			case ')': t.Type = TokenType.EndBracket; break;
			default:

				if (IsSpace(c))
				{
					t.Type = TokenType.Space;
					while (i < text.Length && IsSpace(text[i]))
						i++;
					t.Length = i - t.Start;
					break;
				}

				if (ListContainsNextSubstring(OperatorTrigger.args.Keys, text, i, out string op))
				{
					t.Type = TokenType.Operator;
					t.Length = op.Length;
					break;
				}

				if (ListContainsNextSubstring(API.Backing.__namedTriggers.Keys, text, i, out string trigger))
				{
					t.Type = TokenType.Trigger;
					t.Length = trigger.Length;
					break;
				}

				return ParseArgToken(text, i);
			}

			t.Value = text.Substring(t.Start, t.Length);
			return t;
		}

		public static List<HappenAction> ParseActions(Happen happen, string line)
		{
			List<HappenAction> result = new();
			var tokens = GetTokens(line, NextActionToken);
			//LogMessage(string.Join(", ", tokens.Select(x => x.Type)));
			//LogMessage(string.Join(", ", tokens.Select(x => x.Value)));
			var ts = tokens.GetEnumerator();
			ts.MoveNext();
			while (ts.Current.Type != TokenType.EndOfLine)
			{
				if (ts.Current.Type == TokenType.Action)
				{
					var action = ParseAction(happen, ts);
					if (action != null) {
						result.Add(action);
				}
				}
				else
				{
					ts.MoveNext();
				}
			}
			return result;
		}


		static HappenAction? ParseAction(Happen happen, IEnumerator<Token> ts)
		{
			string actionName = ts.Current.Value;
			API.Backing.Create_NamedHappenBuilder builder = API.Backing.__namedActions[ts.Current.Value];
			ts.MoveNext();
			List<string> rawArgs = new();
			while (ts.Current.Type == TokenType.Arg)
			{
				rawArgs.Add(ts.Current.Value);
				ts.MoveNext();
			}
			//LogMessage($"found action [{actionName}] with args [{string.Join(", ", rawArgs)}]");
			return builder(happen, new ArgSet(rawArgs.ToArray(), happen.Set.world));
		}


		static Token NextActionToken(string text, int i)
		{
			var t = new Token();
			t.Start = i;
			t.Length = 1;
			char c = text[i];

			switch (c)
			{
			case ',': t.Type = TokenType.ValueSeparator; break;
			default:

				if (IsSpace(c))
				{
					t.Type = TokenType.Space;
					while (i < text.Length && IsSpace(text[i]))
						i++;
					t.Length = i - t.Start;
					break;
				}

				if (ListContainsNextSubstring(API.Backing.__namedActions.Keys, text, i, out string trigger))
				{
					t.Type = TokenType.Action;
					t.Length = trigger.Length;
					break;
				}

				return ParseArgToken(text, i);
			}

			t.Value = text.Substring(t.Start, t.Length);
			return t;
		}


		#endregion

		static IEnumerable<Token> GetTokens(string text, Func<string, int, Token> NextToken)
		{
			int i = 0;
			while (i < text.Length)
			{
				var token = NextToken(text, i);
				i += token.Length;

				if (token.Type != TokenType.Space)
					yield return token;
			}

			yield return new Token()
			{
				Start = text.Length,
				Length = 0,
				Type = TokenType.EndOfLine
			};
		}

		static bool IsSpace(char c)
		{
			return c == ' ' || c == '\t' || c == '\r' || c == '\n';
		}
		public static ArgSet ParseFMT(string text, World world)
		{ 
			//wip
			ArgSet set = new ArgSet();
			bool arg = false;
			int lastSplit = 0;
			for (int i = 0; i < text.Length; i++)
			{
				if (arg)
				{
					Token token = ParseArgToken(text, i);
					i += token.Length;
					if (token.Value.StartsWith("{")) token.Value = token.Value[1..];
					if (token.Value.EndsWith("{")) token.Value = token.Value[..^1];
					arg = false;
					set.Add(VarRegistry.ParseArg(token.Value, out _, world));
					lastSplit = i + 1;
				}

				if (text[i] == '{') 
				{
					arg = true; 
					set.Add(new StaticArg(text.Substring(lastSplit, i - lastSplit))); 
					lastSplit = i;
				}

			}
			LogMessage(string.Join(", ", set.Select(x => x.Raw)));
			return set;
		}

		static Token ParseArgToken(string text, int i)
		{
			Token t = new Token();
			t.Start = i;
			t.Type = TokenType.Arg;
			int FMTCount = 0;
			bool FMT = text[i] == '{';
			if (text[i] == '\'' || FMT)
			{
				i++;
				while (i < text.Length && (text[i] != '\'' || FMTCount != 0))
				{
					if (text[i] == '{') FMTCount++;
					if (text[i] == '}') FMTCount--;
					if (FMT && FMTCount == -1) break;
					if (text[i] == '\\') i += 2;
					else i += 1;
				}
				i++;
			}
			else
			{
				while (i < text.Length && !IsSpace(text[i]) && text[i] != '}' && text[i] != ')') i++;
			}

			if (i > text.Length) throw new AtmoParseException(text, text.Length, "Unterminated string!");

			t.Length = i - t.Start;
			t.Value = ParseRawArg(text.Substring(t.Start, t.Length));
			return t;
		}

		static string ParseRawArg(string text)
		{
			if (!text.StartsWith("'") || !text.EndsWith("'")) return text;
			var sb = new StringBuilder();

			//LogMessage($"trimming quotes from string [{text}]");

			int start = 1;
			int current = start;
			int end = text.Length - 1;

			while (current < end)
			{
				// Evaluate escape sequences
				if (text[current] == '\\')
				{
					sb.Append(text, start, current - start);
					LogMessage(sb.ToString());
					current++;
					if (current >= end) throw new AtmoParseException(text, current, "Unexpected end of string escape sequence!");

					switch (text[current])
					{
					case '\"': sb.Append('"'); break;
					case '\\': sb.Append('\\'); break;
					case 'b': sb.Append('\b'); break;
					case 'f': sb.Append('\f'); break;
					case 'n': sb.Append('\n'); break;
					case 'r': sb.Append('\r'); break;
					case 't': sb.Append('\t'); break;
					case 'u':

						if (current + 4 >= end) throw new AtmoParseException(text, end, "Unexpected end of string in unicode escape sequence!");

						for (int i = 0; i < 4; i++)
						{
							var c = text[current + 1 + i];
							if (!(c >= '0' && c <= '9')
								&& !(c >= 'a' && c <= 'f')
								&& !(c >= 'A' && c <= 'F'))
							{
								throw new AtmoParseException(text, current + 1 + i, $"Unexpected character in unicode escape sequence: {c}");
							}
						}
						int codePoint = int.Parse(text.Substring(current + 1, 4), System.Globalization.NumberStyles.HexNumber);
						sb.Append((char)codePoint);
						current += 4;

						break;
					}
					start = current + 1;
				}
				else if (text[current] <= 0x001F)
				{
					bool lineBreak = text[current] == '\r' || text[current] == '\n';
					throw new AtmoParseException(text, current, $"Unexpected {(lineBreak ? "line break" : "control character")} in string!");
				}

				current++;
			}

			sb.Append(text, start, end - start);
			LogMessage(sb.ToString());

			return sb.ToString();
		}


		static bool ListContainsNextSubstring(IEnumerable<string> list, string text, int start, out string value)
		{
			value = "";
			foreach (string match in list)
			{
				if (text.Length > start + match.Length && text.Substring(start, match.Length).Equals( match, StringComparison.Ordinal))
				{
					value = match;
					return true;
				}
			}
			return false;
		}

		struct Token
		{
			public int Start;
			public int Length;
			public TokenType Type;

			public string Value;
		}

		enum TokenType
		{
			Space,
			ValueSeparator,
			BeginBracket,
			EndBracket,
			Trigger,
			Operator,
			Action,
			Arg,
			EndOfLine,
		}

		enum FMTType
		{
		Literal,
		OpenBracket,
		EndBreacket,
		}
	}

	/// <summary>
	/// Represents errors that occur when parsing JSON data.
	/// </summary>
	public class AtmoParseException : Exception
	{
		/// <summary>
		/// The offset in the input string that the error occurred at.
		/// </summary>
		public int CharIndex { get; private set; }

		/// <summary>
		/// The line in the input string that the error occurred at.
		/// </summary>
		public int Line { get; private set; }

		internal AtmoParseException(string text, int position) => FindLine(text, position);
		internal AtmoParseException(string text, int position, string message) : base(message) => FindLine(text, position);
		internal AtmoParseException(string text, int position, string message, Exception inner) : base(message, inner) => FindLine(text, position);

		void FindLine(string text, int position)
		{
			int line = 0;
			for (int i = 0; i < text.Length && i < position; i++)
			{
				if (text[i] == '\r' || text[i] == '\n')
				{
					line++;
					if (text[i] == '\r' && i + 1 < text.Length && text[i + 1] == '\n')
						i++;
				}
			}

			CharIndex = position;
			Line = line;
		}
	}
}
