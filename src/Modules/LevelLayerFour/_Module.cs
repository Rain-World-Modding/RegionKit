namespace RegionKit.Modules.LevelLayerFour;

using System.Reflection;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using static Pom.Pom.ManagedFieldWithPanel;
using static RegionKit.Modules.Objects._Module;

[RegionKitModule(nameof(onEnable), nameof(onDisable), moduleName: "Level Layer Four")]
public class _Module
{
	static bool loaded = false;

	static AssetBundle llfShadersBundle;
	static Shader llfShader;

	internal static void onEnable()
	{
		On.RainWorld.OnModsInit += RainWorld_OnModsInit;
	}

	internal static void onDisable()
	{
		On.RainWorld.OnModsInit -= RainWorld_OnModsInit;
	}

	private static void RainWorld_OnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
	{
		orig(self);
		if (!loaded)
		{
			llfShadersBundle = AssetBundle.LoadFromFile(AssetManager.ResolveFilePath("assets/regionkit/llfshaders"));
			llfShader = llfShadersBundle.LoadAsset<Shader>("Assets/BackgroundLevelColor.shader");
			self.Shaders["LLFLevelColor"] = FShader.CreateShader("LLFLevelColor", llfShader);
			loaded = true;

			List<ManagedField> settingsFields = new List<ManagedField> {
				new FloatField("fogFalloff", 1.0f, 10.0f, 1.0f, 0.1f, ControlType.slider, "Fog Falloff"),
				new IntegerField("fogMax", 0, 30, 30, ControlType.slider, "Fog Max Depth")
			};

			RegisterFullyManagedObjectType(settingsFields.ToArray(), typeof(LevelLayerFourObj), "LevelLayerFour", OBJECTS_POM_CATEGORY);
		}
	}
	
    public class LevelLayerFourObj : UpdatableAndDeletable, IDrawable {

        public PlacedObject placedObject;

        public bool dirty;

        private int camNum;

        private float fogFaloff = 0.1f;
        private int fogMax = 30;

        public LevelLayerFourObj(PlacedObject placedObject) {
            this.placedObject = placedObject;
            this.dirty = true;
        }

        public void LoadFile(string roomName, int camNum) {
            if (Futile.atlasManager.GetAtlasWithName($"llf_bkg_{roomName}_{camNum}") != null) {
                return;
            }

            Texture2D texture = new(1, 1, TextureFormat.ARGB32, false);

            string roomFile = WorldLoader.FindRoomFile(roomName, true, "_" + camNum.ToString() + "_llf.png");

            AssetManager.SafeWWWLoadTexture(ref texture, roomFile, true, true);

            HeavyTexturesCache.LoadAndCacheAtlasFromTexture($"llf_bkg_{roomName}_{camNum}", texture, false);
        }

        public override void Update(bool eu) {
            base.Update(eu);
            fogFaloff = ((ManagedData)this.placedObject.data).GetValue<float>("fogFalloff");
            fogMax = ((ManagedData)this.placedObject.data).GetValue<int>("fogMax");
        }

        public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner) {
            sLeaser.sprites[0].RemoveFromContainer();
            rCam.ReturnFContainer("Foreground").AddChild(sLeaser.sprites[0]);
        }

        public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette) {

        }

        public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos) {
            if (this.dirty || this.camNum != rCam.currentCameraPosition) {
                this.camNum = rCam.currentCameraPosition;

                LoadFile(this.room.abstractRoom.name, this.camNum + 1);
                sLeaser.sprites[0].SetElementByName($"llf_bkg_{this.room.abstractRoom.name}_{this.camNum + 1}");

                this.dirty = false;
            }

            sLeaser.sprites[0].x = this.room.cameraPositions[rCam.currentCameraPosition].x - camPos.x;
            sLeaser.sprites[0].y = this.room.cameraPositions[rCam.currentCameraPosition].y - camPos.y;

            Shader.SetGlobalFloat("llfFogFalloff", fogFaloff);
            Shader.SetGlobalFloat("llfFogMax", fogMax);

            if (base.slatedForDeletetion || this.room != rCam.room) {
                sLeaser.CleanSpritesAndRemove();
            }

        }

        public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam) {
            sLeaser.sprites = new FSprite[1];
            this.camNum = rCam.currentCameraPosition;

            LoadFile(this.room.abstractRoom.name, this.camNum + 1);
            sLeaser.sprites[0] = new($"llf_bkg_{this.room.abstractRoom.name}_{this.camNum + 1}", true) {
                shader = rCam.game.rainWorld.Shaders["LLFLevelColor"],
                anchorX = 0f,
                anchorY = 0f
            };

            this.AddToContainer(sLeaser, rCam, null);

            this.dirty = true;
        }

    }

}

