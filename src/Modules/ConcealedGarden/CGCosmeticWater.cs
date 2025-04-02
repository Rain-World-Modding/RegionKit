using System.Reflection;
using Mono.Cecil.Cil;
using MonoMod.Cil;

namespace RegionKit.Modules.ConcealedGarden;

#warning this does not work
class CGCosmeticWater : UpdatableAndDeletable, IDrawable
{
	private class CGCosmeticWaterSurface : Water.DefaultSurface
	{
		private readonly float top;
		private readonly (float min, float max) range;

		public CGCosmeticWaterSurface(Water water, Rect bounds) : base(water)
		{
			// Damn you Water.MainSurface...
			totalPoints = (int)(bounds.width / water.triangleWidth) + 1;
			points = new Water.SurfacePoint[totalPoints, 2];
			for (int i = 0; i < totalPoints; i++)
			{
				for (int j = 0; j < 2; j++)
				{
					points[i, j] = new Water.SurfacePoint(new Vector2(bounds.xMin + ((float)i + ((j == 0) ? 0f : 0.5f)) * water.triangleWidth, water.originalWaterLevel));
				}
			}

			top = bounds.yMax;
			range = (bounds.xMin, bounds.xMax);
			this.bounds = bounds;
			waterCutoffs.Clear();
		}

		public override float TargetWaterLevel(float horizontalPosition)
		{
			return horizontalPosition >= range.min && horizontalPosition <= range.max ? top : -10f;
		}
	}

	private readonly PlacedObject pObj;
	private readonly Water water;

	CGCosmeticWaterData data => (CGCosmeticWaterData)pObj.data;

	public CGCosmeticWater(Room room, PlacedObject pObj)
	{
		this.room = room;
		this.pObj = pObj;

		FloatRect rect = data.rect;

		water = new Water(room, Mathf.FloorToInt(rect.top / 20f));
		// room.drawableObjects.Add(this.water);
		water.cosmeticLowerBorder = Mathf.FloorToInt(rect.bottom);

		// Water ctor stuff to be adjusted
		water.surfaces = [new CGCosmeticWaterSurface(water, Rect.MinMaxRect(rect.left, rect.bottom, rect.right, rect.top))];
		water.pointsToRender = IntClamp((int)((room.game.rainWorld.options.ScreenSize.x + 60f) / water.triangleWidth) + 2, 0, water.surfaces[0].totalPoints);
		water.waterSounds.rect = rect;
		water.waterSounds.Volume = Mathf.Pow(Mathf.Clamp01((rect.right - rect.left) / room.game.rainWorld.options.ScreenSize.x), 0.7f) * (room.water ? 0.5f : 1f);
		if (room.waterObject != null) water.airPockets = room.waterObject.airPockets; // share a reference
	}

	public override void Update(bool eu)
	{
		base.Update(eu);
		water.Update();
	}

	public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
	{
		water.InitiateSprites(sLeaser, rCam);
	}

	public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
	{
		water.DrawSprites(sLeaser, rCam, timeStacker, camPos);
	}

	public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
	{
		water.ApplyPalette(sLeaser, rCam, palette);
	}

	public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
	{
		water.AddToContainer(sLeaser, rCam, newContatiner);
	}

	public class CGCosmeticWaterData : ManagedData
	{
		public FloatRect rect
		{
			get
			{
				return new FloatRect(
					Mathf.Min(owner.pos.x, owner.pos.x + handlePos.x),
					Mathf.Min(owner.pos.y, owner.pos.y + handlePos.y),
					Mathf.Max(owner.pos.x, owner.pos.x + handlePos.x),
					Mathf.Max(owner.pos.y, owner.pos.y + handlePos.y));
			}
		}
		[BackedByField("handle")]
		public Vector2 handlePos;
		public CGCosmeticWaterData(PlacedObject owner) : base(owner, [new Vector2Field("handle", new Vector2(100,100), Vector2Field.VectorReprType.rect)])
		{
		}
	}

	public static class Hooks
	{
		public static void Apply()
		{
			IL.Water.DrawSprites += Water_DrawSprites;
		}

		public static void Undo()
		{
			IL.Water.DrawSprites -= Water_DrawSprites;
		}

		private static void Water_DrawSprites(ILContext il)
		{
			try
			{
				// remove 100px buffer
				var c = new ILCursor(il);
				for (int i = 0; i < 2; i++)
				{
					c.GotoNext(MoveType.After, x => x.MatchLdcR4(100f));
					c.Emit(OpCodes.Ldarg_0);
					c.EmitDelegate((float orig, Water self) => self.MainSurface is CGCosmeticWaterSurface ? 0f : orig);
				}

				// Find break point after edge filling
				for (int i = 0; i < 2; i++)
					c.GotoNext(x => x.MatchLdcR4(1400f));
				c.GotoNext(MoveType.After, x => x.MatchCallvirt<WaterTriangleMesh>(nameof(WaterTriangleMesh.MoveVertice)));
				// LogDebug(c);
				ILLabel brTo = c.MarkLabel();

				// Break around it if it's our thing
				for (int i = 0; i < 6; i++)
					c.GotoPrev(x => x.MatchIsinst<WaterTriangleMesh>());
				c.GotoPrev(MoveType.AfterLabel, x => x.MatchLdarg(1), x => x.MatchLdfld<RoomCamera.SpriteLeaser>(nameof(RoomCamera.SpriteLeaser.sprites)));
				// LogDebug(c);
				c.Emit(OpCodes.Ldarg_0);
				c.EmitDelegate((Water self) => self.MainSurface is CGCosmeticWaterSurface);
				c.Emit(OpCodes.Brtrue, brTo);

				// One more condition for the road
				c.GotoPrev(MoveType.After, x => x.MatchCallOrCallvirt(typeof(ModManager).GetProperty(nameof(ModManager.DLCShared), BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).GetGetMethod()));
				c.Emit(OpCodes.Ldarg_0);
				c.EmitDelegate((Water self) => self.MainSurface is not CGCosmeticWaterSurface);
				c.Emit(OpCodes.And);
			}
			catch (Exception e)
			{
				LogError("CGCosmeticWater Water.DrawSprites IL hook failed!");
				LogError(e);
			}
		}
	}
}
