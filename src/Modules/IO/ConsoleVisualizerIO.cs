using MoreSlugcats;

namespace RegionKit.Modules.IO
{
	public static class ConsoleVisualizerIO
	{
		public static void Apply()
		{
			On.MoreSlugcats.ConsoleVisualizer.ctor += ctor;
			On.MoreSlugcats.ConsoleVisualizer.Visibility += Visibility;
		}

		public static void Unapply()
		{
			On.MoreSlugcats.ConsoleVisualizer.ctor -= ctor;
			On.MoreSlugcats.ConsoleVisualizer.Visibility -= Visibility;
		}

		private static void ctor(On.MoreSlugcats.ConsoleVisualizer.orig_ctor orig, ConsoleVisualizer self)
		{
			orig(self);

			self.CustomData().IOLog = new FLabel(GetFont(), string.Empty);
			Futile.stage.AddChild(self.CustomData().IOLog);
			self.CustomData().IOLog.x = 19.666666f;
			self.CustomData().IOLog.y = 699.666672f;
			self.CustomData().IOLog.alignment = FLabelAlignment.Left;

			self.CustomData().RecentIOLog = new List<string>();
		}

		private static void Visibility(On.MoreSlugcats.ConsoleVisualizer.orig_Visibility orig, ConsoleVisualizer self, bool visibility)
		{
			orig(self, visibility);
			self.CustomData().IOLog.isVisible = visibility;
		}

		/// <summary>
		/// Logs a message to the I/O log (Press "K" with devtools open to see)
		/// </summary>
		public static void LogIO(RainWorldGame game, string message)
		{
			_CWTs.CustomData(game.console).RecentIOLog.Add(message);
			if (_CWTs.CustomData(game.console).RecentIOLog.Count > 16)
			{
				_CWTs.CustomData(game.console).RecentIOLog.RemoveRange(0, _CWTs.CustomData(game.console).RecentIOLog.Count - 16);
			}

			message = "";
			foreach (string s in _CWTs.CustomData(game.console).RecentIOLog)
			{
				message += s + Environment.NewLine;
			}

			_CWTs.CustomData(game.console).IOLog.text = message;
			_CWTs.CustomData(game.console).IOLog.SetPosition(new Vector2(19.666666f, 699.666672f - _CWTs.CustomData(game.console).IOLog.textRect.height / 2f));
		}
	}
}
