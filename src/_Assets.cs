namespace RegionKit;

[RegionKitModule(nameof(Enable), nameof(Disable), moduleName: "Assets")]
internal static class _Assets
{
	public static void Enable()
	{
	}

	internal static string FogOfWar => GetUTF8("FogOfWar.txt")!;

	internal static string? GetUTF8(params string[] assetpath)
	{
		//Func<string, string, string>? aggregator = (x, y) => $"{x}.{y}";
		var buff = GetBytes(assetpath);
		return System.Text.Encoding.UTF8.GetString(buff);
	}
	internal static byte[]? GetBytes(params string[] assetpath)
	{
		Func<string, string, string>? aggregator = (x, y) => $"{x}.{y}";
		using var stream = RFL.Assembly.GetExecutingAssembly().GetManifestResourceStream($"RegionKit.Assets.{assetpath.Stitch((Func<string, string, string>?)aggregator)}");
		if (stream is null) return null;
		byte[] buff = new byte[stream.Length];
		stream.Read(buff, 0, (int)stream.Length);
		return buff;
	}
	public static void Disable()
	{

	}
}
