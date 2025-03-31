using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using MonoMod.RuntimeDetour;
using RWCustom;
using UnityEngine;

namespace RegionKit.Modules.ConcealedGarden;

[Obsolete("DrySpot is in-game now.")]
internal class CGDrySpot : UpdatableAndDeletable, IDrawable
{
	internal static class Hooks
	{
		public static Hook? WaterfallStrikeHook = null;
		public static void Apply()
		{
			try
			{
				WaterfallStrikeHook = new Hook(typeof(WaterFall).GetProperty("strikeLevel", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).GetGetMethod(),
						WaterFall_StrikeLevel_Hook);
			}
			catch (Exception e) { LogError($"CGDrySpot waterfall hook failed!\n{e}"); }
			On.Room.AddObject += Room_AddObject;
			_CommonHooks.PostRoomLoad += _CommonHooks_PostRoomLoad;
		}

		public static void Undo()
		{
			WaterfallStrikeHook?.Undo();
			On.Room.AddObject -= Room_AddObject;
			_CommonHooks.PostRoomLoad -= _CommonHooks_PostRoomLoad;
		}
		public static float WaterFall_StrikeLevel_Hook(Func<WaterFall, float> orig, WaterFall self)
		{
			//detailed level apparently is only an MMF thing, otherwise it falls till the bottom of the level
			if (self.room.waterObject != null && ModManager.MMF && !(ModManager.MSC && self.room.waterInverted))
			{ return self.room.waterObject.DetailedWaterLevel(self.pos); }

			return orig(self);
		}

		private static void _CommonHooks_PostRoomLoad(Room self)
		{
			if (self.game == null)
			{
				foreach (PlacedObject pObj in self.roomSettings.placedObjects)
				{
					if (pObj.data is CGDrySpotData data)
					{
						UpdateRoomWaterTiles(data, self);
					}
				}
			}
		}

		private static void Room_AddObject(On.Room.orig_AddObject orig, Room self, UpdatableAndDeletable obj)
		{
			orig(self, obj);

			try
			{
				if (obj is not SplashWater.Splash) return;

				bool drySpots = false;
				for (int i = self.updateList.Count - 1; i >= 0; i--)
				{
					if (self.updateList[i] is CGDrySpot)
					{ drySpots = true; }

					else if (drySpots && self.updateList[i] is not CGDrySpot)
					{
						self.updateList.Remove(obj);
						self.updateList.Insert(i, obj); // reorder so all dryspots run first!
						break;
					}
				}
			}
			catch (Exception e) { LogError(e); } //is no big deal, would rather a stray particle than a crash
		}

	}

	private readonly PlacedObject _pObj;
	private bool _swappedDrawOrder;
	private RoomCamera.SpriteLeaser? _waterLeaser;

	private CGDrySpotData _Data => (_pObj.data as CGDrySpotData)!;

	public CGDrySpot(Room room, PlacedObject pObj)
	{
		this.room = room;
		this._pObj = pObj;
		UpdateRoomWaterTiles(_Data, room);
	}

	public override void Update(bool eu)
	{
		base.Update(eu);

		if (room.waterObject != null)
		{
			FloatRect ownrect = _Data.Rect;

			float dryHeight = ownrect.bottom + WaterFluxOffset();

			if (room.roomRain != null)
			{
				if(_Data.floodType == CGDrySpotData.FloodType.Offset)
				dryHeight += room.roomRain.FloodLevel - room.waterObject.originalWaterLevel;

				else if(_Data.floodType == CGDrySpotData.FloodType.Sync)
				dryHeight = NewFlood(room.roomRain, dryHeight);
			}

			bool inside = false;
			foreach (var surface in room.waterObject.surfaces)
			{
				for (int i = 0; i < surface.points.GetLength(0); i++)
				{
					Water.SurfacePoint pt = surface.points[i, 0];
					if (pt.defaultPos.x > ownrect.left && pt.defaultPos.x < ownrect.right)
					{
						if (!inside && i > 0)
						{
							// first in
							surface.points[i - 1, 0].defaultPos.x = ownrect.left - 1f;
							surface.points[i - 1, 1].defaultPos.x = ownrect.left - 1f;
							pt.defaultPos.x = ownrect.left + 1f;
							surface.points[i, 1].defaultPos.x = ownrect.left + 1f;

							surface.points[i - 1, 0].pos *= 0.1f;
							surface.points[i - 1, 1].pos *= 0.1f;
							pt.pos *= 0.1f;
							surface.points[i, 1].pos *= 0.1f;
						}
						inside = true;
						if (pt.defaultPos.y > dryHeight)
						{
							pt.defaultPos.y = dryHeight;
							surface.points[i, 1].defaultPos.y = dryHeight;
						}
					}
					else if (inside) // was inside already
					{
						// first out
						pt.defaultPos.x = ownrect.right + 1f;
						surface.points[i, 1].defaultPos.x = ownrect.right + 1f;
						surface.points[i - 1, 0].defaultPos.x = ownrect.right - 1f;
						surface.points[i - 1, 1].defaultPos.x = ownrect.right - 1f;

						pt.pos *= 0.1f;
						surface.points[i, 1].pos *= 0.1f;
						surface.points[i - 1, 0].pos *= 0.1f;
						surface.points[i - 1, 1].pos *= 0.1f;
						break;
					}
				}
			}
			for (int i = 0; i < room.waterObject.surfaces.GetLength(0); i++)
			{
			}
		}
		for (int i = room.updateList.Count - 1; i >= 0; i--)
		{
			if (room.updateList[i] is not CGDrySpot)
			{
				room.updateList.Remove(this);
				room.updateList.Add(this); // reorder so all dryspots run first!
				break;
			}
		}
	}

