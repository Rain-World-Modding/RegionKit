using System.Runtime.CompilerServices;

namespace RegionKit.Modules.Objects
{
	public class ColoredSSFuses : SuperStructureFuses
	{
		public ColoredSSFusesData Data => (placedObject.data as ColoredSSFusesData)!;

		public Color ActiveColor => Data.ActiveColor;
		public Color BrokenColor => Data.BrokenColor;

		public float Depth => 1f - (Data.Depth + 0.5f) / 30f;

		public ColoredSSFuses(PlacedObject placedObject, Room room) : base(placedObject, (placedObject.data as ColoredSSFusesData)!.Rect, room)
		{
		}

		public override void Update(bool eu)
		{
			broken = Data.BrokenAmt;
			rect = Data.Rect;
			base.Update(eu);
		}
		
		public void DrawSpritesExtra(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker)
		{
			if (culled || debugMode > 0 || slatedForDeletetion || room != rCam.room) return;

			foreach (var sprite in sLeaser.sprites)
			{
				sprite.alpha = Depth;
			}
		}

		/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

		private class ColorData { public Color? activeColor; public Color? brokenColor; }

		private static readonly ConditionalWeakTable<Region, ColorData> ssFuseColorTable = new();

		internal static void Apply()
		{
			On.SuperStructureFuses.DrawSprites += SuperStructureFuses_DrawSprites;
			_CommonHooks.SpecificUnrecognizedRegionParamProcessor["SSFuseActiveColor"] = ProcessActiveColor;
			_CommonHooks.SpecificUnrecognizedRegionParamProcessor["SSFuseBrokenColor"] = ProcessBrokenColor;
		}

		internal static void Undo()
		{
			On.SuperStructureFuses.DrawSprites -= SuperStructureFuses_DrawSprites;
			_CommonHooks.SpecificUnrecognizedRegionParamProcessor.Remove("SSFuseActiveColor");
			_CommonHooks.SpecificUnrecognizedRegionParamProcessor.Remove("SSFuseBrokenColor");
		}

		private static void ProcessActiveColor(Region region, string key, string value) => ssFuseColorTable.GetValue(region, p => new()).activeColor = Custom.hexToColor(value);
		private static void ProcessBrokenColor(Region region, string key, string value) => ssFuseColorTable.GetValue(region, p => new()).brokenColor = Custom.hexToColor(value);

		private static void SuperStructureFuses_DrawSprites(On.SuperStructureFuses.orig_DrawSprites orig, SuperStructureFuses self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
		{
			orig(self, sLeaser, rCam, timeStacker, camPos);

			if (self.culled || self.debugMode > 0 || self.slatedForDeletetion || self.room != rCam.room || self.room?.world?.region is null) return;

			Color activeColor = Color.blue, brokenColor = Color.red;
			if (self is ColoredSSFuses coloredSSFuses)
			{
				activeColor = coloredSSFuses.ActiveColor;
				brokenColor = coloredSSFuses.BrokenColor;
			}
			else if (ssFuseColorTable.TryGetValue(self.room.world.region, out var data))
			{
				if (data.activeColor != null) activeColor = data.activeColor.Value;
				if (data.brokenColor != null) brokenColor = data.brokenColor.Value;
			}
			else
			{
				return;
			}

			float num = Mathf.Lerp(self.lastLightness, self.lightness, timeStacker);
			int sp = 0;
			for (int j = 0; j < self.lights.GetLength(0); j++)
			{
				for (int k = 0; k < self.lights.GetLength(1); k++)
				{
					if (self.smoothedMalfunction > 0.2f && self.lights[j, k, 0] > 0.5f && Custom.InsideRect(new IntVector2(j, k), self.malfunctionRect))
					{
						sLeaser.sprites[sp].color = Color.Lerp(Color.black, brokenColor, Mathf.Lerp(1f, 0.25f, Mathf.Pow(self.smoothedMalfunction, 0.2f)) * self.powerFlicker);
					}
					else if (self.lights[j, k, 3] == 5f)
					{
						sLeaser.sprites[sp].color = Color.Lerp(Color.black, brokenColor, (((int)self.lights[j, k, 4] % 8 > 3) ? 1f : 0f) * Mathf.Lerp(1f, 0.25f, Mathf.Pow(self.smoothedMalfunction, 0.2f)) * self.powerFlicker);
					}
					else
					{
						sLeaser.sprites[sp].color = Color.Lerp(Color.black, activeColor, Mathf.Pow(Mathf.Max(0f, Mathf.Lerp(self.lights[j, k, 1], self.lights[j, k, 0], timeStacker)), 2.5f - num) * Mathf.Lerp(0.3f, 1f, num) * Mathf.Lerp(1f, 0.5f, Mathf.Pow(self.smoothedMalfunction, 0.2f)) * self.powerFlicker);
					}
					sp++;
				}
			}

			(self as ColoredSSFuses)?.DrawSpritesExtra(sLeaser, rCam, timeStacker);
		}
	}
}
