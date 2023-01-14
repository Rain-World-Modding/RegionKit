namespace RegionKit;

[RegionKitModule(nameof(Enable), nameof(Disable), moduleName: "Assets")]
internal static class _Assets
{
	public static void Enable()
	{
	}

	internal static string FogOfWar
	{
		get
		{
			using var stream = RFL.Assembly.GetExecutingAssembly().GetManifestResourceStream("RegionKit.Assets.FogOfWar.txt");
			byte[] buff = new byte[stream.Length];
			stream.Read(buff, 0, (int)stream.Length);
			return System.Text.Encoding.UTF8.GetString(buff);
		}
		//var buff = //stream.Read(stream.Length);

	}
	public static void Disable()
	{


	}
}
