using RegionKit.Modules.Atmo.Helpers;
using static RegionKit.Modules.Atmo.Atmod;
using TXT = System.Text.RegularExpressions;

using System.Text;
using RegionKit.Modules.Atmo.Body;

using static PredicateInlay;

namespace RegionKit.Modules.Atmo.Gen;
/// <summary>
/// Responsible for filling <see cref="HappenSet"/>s from atmo script files.
/// </summary>
public class HappenParser {
	//this is still a mess but manageable
	#region fields
	#region statfields
	internal const TXT.RegexOptions OPTIONS = TXT.RegexOptions.IgnoreCase;
	private static readonly Dictionary<LineKind, TXT.Regex> __LineMatchers;
	private static readonly LineKind[] __happenProps = new[] { LineKind.HappenWhere, LineKind.HappenWhen, LineKind.HappenWhat, LineKind.HappenEnd };
	internal static readonly TXT.Regex __roomsep = new("[\\s\\t]*,[\\s\\t]*|[\\s\\t]+", OPTIONS);
	#endregion statfields
	private readonly Dictionary<string, GroupContents> _allGroupContents = new();
	private readonly List<HappenConfig> _retrievedHappens = new();
	private readonly string[] _allLines;
	private int _index = 0;
	/// <summary>
	/// Whether the parser has finished. false if there's unprocessed lines.
	/// </summary>
	public bool done => _aborted || _index >= _allLines.Length;
	private bool _aborted;
	private string _cline;
	private ParsePhase _phase = ParsePhase.None;
	private HappenConfig _cHapp;
	private string? _cGroupName = null;
	private GroupContents _cGroupContents = new();
	#endregion fields
	/// <summary>
	/// Creates a parser for a specified file. <see cref="_Advance"/> the return value until it's <see cref="done"/>.
	/// </summary>
	/// <param name="file">Input file.</param>
	public HappenParser(System.IO.FileInfo file) {
		BangBang(file, nameof(file));
		_cline = string.Empty;
		VerboseLog($"HappenParse: booting for file: {file.FullName}");
		if (!file.Exists) {
			_aborted = true;
			LogWarning($"HappenParse: file does not exist. aborted");
			_allLines = new string[0];
			return;
		}
		_allLines = System.IO.File.ReadAllLines(file.FullName, Encoding.UTF8);
	}
	internal void _Advance() {
		_cline = _allLines[_index];
		TXT.Match
				group_que = __LineMatchers[LineKind.GroupBegin].Match(_cline),
				happn_que = __LineMatchers[LineKind.HappenBegin].Match(_cline);
		if (_cline.StartsWith("//") || _aborted) goto stop;
		try {
			switch (_phase) {
			case ParsePhase.None: {
				if (group_que.Success) {
					_cGroupName = _cline.Substring(group_que.Length);
					VerboseLog($"HappenParse: Beginning group block: {_cGroupName}");
					_phase = ParsePhase.Group;
				}
				else if (happn_que.Success) {
					_cHapp = new(_cline.Substring(happn_que.Length));
					VerboseLog($"HappenParse: Beginning happen block: {_cHapp.name}");
					_phase = ParsePhase.Happen;

				}
			}
			break;
			case ParsePhase.Group: {
				_ParseGroup();
			}
			break;
			case ParsePhase.Happen: {
				_ParseHappen();
			}
			break;
			default:
				break;
			}
		}
		catch (Exception ex) {
			LogError($"HappenParse: Irrecoverable error:" +
				$"\n{ex}" +
				$"\nAborting");
			_aborted = true;
		}
	stop:
		_index++;
	}
	private void _ParseGroup() {
		TXT.Match ge;
		if (_cGroupName is null) {
			LogWarning($"Error parsing group: current name is null! aborting!");
			_phase = ParsePhase.None;
			return;
		}
		if ((ge = __LineMatchers[LineKind.GroupEnd].Match(_cline)).Success && ge.Index == 0) {
			_FinalizeGroup();
			_phase = ParsePhase.None;
			return;
		}
		if (_cline?.Length > 4 && _cline.StartsWith("./") && _cline.EndsWith("/.")) {
			try {
				_cGroupContents.matchers.Add(new TXT.Regex(_cline.Substring(2, _cline.Length - 4)));
				VerboseLog($"HappenParse: Created a regex matcher for: {_cline}");
			}
			catch (Exception ex) {
				LogWarning($"HappenParse: error creating a regular expression in group block!" +
					$"\n{ex}" +
					$"\nSource line: {_cline}");
			}
			return;
		}
		foreach (string? ss in __roomsep.Split(_cline)) {
			if (ss.Length is 0) continue;
			_cGroupContents.rooms.Add(ss);
		}
	}

