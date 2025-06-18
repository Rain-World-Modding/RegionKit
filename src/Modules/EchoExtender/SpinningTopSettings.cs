using System.IO;

namespace RegionKit.Modules.EchoExtender
{
	public struct SpinningTopSettings
	{
		/// <summary>
		/// Whether or not to use the default conversation for Spinning Top
		/// </summary>
		public bool HasDefaultConversation;
		/// <summary>
		/// UNIMPLEMENTED
		/// </summary>
		public float MinimumRipple;

		internal static SpinningTopSettings GetDefault()
		{
			return new SpinningTopSettings
			{
				HasDefaultConversation = true,
				MinimumRipple = -1f,
			};
		}

		public static SpinningTopSettings FromFile(string path)
		{
			SpinningTopSettings settings = GetDefault();

			if (!File.Exists(path))
			{
				LogError("[Echo Extender] No settings file found! Using default");
				return settings;
			}

			LogMessage("[Echo Extender] Found ST settings file: " + path);
			string[] rows = File.ReadAllLines(path);

			foreach (string row in rows)
			{
				if (row.StartsWith("#") || row.StartsWith("//")) continue;
				try
				{
					string[] split = row.Split(':');
					string pass = split[0].Trim();
					string trimmed = split.Length >= 2 ? split[1].Trim() : "";
					bool
						sfloat = float.TryParse(trimmed, out float floatval),
						sint = int.TryParse(trimmed, out int intval);
					switch (pass.Trim().ToLower())
					{
					case "defaultconversation":
						settings.HasDefaultConversation = bool.Parse(trimmed);
						break;
					case "minimumripple":
						settings.MinimumRipple = floatval;
						break;
					default:
						LogWarning($"[Echo Extender] Setting '{pass.Trim().ToLower()}' not found! Skipping : " + row);
						break;
					}
				}
				catch (Exception ex)
				{
					LogWarning($"[Echo Extender] Failed to parse line \"{row}\" : {ex}");
				}
			}

			return settings;
		}
	}
}
