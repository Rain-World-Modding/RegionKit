using static RegionKit.Modules.Atmo.Atmod;

using System.Text;
using RegionKit.Modules.Atmo.Body;
using System.Text.RegularExpressions;

namespace RegionKit.Modules.Atmo.Gen;
/// <summary>
/// Responsible for filling <see cref="HappenSet"/>s from atmo script files.
/// </summary>
public class HappenParser
{
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
	internal static readonly Regex _splitCommaIgnoreWhitespace = new("[\\s\\t]*,[\\s\\t]*", OPTIONS);
	internal static readonly Regex _splitWhitespace = new("[\\s\\t]+", OPTIONS);
	#endregion statfields
	private readonly Dictionary<string, RoomGroup> _allGroups = new();
	private readonly Dictionary<Happen, RoomGroup> _allHappens = new();
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
	private Happen? _currentHappen;
	private RoomGroup? _currentGroup = null;

	private HappenSet set;
	#endregion fields
	/// <summary>
	/// Creates a parser for a specified file. <see cref="_Advance"/> the return value until it's <see cref="done"/>.
	/// </summary>
	/// <param name="file">Input file.</param>
	public HappenParser(System.IO.FileInfo file, HappenSet set)
	{
		BangBang(file, nameof(file));
		VerboseLog($"HappenParse: booting for file: {file.FullName}");
		if (!file.Exists)
		{
			_aborted = true;
			LogfixWarning($"HappenParse: file does not exist. aborted");
			_allLines = new string[0];
			return;
		}
		this.set = set;
		_allLines = System.IO.File.ReadAllLines(file.FullName, Encoding.UTF8);
	}
	internal void _Advance()
	{
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
						_currentHappen = new(_currentLine.Substring(happn_que.Length), set, set.world.game); 
						_allHappens[_currentHappen] = new(_currentHappen.name);
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
	private void _ParseGroup()
	{
		if (_currentGroup is null || string.IsNullOrWhiteSpace(_currentLine))
		{
			LogfixWarning($"Error parsing group: current group is null! aborting!");
			_phase = ParsePhase.None;
			return;
		}

		Match end = LineMatchers[LineKind.GroupEnd].Match(_currentLine);
		if (end.Success && end.Index == 0)
		{
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
				LogfixWarning($"HappenParse: error creating a regular expression in group block!\n{ex}\nSource line: {line}");
			}
			return;
		}

		foreach (string? ss in _splitCommaIgnoreWhitespace.Split(line))
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
			case WhereOps.Group: group._groupNames.Add(s); break;
			}
		}
	}

	private void _ParseHappen()
	{
		if (_currentHappen is null)
		{
			LogfixWarning($"Error parsing happen: current happen is null! aborting!");
			_phase = ParsePhase.None;
			return;
		}

		foreach (LineKind prop in __happenProps)
		{
			Match match = LineMatchers[prop].Match(_currentLine);

			if (match.Success && match.Index == 0)
			{
				string? payload = _currentLine.Substring(match.Length);
				switch (prop)
				{
				case LineKind.HappenWhere:
					VerboseLog("HappenParse: Recognized WHERE clause");
					ParseGroupFromLine(_allHappens[_currentHappen], payload, WhereOps.Group);
					break;

				case LineKind.HappenWhat:
					VerboseLog("HappenParse: Recognized WHAT clause");
					_currentHappen.AddActions(NewParser.ParseActions(_currentHappen, payload));
					break;

				case LineKind.HappenWhen:
					VerboseLog("HappenParse: Recognized WHEN clause");
					_currentHappen.AddTrigger(NewParser.ParseTriggerLine(_currentHappen, payload));
					break;

				case LineKind.HappenEnd:
					VerboseLog("HappenParse: finishing a happen block");
					if (!_currentHappen.Finalize())
					{ _allHappens.Remove(_currentHappen); }
					_currentHappen = null;
					_phase = ParsePhase.None;
					break;

				default:
					break;
				}
				break;
			}
		}
	}

	private void _FinalizeGroup()
	{
		if (_currentGroup is null)
		{ LogfixWarning("HappenParse: attempted to finalize group while group is null!"); }
		else
		{
			string name = _currentGroup.name;
			VerboseLog($"HappenParse: ending group: {name}. Regex patterns: {_currentGroup._matchers.Count}, Literal rooms: {_currentGroup.includeRooms.Count}");
			_allGroups.Add(name, _currentGroup);
		}
		_currentGroup = null;
	}
	#region nested
	private enum WhereOps
	{
		Group,
		Include,
		Exclude,
	}
	private enum LineKind
	{
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
	private enum ParsePhase
	{
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
	public static void Parse(System.IO.FileInfo file, HappenSet set, RainWorldGame game)
	{
		BangBang(file, "file");
		BangBang(set, "set");
		BangBang(game, "rwg");
		HappenParser p = new(file, set);
		for (int i = 0; i < p._allLines.Length; i++)
		{
			p._Advance();
		}
		if (p._currentGroup is not null)
		{
			LogfixWarning($"HappenParse: Group {p._currentGroup.name} missing END! Last block in file, auto wrapping");
			p._FinalizeGroup();
		}
		if (p._currentHappen is not null)
		{
			LogfixWarning($"HappenParse: Happen {p._currentHappen.name} missing END! Last block in file, auto wrapping");

		}
		foreach (RoomGroup group in p._allGroups.Values)
		{
			group.EvaluateMatchers(game.world);
			group.EvaluateGroups(p._allGroups);
		}
		set.InsertGroups(p._allGroups.Values.ToList());
		foreach ((Happen happen, RoomGroup group) in p._allHappens)
		{
			LogMessage($"evaluate and add [{happen.name}] with rooms [{string.Join(", ", group.Rooms)}]");

			group.EvaluateMatchers(game.world);
			group.EvaluateGroups(p._allGroups);

			set.InsertHappen(happen, group);
		}
	}
}
