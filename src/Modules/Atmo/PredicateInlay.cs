using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Text.RegularExpressions;
/// <summary>
/// Parses a string by logical expression operators (symbolic as well as words and/or/xor/not) into a treelike structure, then, using a supplied <see cref="del_FetchPred"/>, exchanges resulting substrings between logical operators into delegates, then evaluates expression on demand.
/// </summary>
public sealed partial class PredicateInlay
{
	//compiling to a dynamic method?
	//no fuck that
	#region fields
	/// <summary>
	/// Contains the Inlay's logic tree
	/// </summary>
	private readonly Tree TheTree;
	//private Guid myID = Guid.NewGuid();
	//w
	//private DynamicMethod? compiledEvalDynM;
	//private del_Pred? compiledEval;

	private Action<object>? logger;
	private bool? mendOnThrow = true;
	#endregion fields
	/// <summary>
	/// Parses an instance from a given expression.
	/// </summary>
	/// <param name="expression">Source string.</param>
	/// <param name="exchanger">Delegate to exchange leaves into callbacks. May be null.</param>
	/// <param name="logger">Optional callback that receives information from the instance (like a message when something breaks)</param>
	/// <param name="mendOnThrow">Whether to auto-void leaf callbacks on exceptions. Null to do nothing, non-null to set errored triggers to blanks of specific state (true/false)</param>
	public PredicateInlay(string expression, del_FetchPred? exchanger, Action<object>? logger = null, bool? mendOnThrow = null)
	{
		this.logger = logger;
		this.mendOnThrow = mendOnThrow;
		//tokenize
		Token[] tokens = Tokenize(expression).ToArray();
		//prepare the soil
		int index = 0;
		IExpr? x = Parse(tokens, ref index);
		//plant the tree
		TheTree = new(x);
		//water it
		Populate(exchanger);
	}
	#region user methods
	/// <summary>
	/// Populates leaves using given fetcher.
	/// </summary>
	/// <param name="newExchanger"></param>
	public void Populate(del_FetchPred? newExchanger)
	{
		if (newExchanger is null) return;
		TheTree.Populate(newExchanger);
		//compiledEvalDynM = null;
		//compiledEval = null;
	}
	/// <summary>
	/// Throws <see cref="NotImplementedException"/>.
	/// </summary>
	/// <exception cref="NotImplementedException"></exception>
	public void Compile()
	{
		throw new NotImplementedException("Compiling to a dynamic method is not available");
	}
	/// <summary>
	/// Walks the instance to obtain the result.
	/// </summary>
	/// <returns></returns>
	public bool Eval()
	{
		return TheTree.Eval(logger, mendOnThrow);
	}

