using System.Text.RegularExpressions;

namespace RegionKit.Modules.MoonStuff
{
	[RegionKitModule(nameof(Enable), nameof(Disable), nameof(Setup), moduleName: "Moon Stuff")]
	internal static class _Module
	{
		public static void Setup()
		{
			RegisterManagedObject(new CrystalType());
			RegisterManagedObject(new SandfallType());
			RegisterManagedObject(new LightSourceFlickerType());
			//RegisterManagedObject(new ColoredOESphereType());
			RegisterManagedObject(new WarmSpotType());
			RegisterManagedObject(new ConveyorBeltType());

			RegisterFullyManagedObjectType(null!, typeof(ConveyorBeltCover), _Enums.ConveyorBeltCover, Objects._Enums.GameplayCategory);

			LoadShaders();
		}

		public static void Enable()
		{
			_CommonHooks.GeneralUnrecognizedRegionParamProcessor += MoonRegionParams;
			On.MoreSlugcats.OEsphere.AddToContainer += OESphereFix;
			On.SoundLoader.LoadSounds += SoundLoader_LoadSounds;

			LightSourceFlickerHooks.Apply();
		}

		public static void Disable()
		{
			_CommonHooks.GeneralUnrecognizedRegionParamProcessor -= MoonRegionParams;
			On.MoreSlugcats.OEsphere.AddToContainer -= OESphereFix;
			On.SoundLoader.LoadSounds -= SoundLoader_LoadSounds;

			LightSourceFlickerHooks.Undo();
		}

		private static void MoonRegionParams(Region region, string key, string value)
		{
			switch (key.ToLowerInvariant())
			{
				case "defaultcrystalcolor":
				{
					string[] vals = Regex.Split(value.Trim(), ",");

					HSLColor col = new HSLColor(
						float.TryParse(vals[0], out float h) ? h : 0.87f,
						float.TryParse(vals[1], out float s) ? s : 0.9f,
						float.TryParse(vals[2], out float l) ? l : 0.6f
						);

					region.MoonRegionData().CrystalColor = col;
					break;
				}
				case "defaultcolouredoespherecolour" or "defaultcoloredoespherecolor":
				{
					region.MoonRegionData().OESphereHue = float.TryParse(value, out float h) ? h : 0.06f;
					break;
				}
				case "removeraintimer":
				{
					region.MoonRegionData().hideTimer = bool.TryParse(value, out bool t) && t;
					break;
				}
			}
		}

		public static void OESphereFix(On.MoreSlugcats.OEsphere.orig_AddToContainer orig, MoreSlugcats.OEsphere self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
		{
			rCam.ReturnFContainer("GrabShaders").AddChild(sLeaser.sprites[0]);
			rCam.ReturnFContainer("GrabShaders").AddChild(sLeaser.sprites[1]);
			rCam.ReturnFContainer("Foreground").AddChild(sLeaser.sprites[2]);
			sLeaser.sprites[1].MoveInFrontOfOtherNode(sLeaser.sprites[0]);
			sLeaser.sprites[0].MoveToBack();
		}

		private static void SoundLoader_LoadSounds(On.SoundLoader.orig_LoadSounds orig, SoundLoader self)
		{
			_ = _Enums.Sandfall_LOOP;
			orig(self);
		}

		private static void LoadShaders()
		{
			AssetBundle assetBundle = AssetBundle.LoadFromFile(AssetManager.ResolveFilePath("assets/regionkit/rk_moonstuff"));
			Custom.rainWorld.Shaders.Add("ColoredOESphereBase", FShader.CreateShader("ColoredOESphereBase", assetBundle.LoadAsset<Shader>("assets/shaders/ColoredOESphereBase.shader")));
			Custom.rainWorld.Shaders.Add("ColoredOESphereLight", FShader.CreateShader("ColoredOESphereLight", assetBundle.LoadAsset<Shader>("assets/shaders/ColoredOESphereLight.shader")));
			Custom.rainWorld.Shaders.Add("SandFall", FShader.CreateShader("SandFall", assetBundle.LoadAsset<Shader>("assets/shaders/SandFall.shader")));
		}
	}
}
