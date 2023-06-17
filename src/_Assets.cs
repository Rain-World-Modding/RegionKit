
namespace RegionKit;

[RegionKitModule(nameof(Enable), nameof(Disable), moduleName: "Assets")]
internal static class _Assets
{
	private static bool __appliedOnce = false;
	public static void Enable()
	{
		LoadResources();
		__appliedOnce = true;
	}

	internal static void LoadResources(/* On.RainWorld.orig_OnModsInit orig, RainWorld self */)
	{
		// orig(self);
		__logger.LogMessage($"Assets module loading atlases from assetpath assets/regionkit");

		foreach (string? dir in new[] { "", "sprites", /* "littleplanet", "extendedgates" */ })
		{
			string fdir = $"assets/regionkit{(dir is not "" ? "/" + dir : dir)}";
			foreach (string f in AssetManager.ListDirectory(fdir, false, true))
			{
				string? atlasname = null;
				System.IO.FileInfo fi = new(f);
				if (fi.Extension is ".png")
				{
					//__logger.LogMessage($"Assets module Found an image: {f}");
					//FAtlasManager..LoadImage("");
					atlasname = fi.Name[..^fi.Extension.Length];
					Futile.atlasManager.LoadAtlasOrImage($"{fdir}/{atlasname}");
					

#if false
					System.IO.FileInfo
						slicerfile = new(atlasname + ".json"),
						metafile = new(atlasname + ".meta");
					using System.IO.Stream 
						pngstream = fi.OpenRead();
					using System.IO.Stream?
						slicerstream = slicerfile.Exists ? slicerfile.OpenRead() : null,
						metastream = metafile.Exists ? metafile.OpenRead() : null;
					__logger.LogDebug($"Assets module loading png {pngstream.Length}, slicer {slicerstream?.Length ?? -1}, meta {metastream?.Length ?? -1}");
					LoadCustomAtlas(atlasname, pngstream, slicerstream, metastream);
#endif
					//fi.AppendText();
				}
				try
				{
				}
				catch (Exception ex)
				{
					__logger.LogError($"Assets module Failed to load atlas {atlasname} : {ex}");
				}
				//if (fi.)
			}
		}
	}

	internal static string? GetUTF8(params string[] assetpath)
	{
		//Func<string, string, string>? aggregator = (x, y) => $"{x}.{y}";
		var buff = GetBytes(assetpath);
		return System.Text.Encoding.UTF8.GetString(buff);
	}
	internal static byte[]? GetBytes(params string[] assetpath)
	{
		using System.IO.Stream? stream = GetStream(assetpath);
		if (stream is null) return null;
		byte[] buff = new byte[stream.Length];
		stream.Read(buff, 0, (int)stream.Length);
		return buff;
	}
	/// <summary>
	/// Returns an asset stream, taking from merged assets or ER. Dispose of it properly!
	/// </summary>
	internal static System.IO.Stream? GetStream(params string[] assetpath)
	{
		Func<string, string, string>
			dotjoin = (x, y) => $"{x}.{y}",
			slashjoin = (x, y) => $"{x}/{y}";
		var file = AssetManager.ResolveFilePath($"assets/regionkit/{assetpath.Stitch(slashjoin)}");
		if (System.IO.File.Exists(file)) return System.IO.File.OpenRead(file);
		return System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream($"RegionKit.Assets.Embedded.{assetpath.Stitch(dotjoin)}");
	}

	#region ATLASES

	// internal static void LoadAditionalResources()
	// {
	// 	if (__appliedOnce) return;
	// 	const string LittlePlanet = "LittlePlanet";
	// 	const string ExtendedGates = "ExtendedGates";
	// 	foreach (string[] path in new[] {
	// 		new[] { LittlePlanet, LittlePlanet},
	// 		new[] { LittlePlanet, "LittlePlanetRing"},
	// 		new[] { ExtendedGates, "ExtendedGateSymbols"}  }) try
	// 		{
	// 			using System.IO.Stream?
	// 				pngstream = _Assets.GetStream(path[0], $"{path[1]}.png")!,
	// 				jsonstream = _Assets.GetStream(path[0], $"{path[1]}.json")!;
	// 			LoadCustomAtlas(path[1], pngstream, jsonstream);
	// 		}
	// 		catch (Exception ex)
	// 		{
	// 			__logger.LogError($"Problem loading LittlePlanet atlases {ex}");
	// 		}
	// 	__logger.LogInfo("Loading additional EG resources");


	// }

