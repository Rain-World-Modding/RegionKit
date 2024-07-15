using RegionKit.Modules.Atmo.Helpers;
using static RegionKit.Modules.Atmo.Atmod;

using System.Text;
using RegionKit.Modules.Atmo.Body;

using static PredicateInlay;
using System.Text.RegularExpressions;

namespace RegionKit.Modules.Atmo.Gen;
/// <summary>
/// Responsible for filling <see cref="HappenSet"/>s from atmo script files.
/// </summary>
public class HappenParser {
	//this is still a mess but manageable
	#region fields
	#region statfields
	internal const RegexOptions OPTIONS = RegexOptions.IgnoreCase;
	private static readonly Dictionary<LineKind, Regex> __LineMatchers = new()
	{
		{LineKind.Comment, new Regex("//", OPTIONS)},
			{ LineKind.HappenWhat, new Regex("WHAT\\s*:\\s*", OPTIONS) },
			{LineKind.HappenBegin, new Regex("HAPPEN\\s*:\\s*", OPTIONS)},
			{LineKind.HappenWhen, new Regex("WHEN\\s*:\\s*", OPTIONS)},
			{LineKind.HappenWhere, new Regex("WHERE\\s*:\\s*", OPTIONS)},
			{LineKind.GroupBegin, new Regex("GROUP\\s*:\\s*", OPTIONS)},
			{LineKind.GroupEnd, new Regex("END\\s+GROUP", OPTIONS)},
			{LineKind.HappenEnd, new Regex("END\\s+HAPPEN", OPTIONS)},
			{LineKind.Other, new Regex(".*", OPTIONS) }
	};
	private static readonly LineKind[] __happenProps = new[] { LineKind.HappenWhere, LineKind.HappenWhen, LineKind.HappenWhat, LineKind.HappenEnd };
	internal static readonly Regex __roomSeparator = new("[\\s\\t]*,[\\s\\t]*|[\\s\\t]+", OPTIONS);
	#endregion statfields
	private readonly Dictionary<string, RoomGroup> _allGroups = new();
	private readonly List<HappenConfig> _allHappens = new();
	private readonly string[] _allLines;
	private int _index = 0;
	/// <summary>
	/// Whether the parser has finished. false if there's unprocessed lines.
	/// </summary>
	public bool done => _aborted || _index >= _allLines.Length;

	private static Dictionary<LineKind, Regex> LineMatchers => __LineMatchers;

	private bool _aborted;
	private string _currentLine = string.Empty;
	private ParsePhase _phase = ParsePhase.None;
	private HappenConfig _currentHappen;
	private RoomGroup? _currentGroup = null;
	#endregion fields
	/// <summary>
	/// Creates a parser for a specified file. <see cref="_Advance"/> the return value until it's <see cref="done"/>.
	/// </summary>
	/// <param name="file">Input file.</param>
	public HappenParser(System.IO.FileInfo file) {
		BangBang(file, nameof(file));
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
		_currentLine = _allLines[_index];
		Match
				group_que = LineMatchers[LineKind.GroupBegin].Match(_currentLine),
				happn_que = LineMatchers[LineKind.HappenBegin].Match(_currentLine);
		if (!_currentLine.StartsWith("//") && !_aborted)
		{
			try
			{
				switch (_phase)
				{
				case ParsePhase.None:
				{
					if (group_que.Success)
					{
						string name = _currentLine.Substring(group_que.Length);
						VerboseLog($"HappenParse: Beginning group block: {name}");
						_currentGroup = new(name);
						_phase = ParsePhase.Group;
					}
					else if (happn_que.Success)
					{
						_currentHappen = new(_currentLine.Substring(happn_que.Length));
						VerboseLog($"HappenParse: Beginning happen block: {_currentHappen.name}");
						_phase = ParsePhase.Happen;

					}
					break;
				}
				case ParsePhase.Group: _ParseGroup(); break;
				case ParsePhase.Happen: _ParseHappen(); break;
				default: break;
				}
			}
			catch (Exception ex)
			{
				LogError($"HappenParse: Irrecoverable error:" +
					$"\n{ex}" +
					$"\nAborting");
				_aborted = true;
			}
		}
		_index++;
	}
	private void _ParseGroup() {
		if (_currentGroup is null || string.IsNullOrWhiteSpace(_currentLine))
		{
			LogWarning($"Error parsing group: current group is null! aborting!");
			_phase = ParsePhase.None;
			return;
		}

		Match end = LineMatchers[LineKind.GroupEnd].Match(_currentLine);
		if (end.Success && end.Index == 0) {
			_FinalizeGroup();
			_phase = ParsePhase.None;
			return;
		}

		ParseGroupFromLine(_currentGroup, _currentLine);
	}