	#endregion
	#region internals
	/// <summary>
	/// Tokenizes a string.
	/// </summary>
	/// <param name="expression"></param>
	/// <returns>An array of tokens.</returns>
	public static List<Token> Tokenize(string expression)
	{
		List<Token> tokens = new();
		string? remaining = expression.Clone() as string;
		List<KeyValuePair<TokenType, Match>> results = new();
		while ((remaining?.Length ?? 0) > 0)
		{
			results.Clear();
			//closest match is considered the correct one.
			int closest = int.MaxValue;
			//token recognition precedence set by enum order.
			foreach (TokenType val in Enum.GetValues(typeof(TokenType)))
			{
				Regex? matcher = exes[val];
				Match? match = matcher.Match(remaining);
				if (match.Success)
				{
					//something found.
					if (match.Index < closest) closest = match.Index;
					results.Add(new(val, match));
				}
			}
			//scroll through all acquired results, take the closest one with higher precedence.
			KeyValuePair<TokenType, Match>? selectedKvp = null;
			for (int i = 0; i < results.Count; i++)
			{
				KeyValuePair<TokenType, Match> kvp = results[i];
				if (kvp.Value.Index == closest) { selectedKvp = kvp; break; }
			}
			//no recognizable patterns left, abort.
			if (selectedKvp == null)
				break;
			//cut the remaining string, add gathered token if not a separator.
			TokenType tokType = selectedKvp.Value.Key;
			Match? selectedMatch = selectedKvp.Value.Value; //fuck these things get ugly
			remaining = remaining?.Substring(selectedMatch.Index + selectedMatch.Length);
			if (selectedKvp.Value.Key != TokenType.Separator)
			{
				tokens.Add(new Token(tokType, selectedMatch.Value));
			}
		}
		return tokens;
	}
	/// <summary>
	/// Recursive descent parsing.
	/// </summary>
	/// <param name="tokens">An array of tokens to work over.</param>
	/// <param name="index">A reference to current index. Obviously, top layer should start at zero.</param>
	/// <returns>The resulting <see cref="IExpr"/>.</returns>
	/// <exception cref="InvalidOperationException">Failed to strip a group.</exception>
	private IExpr Parse(Token[] tokens, ref int index)
	{
		if (tokens.Length == 0) return new Stub();
		List<string> litStack = new();
		int prevWordIndex = index; //index of a last word, used for finalizing words
		string? cWord = null; //current word's name
		List<IExpr> branches = new();
		for (; index < tokens.Length; index++)
		{
			//see what current token is
			ref Token cTok = ref tokens[index];
			if (cTok.type is not TokenType.Literal && cWord is not null)
			{
				FinalizeWord(); //a word's arguments have ended.
			}

			switch (cTok.type)
			{
			//if it's a delim, recurse into an embedded group
			case TokenType.DelimOpen:
				index += 1;
				branches.Add(Parse(tokens, ref index)); //descend
				break;
			case TokenType.DelimClose:
				if (cWord is not null) FinalizeWord();
				goto finish; // round up
							 //if it's an operator, push an operator
			case TokenType.Operator:
				branches.Add(new Oper(GetOp(in cTok) ?? Op.OR));
				break;
			//begin recording a new word
			case TokenType.Word:
				prevWordIndex = index;
				cWord = cTok.val;
				break;
			default:
				break;
			}
		}
	finish:
		if (cWord is not null) FinalizeWord(); //just to be sure
											   //operators start consuming
		foreach (Op tp in new[] { Op.NOT, Op.AND, Op.XOR, Op.OR })
		{
			//looping right to left.
			for (int i = branches.Count - 1; i >= 0; i--)
			{
				IExpr cBranch = branches[i];
				if (cBranch is Oper o && o.TP == tp && o.L is null && o.R is null)
				{
					if (i < 0 || i >= branches.Count) continue;
					if (o.TP is not Op.NOT)
					{
						//remove both
						o.R = branches[i + 1];
						o.L = branches[i - 1];
						branches[i] = o;
						branches.RemoveAt(i + 1);
						branches.RemoveAt(i - 1);
						i--;
					}
					else
					{
						//only on the right
						o.R = branches[i + 1];
						branches[i] = o;
						branches.RemoveAt(i + 1);
					}
				}
			}
		}

		//for (int i = branches.Count - 1; i >= 0; i--)
		//{
		//    IExpr cBranch = branches[i];
		//    if (cBranch is Oper o && o.L == null) { branches[i] = o.R; }
		//}
		return branches.Count switch
		{
			0 => new Stub(), // empty group
			1 => branches[0], // normal
			_ => throw new InvalidOperationException("Can't abstract away group!"), //failed to strip
		};
		void FinalizeWord()
		{
			Leaf? leaf = MakeLeaf(tokens, in prevWordIndex);
			if (leaf is not null) branches.Add(leaf);
			cWord = null;
			litStack.Clear();
		}
	}
	#endregion
	#region nested
	/// <summary>
	/// Wraps compiled expression structure
	/// </summary>
	public class Tree
	{
		/// <summary>
		/// Answers if instance has been populated with callbacks.
		/// </summary>
		public bool Populated { get; private set; }
		/// <summary>
		/// root node
		/// </summary>
		public readonly IExpr root;
		/// <summary>
		/// Creates an instance with given node as root.
		/// </summary>
		/// <param name="root"></param>
		public Tree(IExpr root)
		{
			this.root = root;
		}
		/// <summary>
		/// Fills the expression using given <see cref="del_FetchPred"/>
		/// </summary>
		/// <param name="exchanger"></param>
		public void Populate(del_FetchPred exchanger)
		{
			Populated = true;
			root.Populate(exchanger);
		}
		/// <summary>
		/// Runs evaluation on a tree
		/// </summary>
		/// <returns></returns>
		public bool Eval(Action<object>? logger, bool? mend)
		{
			return root.Eval(logger, mend);
		}
	}
	/// <summary>
	/// Base interface for expressions
	/// </summary>
	public interface IExpr
	{
		/// <summary>
		/// Evaluates a node and checks if it's true or false. Ran repeatedly.
		/// </summary>
		/// <returns></returns>
		public bool Eval(Action<object>? logger, bool? mend);
		/// <summary>
		/// Populates a node (and children nodes if any) using a given <see cref="del_FetchPred"/>. Ran once.
		/// </summary>
		/// <param name="exchanger"></param>
		public void Populate(del_FetchPred? exchanger);
	}
	/// <summary>
	/// Empty node, always returns true
	/// </summary>
	[System.Diagnostics.DebuggerDisplay("{ToString()}")]
	public struct Stub : IExpr
	{
		/// <inheritdoc/>
		public bool Eval(Action<object>? logger, bool? mend)
		{
			return true;
		}