	internal static KeyValuePair<string, string> MetaEntryToKeyVal(string input)
	{
		if (string.IsNullOrEmpty(input)) return new KeyValuePair<string, string>("", "");
		string[] pieces = input.Split(new char[] { ':' }, 2); // No trim option in framework 3.5
		if (pieces.Length == 0) return new KeyValuePair<string, string>("", "");
		if (pieces.Length == 1) return new KeyValuePair<string, string>(pieces[0].Trim(), "");
		return new KeyValuePair<string, string>(pieces[0].Trim(), pieces[1].Trim());
	}

	internal static FAtlas LoadCustomAtlas(string atlasName, System.IO.Stream textureStream, System.IO.Stream? slicerStream, System.IO.Stream? metaStream = null)
	{
		try
		{
			Texture2D imageData = new Texture2D(0, 0, TextureFormat.ARGB32, false);
			byte[] bytes = new byte[textureStream.Length];
			textureStream.Read(bytes, 0, (int)textureStream.Length);
			imageData.LoadImage(bytes);
			Dictionary<string, object>? slicerData = null;
			if (slicerStream != null)
			{
				System.IO.StreamReader sr = new(slicerStream, System.Text.Encoding.UTF8);
				slicerData = sr.ReadToEnd().dictionaryFromJson();
			}
			Dictionary<string, string>? metaData = null;
			if (metaStream != null)
			{
				System.IO.StreamReader sr = new(metaStream, System.Text.Encoding.UTF8);
				metaData = new Dictionary<string, string>(); // Boooooo no linq and no splitlines, shame on you c#
				for (string fullLine = sr.ReadLine(); fullLine != null; fullLine = sr.ReadLine())
				{
					(metaData as IDictionary<string, string>).Add(MetaEntryToKeyVal(fullLine));
				}
			}

			return LoadCustomAtlas(atlasName, imageData, slicerData, metaData);
		}
		finally
		{
			textureStream.Close();
			slicerStream?.Close();
			metaStream?.Close();
		}
	}