	public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
	{
		if (room.waterObject != null)
		{
			if (!this._swappedDrawOrder) // water updates first, so we can update after it :/
			{
				RoomCamera.SpriteLeaser? found = null;
				foreach (var item in rCam.spriteLeasers)
				{
					if (item.drawableObject == room.waterObject)
					{
						found = item;
					}
				}
				if (found != null)
				{
					rCam.spriteLeasers.Remove(found);
					rCam.spriteLeasers.Add(found);
					_swappedDrawOrder = true;
					this._waterLeaser = found;
				}
			}

			if (this._waterLeaser != null) // redraw water but good
			{
				foreach (var surface in room.waterObject.surfaces)
				{
					float y = -10f;
					if (room.waterObject.cosmeticLowerBorder > -1f)
					{
						y = room.waterObject.cosmeticLowerBorder - camPos.y;
					}
					int num = Custom.IntClamp(surface.PreviousPoint(camPos.x - 30f), 0, surface.points.GetLength(0) - 1);
					int num2 = Custom.IntClamp(num + room.waterObject.pointsToRender, 0, surface.points.GetLength(0) - 1);
					var waterTriangleMesh = (_waterLeaser.sprites[1] as WaterTriangleMesh)!;
					for (int i = num; i < num2; i++)
					{
						int num3 = (i - num) * 2;
						Vector2 vector = surface.points[i, 0].defaultPos + Vector2.Lerp(surface.points[i, 0].lastPos, surface.points[i, 0].pos, timeStacker) - camPos + new Vector2(0f, room.waterObject.cosmeticSurfaceDisplace);
						Vector2 vector2 = surface.points[i, 1].defaultPos + Vector2.Lerp(surface.points[i, 1].lastPos, surface.points[i, 1].pos, timeStacker) - camPos + new Vector2(0f, room.waterObject.cosmeticSurfaceDisplace);
						Vector2 vector3 = surface.points[i + 1, 0].defaultPos + Vector2.Lerp(surface.points[i + 1, 0].lastPos, surface.points[i + 1, 0].pos, timeStacker) - camPos + new Vector2(0f, room.waterObject.cosmeticSurfaceDisplace);
						Vector2 v = surface.points[i + 1, 1].defaultPos + Vector2.Lerp(surface.points[i + 1, 1].lastPos, surface.points[i + 1, 1].pos, timeStacker) - camPos + new Vector2(0f, room.waterObject.cosmeticSurfaceDisplace);
						vector = Custom.ApplyDepthOnVector(vector, new Vector2(rCam.sSize.x / 2f, rCam.sSize.y * 0.6666667f), -10f);
						vector2 = Custom.ApplyDepthOnVector(vector2, new Vector2(rCam.sSize.x / 2f, rCam.sSize.y * 0.6666667f), 30f);
						vector3 = Custom.ApplyDepthOnVector(vector3, new Vector2(rCam.sSize.x / 2f, rCam.sSize.y * 0.6666667f), -10f);
						v = Custom.ApplyDepthOnVector(v, new Vector2(rCam.sSize.x / 2f, rCam.sSize.y * 0.6666667f), 30f);
						if (i == num)
						{
							vector2.x -= 100f;
						}
						else if (i == num2 - 1)
						{
							vector2.x += 100f;
						}

						// goes straight down rather than at an angle

						waterTriangleMesh.MoveVertice(num3, new Vector2(vector.x, y));
						waterTriangleMesh.MoveVertice(num3 + 1, vector);
						waterTriangleMesh.MoveVertice(num3 + 2, new Vector2(vector3.x, y));
						waterTriangleMesh.MoveVertice(num3 + 3, vector3);
					}

					waterTriangleMesh.color = new Color(0f, 0f, 0f);
				}
			}
		}
	}