		/// <inheritdoc/>
		public void Populate(del_FetchPred? exchanger) { }
		/// <inheritdoc/>
		public override string ToString()
		{
			return "{}";
		}
	}
	/// <summary>
	/// An end node; carries parameters passed when parsing and a final callback reference. If the callback is null, always true.
	/// </summary>
	[System.Diagnostics.DebuggerDisplay("{ToString()}")]
	public struct Leaf : IExpr
	{
		/// <summary>
		/// Name of the node.
		/// </summary>
		public readonly string funcName;
		/// <summary>
		/// Arguments given to the node.
		/// </summary>
		public readonly string[] args;
		/// <summary>
		/// Callback the node has been populated with.
		/// </summary>
		public del_Pred? myCallback { get; private set; }
		/// <summary>
		/// Creates an unpopulated instance with given name and arguments.
		/// </summary>
		/// <param name="funcName"></param>
		/// <param name="args"></param>
		public Leaf(string funcName, string[] args)
		{
			this.funcName = funcName;
			this.args = args;
			myCallback = null;
		}
		/// <inheritdoc/>
		public void Populate(del_FetchPred? exchanger)
		{
			myCallback = exchanger?.Invoke(funcName, args);
		}

		/// <inheritdoc/>
		public override string ToString()
		{
			return funcName + "(" + (args.Length == 0 ? string.Empty : args.Aggregate((x, y) => $"{x}, {y}")) + ")";
		}
		/// <inheritdoc/>
		public bool Eval(Action<object>? logger, bool? mend)
		{
			try {
				return myCallback?.Invoke() ?? true;
			}
			catch (Exception ex){
				if (logger is not null){
					logger($"Error on leaf eval: {ex.Message}");
				}
				if (mend is not null){
					myCallback = delegate { return mend.Value; };
				}
			}
			return true;
		}
	}
	/// <summary>
	/// A compile time node. should always be stripped when finishing parse.
	/// </summary>
	[System.Diagnostics.DebuggerDisplay("{ToString()}")]
	public struct Group : IExpr
	{
		/// <summary>
		/// Child members.
		/// </summary>
		public IExpr[] members;
		/// <summary>
		/// Throws <see cref="InvalidOperationException"/>.
		/// </summary>
		/// <returns></returns>
		/// <exception cref="InvalidOperationException"></exception>

		public bool Eval(Action<object>? logger, bool? mend)
		{
			throw new InvalidOperationException("Groups should not exist!");
		}

		/// <summary>
		/// Throws <see cref="InvalidOperationException"/>.
		/// </summary>
		/// <returns></returns>
		/// <exception cref="InvalidOperationException"></exception>
		public void Populate(del_FetchPred? exchanger)
		{
			throw new InvalidOperationException("Groups should not exist!");
		}
	}
	/// <summary>
	/// An operator. Can have one or two operands (if one, it's always on the right).
	/// </summary>
	[System.Diagnostics.DebuggerDisplay("{ToString()}")]
	public struct Oper : IExpr
	{
		/// <summary>
		/// operation type
		/// </summary>
		public Op TP;
		/// <summary>
		/// Left operand
		/// </summary>
		public IExpr? L;
		/// <summary>
		/// right operand
		/// </summary>
		public IExpr? R;
		/// <summary>
		/// Creates a new operator with a given type.
		/// </summary>
		/// <param name="tP"></param>
		public Oper(Op tP)
		{
			TP = tP;
			L = null;
			R = null;
		}
		/// <inheritdoc/>
		public bool Eval(Action<object>? logger, bool? mend)
		{
			return TP switch
			{
				Op.AND => (L?.Eval(logger, mend) ?? true) && (R?.Eval(logger, mend) ?? true),
				Op.OR => (L?.Eval(logger, mend) ?? true) || (R?.Eval(logger, mend) ?? true),
				Op.XOR => (L?.Eval(logger, mend) ?? true) ^ (R?.Eval(logger, mend) ?? true),
				Op.NOT => !(R?.Eval(logger, mend) ?? true),
				_ => throw new ArgumentException("Invalid operator"),
			};
		}

