namespace RegionKit.Modules;

[RegionKitModule(nameof(Enable), nameof(Disable), moduleName: "Assets")]
internal static class _Assets
{
	public static void Enable()
	{


	}

	internal static string? FogOfWar
	{
		get
		{
			using var stream = RFL.Assembly.GetExecutingAssembly().GetManifestResourceStream("RegionKit.Assets.FogOfWar.txt");
			try
			{
				byte[] buff = new byte[stream.Length];
				stream.Read(buff, 0, (int)stream.Length);
				return System.Text.Encoding.UTF8.GetString(buff);
			}
			catch (Exception ex)
			{
				plog.LogError($"Could not load FogOfWar from ER! {ex}");
				return null;
			}
		}
		//var buff = //stream.Read(stream.Length);

	}
	public static void Disable()
	{


	}
}
