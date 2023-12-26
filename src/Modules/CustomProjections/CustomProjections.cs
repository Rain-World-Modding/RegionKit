using System;
using System.IO;
using System.Runtime.CompilerServices;
using BepInEx;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using OverseerHolograms;

namespace RegionKit.Modules.CustomProjections;

internal static class CustomProjections
{
	private static ConditionalWeakTable<IOwnAHoloImage, StrongBox<string>> _RNDName = new();

	public static StrongBox<string> RNDName(this IOwnAHoloImage p) => _RNDName.GetValue(p, _ => new("RND_PROJ"));

	public static Dictionary<string, List<OverseerImage.ImageID>> Registry = new();

	public static void BuildRegistry()
	{
		Registry = new();
		foreach (string path in AssetManager.ListDirectory("Projections"))
		{
			string filename = Path.GetFileNameWithoutExtension(path);
			string txtPath = AssetManager.ResolveFilePath(Path.Combine("Projections", filename + ".txt"));
			if (path.EndsWith(".png") && File.Exists(txtPath))
			{
				foreach (string line in File.ReadAllLines(txtPath))
				{
					if (!line.IsNullOrWhiteSpace())
					{
						if (!Registry.ContainsKey(filename))
						{ Registry[filename] = new(); }

						var r = new OverseerImage.ImageID(line.Trim(), true);
						Registry[filename].Add(r);
					}
				}
			}
		}
	}

	public static void ClearRegistry()
	{
		foreach (List<OverseerImage.ImageID> list in Registry.Values)
		{
			foreach (OverseerImage.ImageID id in list)
			{
				if (id.index >= 25) id.Unregister();
			}
		}
		Registry = new();
	}

	public static void Apply()
	{
		BuildRegistry();

		IL.OverseerHolograms.OverseerImage.ctor += OverseerImage_ctor;

		On.OverseerHolograms.OverseerImage.HoloImage.InitiateSprites += HoloImage_InitiateSprites;
		On.OverseerHolograms.OverseerImage.HoloImage.ctor += HoloImage_ctor;
		On.OverseerHolograms.OverseerImage.HoloImage.DrawSprites += HoloImage_DrawSprites;
	}

	public static void Undo()
	{
		ClearRegistry();

		IL.OverseerHolograms.OverseerImage.ctor -= OverseerImage_ctor;

		On.OverseerHolograms.OverseerImage.HoloImage.InitiateSprites -= HoloImage_InitiateSprites;
		On.OverseerHolograms.OverseerImage.HoloImage.ctor -= HoloImage_ctor;
		On.OverseerHolograms.OverseerImage.HoloImage.DrawSprites -= HoloImage_DrawSprites;
	}

	private static void HoloImage_DrawSprites(On.OverseerHolograms.OverseerImage.HoloImage.orig_DrawSprites orig, OverseerImage.HoloImage self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos, Vector2 partPos, Vector2 headPos, float useFade, float popOut, Color useColor)
	{
		orig(self, sLeaser, rCam, timeStacker, camPos, partPos, headPos, useFade, popOut, useColor);

		if (self.isAdvertisement) return;

		if (self.showRandomFlickerImage)
		{
			if (self.imageOwner.RNDName().Value != "RND_PROJ" && Futile.atlasManager.DoesContainElementWithName(self.imageOwner.RNDName().Value))
			{ sLeaser.sprites[self.firstSprite].element = Futile.atlasManager.GetElementWithName(self.imageOwner.RNDName().Value); }
		}

		else if (self.imageOwner.CurrImage.Index >= 25)
		{
			foreach (string image in Registry.Keys)
			{
				if (!Registry[image].Contains(self.imageOwner.CurrImage)) continue;

				if (Futile.atlasManager.DoesContainElementWithName(image))
				{ sLeaser.sprites[self.firstSprite].element = Futile.atlasManager.GetElementWithName(image); }

				if (Registry[image].IndexOf(self.imageOwner.CurrImage) >= 0)
				{
					var color = sLeaser.sprites[self.firstSprite].color;
					sLeaser.sprites[self.firstSprite].color = new(color.r, color.g, Registry[image].IndexOf(self.imageOwner.CurrImage) / 25f);
				}
				break;
			}
		}
	}