		/// <inheritdoc/>
		public void Populate(del_FetchPred? exchanger)
		{
			L?.Populate(exchanger);
			R?.Populate(exchanger);
		}
		/// <inheritdoc/>
		public override string ToString()
		{
			return $"[ {L} {TP} {R} ]";
		}
	}
	/// <summary>
	/// A parsing token. Carries type and value.
	/// </summary>
	[System.Diagnostics.DebuggerDisplay("{type}:\"{val}\"")]
	public struct Token
	{
		/// <summary>
		/// Type of the instance.
		/// </summary>
		public TokenType type;
		/// <summary>
		/// Raw value of the instance.
		/// </summary>
		public string val;
		/// <summary>
		/// Creates a new instance wigh given type and raw value.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="val"></param>
		public Token(TokenType type, string val)
		{
			this.type = type;
			this.val = val;
		}
	}
	/// <summary>
	/// Operation type.
	/// </summary>
	public enum Op
	{
		/// <summary>
		/// Invert
		/// </summary>
		NOT,
		/// <summary>
		/// Logical AND
		/// </summary>
		AND,
		/// <summary>
		/// Exclusive OR
		/// </summary>
		XOR,
		/// <summary>
		/// Normal OR
		/// </summary>
		OR,
	}
	/// <summary>
	/// Token type. Order of enum items determines recognition precedence.
	/// </summary>
	public enum TokenType
	{
		/// <summary>
		/// Opening brace
		/// </summary>
		DelimOpen,
		/// <summary>
		/// Closing brace
		/// </summary>
		DelimClose,
		/// <summary>
		/// Whitespace
		/// </summary>
		Separator,
		/// <summary>
		/// Operator acting on two values
		/// </summary>
		Operator,
		/// <summary>
		/// Literals for function arguments
		/// </summary>
		Literal,
		/// <summary>
		/// Function name
		/// </summary>
		Word,
		//Discard
	}
	/// <summary>
	/// Eval invocation delegates.
	/// </summary>
	/// <returns></returns>
	public delegate bool del_Pred();
	/// <summary>
	/// Eval invocation delegates retrieval delegates. =
	/// </summary>
	/// <param name="name">Function name.</param>
	/// <param name="args">Arguments.</param>
	/// <returns>Delegate for selected word. Returning null is allowed.</returns>
	public delegate del_Pred? del_FetchPred(string name, params string[] args);
	#endregion
	#region statics
	/// <summary>
	/// Tries to create a new leaf from a specified position in an array of tokens
	/// </summary>
	/// <param name="tokens"></param>
	/// <param name="index"></param>
	/// <returns>Resulting leaf, null if token's type was not <see cref="TokenType.Word"/></returns>
	public static Leaf? MakeLeaf(Token[] tokens, in int index)
	{
		if (index < 0 || index >= tokens.Length) return null;
		Token tok = tokens[index];
		if (tok.type != TokenType.Word) return null;

		List<string> args = new();
		for (int i = index + 1; i < tokens.Length; i++)
		{
			Token argque = tokens[i];
			if (argque.type != TokenType.Literal) break;
			args.Add(argque.val);
		}
		return new Leaf(tok.val, args.ToArray());
	}
	/// <summary>
	/// Gets an operator type from token 
	/// </summary>
	/// <param name="t">Token to check</param>
	/// <returns>Resulting operation type, null if token was not an operator token.</returns>
	/// <exception cref="ArgumentException"></exception>
	public static Op? GetOp(in Token t)
	{
		if (t.type != TokenType.Operator) return null;
		return t.val.ToLower() switch
		{
			"|" or "or" => Op.OR,
			"&" or "and" => Op.AND,
			"^" or "xor" or "!=" => Op.XOR,
			"!" or "not" => Op.NOT,
			_ => throw new ArgumentException("Invalid token payload")
		};
	}
	/// <summary>
	/// Modify regex options
	/// </summary>
	public static RegexOptions regexops = RegexOptions.IgnoreCase;
	/// <summary>
	/// Returns a recognition regex object for a given token type.
	/// </summary>
	/// <param name="tt"></param>
	/// <returns></returns>
	/// <exception cref="IndexOutOfRangeException"></exception>
	private static Regex RegexForTT(TokenType tt)
	{
		return tt switch
		{
			//~~decide on delims usage
			//they stay as they are
			TokenType.DelimOpen => new Regex("[([{]", regexops),
			TokenType.DelimClose => new Regex("[)\\]}]", regexops),
			TokenType.Separator => new Regex("[\\s,]+", regexops),
			TokenType.Operator => new Regex("!=|[&|^!]|(and|or|xor|not)(?=\\s)", regexops),
			TokenType.Literal => new Regex("(?<=')[^']*(?=')|-?\\d+(\\.\\d+)?", regexops),
			TokenType.Word => new Regex("[a-zA-Z_]+", regexops),
			//TokenType.Discard => throw new NotImplementedException(),
			_ => throw new IndexOutOfRangeException("Supplied invalid token type"),
		};
	}
	/// <summary>
	/// precached token rec regexes
	/// </summary>
	private static readonly Dictionary<TokenType, Regex> exes;
	static PredicateInlay()
	{
		exes = new();
		foreach (TokenType val in Enum.GetValues(typeof(TokenType))) exes.Add(val, RegexForTT(val));
	}
	#endregion
}