	private void ParseGroupFromLine(RoomGroup group, string line, WhereOps defaultOps = WhereOps.Include)
	{
		if (string.IsNullOrWhiteSpace(line)) return;

		if (line?.Length > 4 && line.StartsWith("./") && line.EndsWith("/."))
		{
			try
			{
				group._matchers.Add(new Regex(line.Substring(2, line.Length - 4)));
				VerboseLog($"HappenParse: Created a regex matcher for: {line}");
			}
			catch (Exception ex)
			{
				LogWarning($"HappenParse: error creating a regular expression in group block!\n{ex}\nSource line: {line}");
			}
			return;
		}

		foreach (string? ss in __roomSeparator.Split(line))
		{
			if (string.IsNullOrWhiteSpace(ss)) continue;
			defaultOps = ss[0] switch
			{
				'+' => WhereOps.Include,
				'-' => WhereOps.Exclude,
				'*' => WhereOps.Group,
				_ => defaultOps
			};

			string s = ss[0] switch { '+' or '-' or '*' => ss[1..].Trim(), _ => ss };

			switch (defaultOps)
			{
			case WhereOps.Include: group.includeRooms.Add(s); break;
			case WhereOps.Exclude: group.excludeRooms.Add(s); break;
			case WhereOps.Group:group._groupNames.Add(s); break;
			}
		}
	}

	private void _ParseHappen() {
		foreach (LineKind prop in __happenProps) {
			Match match = LineMatchers[prop].Match(_currentLine);

			if (match.Success && match.Index == 0) {
				string? payload = _currentLine.Substring(match.Length);
				switch (prop) {
				case LineKind.HappenWhere: {
					VerboseLog("HappenParse: Recognized WHERE clause");

					ParseGroupFromLine(_currentHappen.myGroup, payload, WhereOps.Group);
				}
				break;
				case LineKind.HappenWhat: {
					VerboseLog("HappenParse: Recognized WHAT clause");
					PredicateInlay.Token[]? tokens = PredicateInlay.Tokenize(payload).ToArray();
					for (int i = 0; i < tokens.Length; i++) {
						PredicateInlay.Token tok = tokens[i];
						if (tok.type == PredicateInlay.TokenType.Word) {
							PredicateInlay.Leaf leaf = PredicateInlay.MakeLeaf(tokens, in i) ?? new();
							_currentHappen.actions.Set(leaf.funcName, leaf.args.Select(x => x.ApplyEscapes()).ToArray());
						}
					}
				}
				break;
				case LineKind.HappenWhen:
					try {
						if (_currentHappen.conditions is not null) {
							LogWarning("HappenParse: Duplicate WHEN clause! Skipping! (Did you forget to close a previous Happen with END HAPPEN?)");
							break;
						}
						VerboseLog("HappenParse: Recognized WHEN clause");
						_currentHappen.conditions = new PredicateInlay(
							expression: payload,
							exchanger: null,
							logger: (data) => {
								LogWarning($"{_currentHappen.name}: {data}");
							},
							mendOnThrow: true);
					}
					catch (Exception ex) {
						LogError($"HappenParse: Error creating eval tree from a WHEN block for {_currentHappen.name}:\n{ex}");
						_currentHappen.conditions = null;
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

	private void _FinalizeHappen() 
	{
		_allHappens.Add(_currentHappen);
		_currentHappen = default;
	}

	private void _FinalizeGroup() {
		if (_currentGroup is null) 
		{ LogWarning("HappenParse: attempted to finalize group while group is null!"); }
		else 
		{
			string name = _currentGroup.name;
			VerboseLog($"HappenParse: ending group: {name}. Regex patterns: {_currentGroup._matchers.Count}, Literal rooms: {_currentGroup.includeRooms.Count}");
			_allGroups.Add(name, _currentGroup);
		}
		_currentGroup = null;
	}
	#region nested
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
		if (p._currentGroup is not null) {
			LogWarning($"HappenParse: Group {p._currentGroup.name} missing END! Last block in file, auto wrapping");
			p._FinalizeGroup();
		}
		if (p._currentHappen.name is not null) {
			LogWarning($"HappenParse: Happen {p._currentHappen.name} missing END! Last block in file, auto wrapping");

		}
		foreach (KeyValuePair<string, RoomGroup> groupPre in p._allGroups) 
		{
			RoomGroup group = groupPre.Value;
			group.EvaluateMatchers(game.world);
			group.EvaluateGroups(p._allGroups);
		}
		set.InsertGroups(p._allGroups.Values.ToList());
		foreach (HappenConfig cfg in p._allHappens) 
		{
			cfg.myGroup.EvaluateMatchers(game.world);
			cfg.myGroup.EvaluateGroups(p._allGroups);

			var ha = new Happen(cfg, set, game);
			set.InsertHappen(ha, cfg.myGroup);
		}
	}
}
