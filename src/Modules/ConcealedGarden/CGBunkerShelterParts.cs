using RWCustom;
using System;
using System.IO;
using UnityEngine;
using RegionKit.Modules.ShelterBehaviors;

namespace RegionKit.Modules.ConcealedGarden;

internal static class CGBunkerShelterParts
{
	//NEEDS SPRITES TO BE LOADED BEFORE IT CAN BE ENABLED
	public class CGBunkerShelterFlapData : ManagedData
	{
		private static ManagedField[] customfields = new ManagedField[]
			{
					new IntVector2Field("handle", new RWCustom.IntVector2(2,4), IntVector2Field.IntVectorReprType.rect),
			};
#pragma warning disable 0649
		[FloatField("dpt", 1, 30, 8, 1, displayName: "Depth")]
		public float depth;
		[FloatField("ofx", -10, 10, 0, 1, displayName: "Offset X")]
		public float offsetX;
		[FloatField("ofy", -10, 10, 0, 1, displayName: "Offset Y")]
		public float offsetY;
		[BackedByField("handle")]
		public IntVector2 handle;
#pragma warning restore 0649

		public CGBunkerShelterFlapData(PlacedObject owner) : base(owner, customfields) { }
	}

	private static void TryLoad()
	{
		//CustomAtlasLoader.ReadAndLoadCustomAtlas("cgbkr_parts", CustomRegions.Mod.CustomWorldMod.resourcePath + CustomRegions.Mod.CustomWorldMod.activatedPacks["Concealed Garden"] + Path.DirectorySeparatorChar + "Assets" + Path.DirectorySeparatorChar + "Futile" + Path.DirectorySeparatorChar + "Resources" + Path.DirectorySeparatorChar + "Atlases");
	}

	public class CGBunkerShelterFlap : CosmeticSprite, IReactToShelterEvents
	{
		private float closedFactor;
		private float lastClosedFactor;
		private float closeSpeed;
		private PlacedObject pObj;
		IntRect rect;
		private int height;
		private int width;
		private int area;
		private float heightFloat;
		private float widthFloat;
		private StaticSoundLoop backgroundWorkingLoop;

		CGBunkerShelterFlapData data => (CGBunkerShelterFlapData)pObj.data;

		public CGBunkerShelterFlap(Room room, PlacedObject pObj)
		{
			this.room = room;
			this.pObj = pObj;

			var origin = new IntVector2(Mathf.FloorToInt(pObj.pos.x / 20f), Mathf.FloorToInt(pObj.pos.y / 20f));
			rect = IntRect.MakeFromIntVector2(origin);
			rect.ExpandToInclude(origin + data.handle);
			rect.right++;
			rect.top++; // match visuals
			this.height = rect.Height;
			this.width = rect.Width;
			this.area = rect.Area;
			this.heightFloat = 20f * height;
			this.widthFloat = 20f * width;

			this.backgroundWorkingLoop = new StaticSoundLoop(SoundID.Gate_Electric_Screw_Turning_LOOP,
				new Vector2(pObj.pos.x + widthFloat / 2f, pObj.pos.y), room, 0f, Mathf.Clamp(Mathf.Pow(0.8f - (widthFloat * height / (room.PixelWidth * room.PixelHeight)), 3f), 0.5f, 1f));
		}

		public void ShelterEvent(float newFactor, float closeSpeed)
		{
			this.closedFactor = newFactor;
			this.lastClosedFactor = newFactor;
			this.closeSpeed = closeSpeed;
		}

		public override void Update(bool eu)
		{
			base.Update(eu);
			this.backgroundWorkingLoop.volume = Mathf.Lerp(this.backgroundWorkingLoop.volume, (closedFactor != 1f && closedFactor != 0 && closeSpeed != 0f) ? 1f : 0f, 0.085f);
			backgroundWorkingLoop.Update();
			lastClosedFactor = closedFactor;
			closedFactor = Mathf.Clamp01(closedFactor + closeSpeed);
		}