	private void _ParseHappen() {
		foreach (LineKind prop in __happenProps) {
			TXT.Match match;
			TXT.Regex matcher = __LineMatchers[prop];
			if ((match = matcher.Match(_cline)).Success && match.Index == 0) {
				string? payload = _cline.Substring(match.Length);
				switch (prop) {
				case LineKind.HappenWhere: {
					VerboseLog("HappenParse: Recognized WHERE clause");
					WhereOps c = WhereOps.Group;

					var items = Tokenize(payload);
					foreach (Token t in items) {
						//tokenize to allow multiword subregion names
						if (t.type is TokenType.Separator) continue;
						string item = t.val;
						if (item.Length == 0) continue;
						switch (item) {
						case "+":
							c = WhereOps.Include; break;
						case "-":
							c = WhereOps.Exclude; break;
						case "*":
							c = WhereOps.Group; break;
						default:
							switch (c) {
							case WhereOps.Group:
								_cHapp.groups.Add(item);
								break;
							case WhereOps.Include:
								_cHapp.include.Add(item);
								break;
							case WhereOps.Exclude:
								_cHapp.exclude.Add(item);
								break;
							}
							break;
						}
					}
				}
				break;
				case LineKind.HappenWhat: {
					VerboseLog("HappenParse: Recognized WHAT clause");
					PredicateInlay.Token[]? tokens = PredicateInlay.Tokenize(payload).ToArray();
					for (int i = 0; i < tokens.Length; i++) {
						PredicateInlay.Token tok = tokens[i];
						if (tok.type == PredicateInlay.TokenType.Word) {
							PredicateInlay.Leaf leaf = PredicateInlay.MakeLeaf(tokens, in i) ?? new();
							_cHapp.actions.Set(leaf.funcName, leaf.args.Select(x => x.ApplyEscapes()).ToArray());
						}
					}
				}
				break;
				case LineKind.HappenWhen:
					try {
						if (_cHapp.conditions is not null) {
							LogWarning("HappenParse: Duplicate WHEN clause! Skipping! (Did you forget to close a previous Happen with END HAPPEN?)");
							break;
						}
						VerboseLog("HappenParse: Recognized WHEN clause");
						_cHapp.conditions = new PredicateInlay(
							expression: payload,
							exchanger: null,
							logger: (data) => {
								LogWarning($"{_cHapp.name}: {data}");
							},
							mendOnThrow: true);
					}
					catch (Exception ex) {
						LogError($"HappenParse: Error creating eval tree from a WHEN block for {_cHapp.name}:\n{ex}");
						_cHapp.conditions = null;
					}
					break;
				case LineKind.HappenEnd:
					VerboseLog("HappenParse: finishing a happen block");
					_FinalizeHappen();
					_phase = ParsePhase.None;
					break;
				default:
					break;
				}
				break;
			}
		}
	}

	private void _FinalizeHappen() {
		_retrievedHappens.Add(_cHapp);
		_cHapp = default;
	}

