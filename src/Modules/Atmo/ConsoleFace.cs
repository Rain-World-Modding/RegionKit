//off until slime ports devconsole

using DC = DevConsole;
using CMD = DevConsole.Commands;
using DCLI = DevConsole.GameConsole;
using RegionKit.Modules.Atmo.API;

namespace RegionKit.Modules.Atmo;

internal static class ConsoleFace {
	#region fields
	private static ulong mfinv_uid;
	private static string[] __autocomplete_empty = new string[0];
	private static string[] __autocomplete_listcall = new[] { _LIST, _CALL };
	private static string[] __autocomplete_getset = new[] { _GET, _SET };
	private static string[] __autocomplete_printsave = new[] { _PRINT, _SAVE };
	private const string _SET = "set";
	private const string _GET = "get";
	private const string _LIST = "list";
	private const string _CALL = "call";
	private const string _PRINT = "print";
	private const string _SAVE = "save";
	private const string _HELP_VAR = """
		Invalid args!
		atmo_var get [varname] - fetches value of specified variable
		atmo_var set [varname] [value] - sets specified variable to value
		""";
	private const string _HELP_METAF = """
		Invalid args!
		atmo_metafunc list - lists all available metafunctions
		atmo_metafunc call [name] [input] (print|save) (DECIMAL|INTEGER|VECTOR|BOOL|STRING?)=STRING - calls a specified metafunction with given input, and either prints result to console or stores it in a variable, using specified datatype. NOTE: Only the immediate result is stored, if result is dynamic, it will be discarded.
		""";

	#endregion;
	public static void Apply() {
		new CMD.CommandBuilder("atmo_var")
			.AutoComplete(__Autocomplete_Var)
			.Run(__Run_Var)
			.Help("""
			atmo_var get [varname]
			atmo_var set [varname] [value]
			""")
			.Register();
		new CMD.CommandBuilder("atmo_metafunc")
			.AutoComplete(__Autocomplete_Metafun)
			.Run(__Run_Metafun)
			.Help("""
			atmo_metafunc list
			atmo_metafunc call [name] [input] (print|save) (DECIMAL|INTEGER|VECTOR|BOOL|STRING?)=STRING
			""")
			.Register();
		new CMD.CommandBuilder("atmo_perf")
			.RunGame((game, args) => {
				DCLI.WriteLine("""
					All times in milliseconds
					""");
				foreach (Body.Happen.Perf rec in inst.CurrentSet!.GetPerfRecords()) {
					DCLI.WriteLine($"{rec.name}\t: {rec.avg_realup}\t: {rec.samples_realup}\t: {rec.avg_eval}\t: {rec.samples_eval}");
				}
			})
			//.Help("atmo_perf - Fetches performance records from currently active happens")
			.Register();
	}
	private static IEnumerable<string> __Autocomplete_Var(string[] args)
		=> args switch {
			[] => __autocomplete_getset,
			_ => __autocomplete_empty
		};
	private static void __Run_Var(string[] args) {
		void showhelp() {
			DCLI.WriteLine(_HELP_VAR);
		}
		if (args.Length < 2) {
			__NotifyArgsMissing(__Run_Var, "action", "name");
			showhelp();
			return;
		}
		Arg target = VarRegistry.GetVar(args[1], __CurrentSaveslot ?? -1, __CurrentCharacter ?? __slugnameNotFound);
		if (args.AtOr(0, _GET) is _GET) {
			DCLI.WriteLine(target.ToString());
		}
		else {
			if (args.Length < 3) {
				__NotifyArgsMissing(__Run_Var, "value");
				showhelp();
				return;
			}
			target.Str = args[1];
		}

	}

	private static IEnumerable<string> __Autocomplete_Metafun(string[] args)
		=> args switch {
			[] => __autocomplete_listcall,
			[_CALL] => __namedMetafuncs.Keys,
			[_CALL, _, _] => __autocomplete_printsave,
			[_CALL, _, _, _, _] => Enum.GetNames(typeof(ArgType)),
			_ => __autocomplete_empty
		};
	private static void __Run_Metafun(string[] args) {
		void showhelp() {
			DCLI.WriteLine(_HELP_METAF);
		}
		switch (args.AtOr(0, _LIST)) {
		case _LIST: {
			DCLI.WriteLine($"Registered metafunctions: [ {__namedMetafuncs.Keys.Stitch()} ]");
			break;
		}

		case _CALL: {
			int ss = __CurrentSaveslot ?? -1;
			SlugcatStats.Name ch = __CurrentCharacter ?? SlugcatStats.Name.Red;
			if (args.Length < 3) {
				__NotifyArgsMissing(__Run_Metafun, "name", "input");
				showhelp();
				break;
			}
			Arg? res = VarRegistry.GetMetaFunction($"{args[1]} {args[2]}", ss, ch);
			if (res is null) {
				DCLI.WriteLine("Metafunction not found!");
				break;
			}

			var dest = args.AtOr(4, $"v_DCLI_DUMP_{mfinv_uid++}");
			Arg target = VarRegistry.GetVar(dest, ss, ch);
			TryParseEnum(args.AtOr(5, nameof(ArgType.STRING)), out ArgType at);
			if (args.AtOr(3, "print") is "save") {
				__logger.DbgVerbose($"Saving {res} to {dest}");
				Assign(res, target, at);
			}
			else {
				__logger.DbgVerbose($"Printing {res}[{at}] to console");
				DCLI.WriteLine(res[at]?.ToString() ?? "NULL");
			}
			break;
		}
		}
	}

	private static void __NotifyArgsMissing(Delegate where, params string[] args) {
		DCLI.WriteLine($"ATMO.CLI: {where?.Method}: Missing arguments! " +
			$"Required arguments: {args.Stitch()}");
	}
}