	public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam) { sLeaser.sprites = new FSprite[0]; _swappedDrawOrder = false; }
	public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette) { }
	public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner) { }


	#region dynamicLevel
	public static void UpdateRoomWaterTiles(CGDrySpotData data, Room room)
	{
		IntRect rect = new IntRect((int)Math.Floor(data.Rect.left / 20f), (int)Math.Floor(data.Rect.bottom / 20f), (int)Math.Ceiling(data.Rect.right / 20f), Mathf.Max(room.defaultWaterLevel + 2, (int)Math.Ceiling(data.Rect.top / 20f)));
		foreach (IntVector2 pos in rect.ReturnTiles())
		{
			if (room.IsPositionInsideBoundries(pos))
			{
				room.GetTile(pos).waterInt = pos.y == rect.bottom ? 2 : 0;
			}
		}
	}
	/// <summary>
	/// Get the WaterFluxOffset if settings say to
	/// </summary>
	public float WaterFluxOffset()
	{
		if (room.waterFlux == null || room.waterObject == null || !_Data.waterFlux) return 0f;
		return room.waterFlux.fluxWaterLevel - room.waterObject.originalWaterLevel;
	}

	/// <summary>
	/// <para>MSC syncs flood levels across the region. But vanilla flood level is just default water level + timeAfterRain</para>
	/// <para>this method brings up the water level of the DrySpot to meet the regular water level right as the rain hits</para>
	/// </summary>
	public float NonMSCPreFloodBoost(float baseLevel, float targetLevel, float timeUntilRain)
	{
		float preLength = targetLevel - baseLevel;
		return LerpMap(timeUntilRain, preLength, 0f, baseLevel, targetLevel);
	}

	public float NewFlood(RoomRain roomRain, float baseLevel)
	{
		if (roomRain.room?.waterObject == null)
		{ return -100f; }

		Room room = roomRain.room;

		if (roomRain.dangerType != RoomRain.DangerType.Flood && roomRain.dangerType != RoomRain.DangerType.FloodAndRain)
		{ return baseLevel; }

		//float baseLevel = room.waterFlux != null ? room.waterFlux.fluxWaterLevel : room.waterObject.originalWaterLevel;
		float floodLevel = baseLevel - WaterFluxOffset() + roomRain.globalRain.flood;

		if (ModManager.MSC)
		{ floodLevel = roomRain.globalRain.flood - room.world.RoomToWorldPos(new Vector2(0f, 0f), room.abstractRoom.index).y; }
		else
		{
			//bring water level up to regular when in vanilla (can't use global water level, curse you MSC exclusive feature!)
			if (room.world.rainCycle.TimeUntilRain > 0) floodLevel = NonMSCPreFloodBoost(floodLevel, room.waterObject.originalWaterLevel + WaterFluxOffset(), room.world.rainCycle.TimeUntilRain);
			else floodLevel = room.waterObject.originalWaterLevel + roomRain.globalRain.flood;
		}

		//original calculation
		if (!ModManager.MSC && room.waterFlux == null)
		{ return floodLevel; }

		//if it's not flooding, don't change anything
		if (room.world.rainCycle.TimeUntilRain > 0 && room.world.game.globalRain.drainWorldFlood == 0f)
		{ return baseLevel; }

		float result;

		//inverted calculation
		if (ModManager.MSC && room.waterInverted && room.waterFlux != null)
		{ result = room.waterFlux.fluxWaterLevel + (0f - Mathf.Max(floodLevel, 0f)); }

		//MSC calculation
		else
		{
			float shelterDrain = room.abstractRoom.shelter ? room.game.globalRain.drainWorldFlood : 0f;

			result = Mathf.Max(baseLevel, floodLevel - shelterDrain);
		}

		//clamp to fit within room
		float min = (ModManager.MSC && room.waterInverted) ? -80f : -5000f;
		float max = room.PixelHeight + 500f;
		return Mathf.Clamp(result, min, max);
	}

	/// <summary>
	/// the original roomRain.FloodLevel rewritten to be readable - kept around for reference
	/// </summary>
	public float OriginalFlood(RoomRain roomRain)
	{
		if (roomRain.room?.waterObject == null)
		{ return -100f; }

		Room room = roomRain.room;

		if (roomRain.dangerType != RoomRain.DangerType.Flood && roomRain.dangerType != RoomRain.DangerType.FloodAndRain)
		{ return room.waterObject.originalWaterLevel; }

		bool waterFlux = room.waterFlux != null;

		float baseLevel = waterFlux ? room.waterFlux!.fluxWaterLevel : room.waterObject.originalWaterLevel;
		float floodLevel = room.waterObject.originalWaterLevel + roomRain.globalRain.flood;

		if (ModManager.MSC)
		{ floodLevel = roomRain.globalRain.flood - room.world.RoomToWorldPos(new Vector2(0f, 0f), room.abstractRoom.index).y; }

		//original calculation
		if (!ModManager.MSC && !waterFlux)
		{ return floodLevel; }

		//if it's not flooding, don't change anything
		if (room.world.rainCycle.TimeUntilRain > 0 && room.world.game.globalRain.drainWorldFlood == 0f)
		{ return baseLevel; }

		float result;

		//inverted calculation
		if (ModManager.MSC && room.waterInverted && waterFlux)
		{ result = room.waterFlux!.fluxWaterLevel + (0f - Mathf.Max(floodLevel, 0f)); }

		//MSC calculation
		else
		{
			float shelterDrain = room.abstractRoom.shelter && !waterFlux ? room.game.globalRain.drainWorldFlood : 0f;

			result = Mathf.Max(!waterFlux ? 0f : room.waterFlux!.fluxWaterLevel, floodLevel - shelterDrain);
		}

		//clamp to fit within room
		float min = (ModManager.MSC && room.waterInverted) ? -80f : -5000f;
		float max = room.PixelHeight + 500f;
		return Mathf.Clamp(result, min, max);
	}
	#endregion

	public class CGDrySpotData : ManagedData
	{
		public FloatRect Rect
		{
			get
			{
				return new FloatRect(
					Mathf.Min(this.owner.pos.x, this.owner.pos.x + this.handlePos.x),
					Mathf.Min(this.owner.pos.y, this.owner.pos.y + this.handlePos.y),
					Mathf.Max(this.owner.pos.x, this.owner.pos.x + this.handlePos.x),
					Mathf.Max(this.owner.pos.y, this.owner.pos.y + this.handlePos.y));
			}
		}
#pragma warning disable 0649
		[BackedByField("1handle")]
		public Vector2 handlePos;

		[BooleanField("2waterFlux", true, displayName: "WaterFlux")]
		public bool waterFlux;

		[EnumField<FloodType>("3floodType", FloodType.None,control: ManagedFieldWithPanel.ControlType.button, displayName: "FloodType")]
		public FloodType floodType;
#pragma warning restore 0649
		public CGDrySpotData(PlacedObject owner) : base(owner, new ManagedField[] {
					new Vector2Field("1handle", new Vector2(100,100), Vector2Field.VectorReprType.rect)})
		{
		}

		public override void FromString(string s)
		{
			try { base.FromString(s); }
			catch 
			{
				string[] array = Regex.Split(s, "~");
				int num = 0;
				if (NeedsControlPanel && array.Length >= 2 && float.TryParse(array[0], out float x) && float.TryParse(array[1], out float y))
				{
					panelPos = new Vector2(x, y);
					num = 2;
				}

				for (int i = 0; i < fields.Length; i++)
				{
					if (array.Length == num + i)
					{
						LogError("CGDrySpot data uses old format - consider updating with the new settings");
						break;
					}

					try
					{
						object value = fields[i].FromString(array[num + i]);
						SetValue(fields[i].key, value);
					}
					catch (Exception)
					{
						LogError("Error parsing field " + fields[i].key + " from managed data type for " + owner.type.ToString() + "\nMaybe there's a version missmatch between the settings and the running version of the mod.");
					}
				}
			}
		}

		public enum FloodType
		{
		None,
		Offset,
		Sync
		}
	}
}