	private static void HoloImage_InitiateSprites(On.OverseerHolograms.OverseerImage.HoloImage.orig_InitiateSprites orig, OverseerImage.HoloImage self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
	{
		orig(self, sLeaser, rCam);
		if (self.imageOwner.RNDName().Value != "RND_PROJ" && Futile.atlasManager.DoesContainElementWithName(self.imageOwner.RNDName().Value))
		{
			sLeaser.sprites[self.firstSprite].element = Futile.atlasManager.GetElementWithName(self.imageOwner.RNDName().Value);
		}
	}

	private static void HoloImage_ctor(On.OverseerHolograms.OverseerImage.HoloImage.orig_ctor orig, OverseerImage.HoloImage self, OverseerHologram hologram, int firstSprite, IOwnAHoloImage imageOwner)
	{
		orig(self, hologram, firstSprite, imageOwner);
		List<string> processedImages = new();
		//why are these registered here in vanilla? idk but I'll copy the same place
		foreach (string image in Registry.Keys)
		{
			if (processedImages.Contains(image)) continue;
			self.LoadFile(image);
			processedImages.Add(image);
		}

		if (imageOwner.RNDName().Value != "RND_PROJ")
		{ self.LoadFile(imageOwner.RNDName().Value); }
	}

	private static void OverseerImage_ctor(ILContext il)
	{
		var c = new ILCursor(il);

			if (c.TryGotoNext(moveType: MoveType.AfterLabel,
			i => i.MatchLdarg(0),
			i => i.MatchLdarg(0),
			i => i.MatchLdarg(0),
			i => i.MatchLdfld<OverseerHologram>(nameof(OverseerHologram.totalSprites)),
			i => i.MatchLdarg(0),
			i => i.MatchNewobj<OverseerImage.HoloImage>()
			))
			{
				c.Emit(OpCodes.Ldarg_0);
				c.Emit(OpCodes.Ldloc_1);
				c.EmitDelegate((OverseerImage self, string roomName) => 
				{
					string fileName = AssetManager.ResolveFilePath(Path.Combine("Projections", roomName + "_proj.txt"));
					if (File.Exists(fileName))
					{
						ProjectionData data = ProcessProjectionFile(File.ReadAllLines(fileName));
						if (data.images.Count > 0)
						{ data.CopyData(self); }

						else
						{ LogMessage("OverseerProjection has no images!"); }
					}
				
				});
			}
			else { Debug.LogError("Something went terribly wrong"); }


	}

	public struct ProjectionData
	{
		public int timeOnEachImage = 25;
		public int showTime = 150;
		public List<OverseerImage.ImageID> images = new();
		public string RND_Name = "RND_PROJ";

		public ProjectionData() { }

		public void CopyData(OverseerImage self)
		{
			self.timeOnEachImage = timeOnEachImage;
			self.showTime = showTime;
			self.images = images;
			self.RNDName().Value = RND_Name;
		}
	}

	public static ProjectionData ProcessProjectionFile(string[] lines)
	{
		ProjectionData data = new();

		foreach (string text in lines)
		{
			if (new OverseerImage.ImageID(text).index != -1)
			{ data.images.Add(new OverseerImage.ImageID(text)); }

			string[] array = text.Split(':');

			if (array.Length >= 2)
			{
				switch (array[0].ToLower().Trim())
				{
				case "timeoneachimage":
					if (int.TryParse(array[1].Trim(), out var num))
						data.timeOnEachImage = num;
					break;

				case "showtime":
					if (int.TryParse(array[1].Trim(), out var num2))
						data.showTime = num2;
					break;

				case "rnd_image":
					data.RND_Name = array[1].Trim();
					break;
				};
			}
		}
		return data;
	}
}