	internal static FAtlas LoadCustomAtlas(string atlasName, Texture2D imageData, Dictionary<string, object>? slicerData, Dictionary<string, string>? metaData)
	{
		// Some defaults, metadata can overwrite
		// common snense
		if (slicerData != null) // sprite atlases are mostly unaliesed
		{
			imageData.anisoLevel = 1;
			imageData.filterMode = 0;
		}
		else // Single-image should clamp
		{
			imageData.wrapMode = TextureWrapMode.Clamp;
		}

		if (metaData != null)
		{
			metaData.TryGetValue("aniso", out string anisoValue);
			if (!string.IsNullOrEmpty(anisoValue) && int.Parse(anisoValue) > -1) imageData.anisoLevel = int.Parse(anisoValue);
			metaData.TryGetValue("filterMode", out string filterMode);
			if (!string.IsNullOrEmpty(filterMode) && int.Parse(filterMode) > -1) imageData.filterMode = (FilterMode)int.Parse(filterMode);
			metaData.TryGetValue("wrapMode", out string wrapMode);
			if (!string.IsNullOrEmpty(wrapMode) && int.Parse(wrapMode) > -1) imageData.wrapMode = (TextureWrapMode)int.Parse(wrapMode);
			// Todo -  the other 100 useless params
		}

		// make singleimage atlas
		FAtlas fatlas = new FAtlas(atlasName, imageData, FAtlasManager._nextAtlasIndex, false);

		if (slicerData == null) // was actually singleimage
		{
			// Done
			if (Futile.atlasManager.DoesContainAtlas(atlasName))
			{
				__logger.LogInfo("Single-image atlas '" + atlasName + "' being replaced.");
				Futile.atlasManager.ActuallyUnloadAtlasOrImage(atlasName); // Unload previous version if present
			}
			if (Futile.atlasManager._allElementsByName.Remove(atlasName)) __logger.LogInfo("Element '" + atlasName + "' being replaced with new one from atlas " + atlasName);
			FAtlasManager._nextAtlasIndex++; // is this guy even used
			Futile.atlasManager.AddAtlas(fatlas); // Simple
			return fatlas;
		}

		// convert to full atlas
		fatlas._elements.Clear();
		fatlas._elementsByName.Clear();
		fatlas._isSingleImage = false;


		//ctrl c
		//ctrl v

		Dictionary<string, object> dictionary2 = (Dictionary<string, object>)slicerData["frames"];
		float resourceScaleInverse = Futile.resourceScaleInverse;
		int num = 0;
		foreach (KeyValuePair<string, object> keyValuePair in dictionary2)
		{
			FAtlasElement fatlasElement = new FAtlasElement();
			fatlasElement.indexInAtlas = num++;
			string text = keyValuePair.Key;
			if (Futile.shouldRemoveAtlasElementFileExtensions)
			{
				int num2 = text.LastIndexOf(".");
				if (num2 >= 0)
				{
					text = text.Substring(0, num2);
				}
			}
			fatlasElement.name = text;
			IDictionary dictionary3 = (IDictionary)keyValuePair.Value;
			fatlasElement.isTrimmed = (bool)dictionary3["trimmed"];
			if ((bool)dictionary3["rotated"])
			{
				throw new NotSupportedException("Futile no longer supports TexturePacker's \"rotated\" flag. Please disable it when creating the " + fatlas._dataPath + " atlas.");
			}
			IDictionary dictionary4 = (IDictionary)dictionary3["frame"];
			float num3 = float.Parse(dictionary4["x"].ToString());
			float num4 = float.Parse(dictionary4["y"].ToString());
			float num5 = float.Parse(dictionary4["w"].ToString());
			float num6 = float.Parse(dictionary4["h"].ToString());
			Rect uvRect = new Rect(num3 / fatlas._textureSize.x, (fatlas._textureSize.y - num4 - num6) / fatlas._textureSize.y, num5 / fatlas._textureSize.x, num6 / fatlas._textureSize.y);
			fatlasElement.uvRect = uvRect;
			fatlasElement.uvTopLeft.Set(uvRect.xMin, uvRect.yMax);
			fatlasElement.uvTopRight.Set(uvRect.xMax, uvRect.yMax);
			fatlasElement.uvBottomRight.Set(uvRect.xMax, uvRect.yMin);
			fatlasElement.uvBottomLeft.Set(uvRect.xMin, uvRect.yMin);
			IDictionary dictionary5 = (IDictionary)dictionary3["sourceSize"];
			fatlasElement.sourcePixelSize.x = float.Parse(dictionary5["w"].ToString());
			fatlasElement.sourcePixelSize.y = float.Parse(dictionary5["h"].ToString());
			fatlasElement.sourceSize.x = fatlasElement.sourcePixelSize.x * resourceScaleInverse;
			fatlasElement.sourceSize.y = fatlasElement.sourcePixelSize.y * resourceScaleInverse;
			IDictionary dictionary6 = (IDictionary)dictionary3["spriteSourceSize"];
			float left = float.Parse(dictionary6["x"].ToString()) * resourceScaleInverse;
			float top = float.Parse(dictionary6["y"].ToString()) * resourceScaleInverse;
			float width = float.Parse(dictionary6["w"].ToString()) * resourceScaleInverse;
			float height = float.Parse(dictionary6["h"].ToString()) * resourceScaleInverse;
			fatlasElement.sourceRect = new Rect(left, top, width, height);
			fatlas._elements.Add(fatlasElement);
			fatlas._elementsByName.Add(fatlasElement.name, fatlasElement);
		}

		// This currently doesn't remove elements from old atlases, just removes elements from the manager.
		bool nameInUse = Futile.atlasManager.DoesContainAtlas(atlasName);
		if (!nameInUse)
		{
			// remove duplicated elements and add atlas
			foreach (FAtlasElement fae in fatlas._elements)
			{
				if (Futile.atlasManager._allElementsByName.Remove(fae.name)) __logger.LogInfo("Element '" + fae.name + "' being replaced with new one from atlas " + atlasName);
			}
			FAtlasManager._nextAtlasIndex++;
			Futile.atlasManager.AddAtlas(fatlas);
		}
		else
		{
			FAtlas other = Futile.atlasManager.GetAtlasWithName(atlasName);
			bool isFullReplacement = true;
			foreach (FAtlasElement fae in other.elements)
			{
				if (!fatlas._elementsByName.ContainsKey(fae.name)) isFullReplacement = false;
			}
			if (isFullReplacement)
			{
				// Done, we're good, unload the old and load the new
				__logger.LogInfo("Atlas '" + atlasName + "' being fully replaced with custom one");
				Futile.atlasManager.ActuallyUnloadAtlasOrImage(atlasName); // Unload previous version if present
				FAtlasManager._nextAtlasIndex++;
				Futile.atlasManager.AddAtlas(fatlas); // Simple
			}
			else
			{
				// uuuugh
				// partially unload the old
				foreach (FAtlasElement fae in fatlas._elements)
				{
					if (Futile.atlasManager._allElementsByName.Remove(fae.name)) __logger.LogInfo("Element '" + fae.name + "' being replaced with new one from atlas " + atlasName);
				}
				// load the new with a salted name
				do
				{
					atlasName += UnityEngine.Random.Range(0, 9);
				}
				while (Futile.atlasManager.DoesContainAtlas(atlasName));
				fatlas._name = atlasName;
				FAtlasManager._nextAtlasIndex++;
				Futile.atlasManager.AddAtlas(fatlas); // Finally
			}
		}
		return fatlas;
	}
	#endregion ATLASES
	public static void Disable()
	{
		// On.RainWorld.OnModsInit -= LoadResources;
	}
}