	private void _FinalizeGroup() {
		if (_cGroupName is null) {
			LogWarning("HappenParse: attempted to finalize group while group is null!");

		}
		else {
			VerboseLog($"HappenParse: ending group: {_cGroupName}. " +
							$"Regex patterns: {_cGroupContents.matchers.Count}, " +
							$"Literal rooms: {_cGroupContents.rooms.Count}");
			_allGroupContents.Add(_cGroupName, _cGroupContents);
		}
		_cGroupName = null;
		_cGroupContents = new();
	}
	#region nested
	private struct GroupContents {
		internal List<string> rooms = new();
		internal List<TXT.Regex> matchers = new();
		public GroupContents() {
		}
		public GroupContents Finalize(World w) {
			foreach (TXT.Regex? matcher in matchers) {
				for (int i = 0; i < w.abstractRooms.Length; i++) {
					if (matcher.IsMatch(w.abstractRooms[i].name)) rooms.Add(w.abstractRooms[i].name);
				}
			}
			return this;
		}
	}
	private enum WhereOps {
		Group,
		Include,
		Exclude,
	}
	private enum LineKind {
		Comment,
		GroupBegin,
		GroupEnd,
		HappenBegin,
		HappenWhat,
		HappenWhen,
		HappenWhere,
		HappenEnd,
		Other,
	}
	private enum ParsePhase {
		None,
		Group,
		Happen
	}
	#endregion
	#region regex
	private static TXT.Regex MatcherForLK(LineKind lk) {
		return lk switch {
			LineKind.Comment => new TXT.Regex("//", OPTIONS),
			LineKind.HappenWhat => new TXT.Regex("WHAT\\s*:\\s*", OPTIONS),
			LineKind.HappenBegin => new TXT.Regex("HAPPEN\\s*:\\s*", OPTIONS),
			LineKind.HappenWhen => new TXT.Regex("WHEN\\s*:\\s*", OPTIONS),
			LineKind.HappenWhere => new TXT.Regex("WHERE\\s*:\\s*", OPTIONS),
			LineKind.GroupBegin => new TXT.Regex("GROUP\\s*:\\s*", OPTIONS),
			LineKind.GroupEnd => new TXT.Regex("END\\s+GROUP", OPTIONS),
			LineKind.HappenEnd => new TXT.Regex("END\\s+HAPPEN", OPTIONS),
			LineKind.Other => new TXT.Regex(".*", OPTIONS),
			_ => throw new ArgumentException("Invalid LineKind state supplied!"),
		};
	}
	#endregion
	/// <summary>
	/// Attempts to read a file and parse it as a script.
	/// </summary>
	/// <param name="file">File to check from.</param>
	/// <param name="set">Happenset instance to add results to.</param>
	/// <param name="game">Current RainWorldGame instance.</param>
	public static void Parse(System.IO.FileInfo file, HappenSet set, RainWorldGame game) {
		BangBang(file, "file");
		BangBang(set, "set");
		BangBang(game, "rwg");
		HappenParser p = new(file);
		for (int i = 0; i < p._allLines.Length; i++) {
			p._Advance();
		}
		if (p._cGroupName is not null) {
			LogWarning($"HappenParse: Group {p._cGroupName} missing END! Last block in file, auto wrapping");
			p._FinalizeGroup();
		}
		if (p._cHapp.name is not null) {
			LogWarning($"HappenParse: Happen {p._cHapp.name} missing END! Last block in file, auto wrapping");

		}
		Dictionary<string, IEnumerable<string>> groupsFinal = new();
		foreach (KeyValuePair<string, GroupContents> groupPre in p._allGroupContents) {
			GroupContents fin = groupPre.Value.Finalize(set.world);
			groupsFinal.Add(groupPre.Key, fin.rooms);
		}
		set.InsertGroups(groupsFinal);
		foreach (HappenConfig cfg in p._retrievedHappens) {
			var ha = new Happen(cfg, set, game);
			set.InsertHappens(new[] { ha });
			set.AddBind(ha, cfg.groups);
			set.AddExcludes(ha, cfg.exclude);
			set.AddIncludes(ha, cfg.include);
		}
	}
	static HappenParser() {
		__LineMatchers = new();
		foreach (LineKind lk in Enum.GetValues(typeof(LineKind))) __LineMatchers.Add(lk, MatcherForLK(lk));
	}
}