		private int Cog(int i) => i == 0 ? 0 : 1;
		private int LidBL(int i) => 2 + i;
		private int LidBR(int i) => 2 + height + i;
		private int LidB(int i, int j) => 2 + 2 * height + i * width + j;

		public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
		{
			base.InitiateSprites(sLeaser, rCam);
			sLeaser.sprites = new FSprite[rect.Height * 2 + rect.Area + 2];
			FShader shader = this.room.game.rainWorld.Shaders["ColoredSprite2"];
			for (int i = 0; i < 2; i++)
			{
				sLeaser.sprites[Cog(i)] = new FSprite("cgbkr_cog", true) { shader = shader };
			}
			for (int i = 0; i < height; i++)
			{
				sLeaser.sprites[LidBL(i)] = new FSprite("cgbkr_lidB_left", true) { shader = shader };
				sLeaser.sprites[LidBR(i)] = new FSprite("cgbkr_lidB_right", true) { shader = shader };
				for (int j = 0; j < width; j++)
				{
					sLeaser.sprites[LidB(i, j)] = new FSprite("cgbkr_lidB_mid", true) { shader = shader };
				}
			}

			this.AddToContainer(sLeaser, rCam, rCam.ReturnFContainer("Foreground"));
			//Debug.LogError("Flaps initiated, with " + sLeaser.sprites.Length + " sprites");
		}

		public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
		{
			base.DrawSprites(sLeaser, rCam, timeStacker, camPos);
			Vector2 start = new Vector2(rect.left * 20 - camPos.x + data.offsetX, rect.bottom * 20 - camPos.y + data.offsetY);

			//Debug.Log("rendering flaps at " + start);
			float factor = Mathf.Lerp(lastClosedFactor, closedFactor, timeStacker);
			float easedFactor =
				  0.15f * Mathf.Pow(Mathf.InverseLerp(0, 0.2f, factor), 2f)
				+ 0.7f * Mathf.InverseLerp(0.2f, 0.8f, factor)
				+ 0.15f * Mathf.Pow(Mathf.InverseLerp(0.8f, 1, factor), 0.5f);
			float depth = data.GetValue<float>("dpt") / 30f;
			//Debug.Log("rendering flaps at " + depth);
			float depthStep = 1f / 30f;
			float heightStep = 20f - 1f / height;

			for (int i = 0; i < 2; i++)
			{
				sLeaser.sprites[Cog(i)].x = start.x + (i == 0 ? -4f : widthFloat + 4f);
				sLeaser.sprites[Cog(i)].y = start.y + 8;
				sLeaser.sprites[Cog(i)].alpha = depth + depthStep;
				sLeaser.sprites[Cog(i)].rotation = easedFactor * 1440f * (i == 0 ? -1f : 1f);
			}
			float animatedLidY = 7 * Mathf.InverseLerp(0, 0.75f, Mathf.Pow(easedFactor, 0.69f)) + 3 * Mathf.InverseLerp(0.75f, 1f, easedFactor);
			float animatedLidZ = 3 * depthStep * Mathf.InverseLerp(0.6f, 0.9f, easedFactor);
			for (int i = 0; i < height; i++)
			{
				sLeaser.sprites[LidBL(i)].x = start.x - 5f;
				sLeaser.sprites[LidBL(i)].y = start.y + 5f + i * heightStep + animatedLidY;
				sLeaser.sprites[LidBL(i)].alpha = depth + animatedLidZ;
				sLeaser.sprites[LidBR(i)].x = start.x + widthFloat + 5f;
				sLeaser.sprites[LidBR(i)].y = start.y + 5f + i * heightStep + animatedLidY;
				sLeaser.sprites[LidBR(i)].alpha = depth + animatedLidZ;
				for (int j = 0; j < width; j++)
				{
					sLeaser.sprites[LidB(i, j)].x = start.x + 10f + j * 20f;
					sLeaser.sprites[LidB(i, j)].y = start.y + 5f + i * heightStep + animatedLidY;
					sLeaser.sprites[LidB(i, j)].alpha = depth + animatedLidZ;
				}
			}
		}
	}
}
