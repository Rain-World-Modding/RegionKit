using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using RWCustom;
using System.IO;

namespace RegionKit.Modules.Misc;

//todo: has a lot of hard parsing logic, check if works
internal class CloudAdjustment
{
	public static void Apply()
	{
		//load values from the Properties file
		On.World.LoadMapConfig += World_LoadMapConfig;

		//set startAltitude and endAltitude if they're adjusted
		On.AboveCloudsView.ctor += AboveCloudsView_ctor;

		//pretty much the actual changes
		On.BackgroundScene.RoomToWorldPos += BackgroundScene_RoomToWorldPos;

		// CloudAdjustment.CRS = false;
		// Version version = new Version("0.9.43");
		// foreach (System.Reflection.Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
		// {
		// 	if (assembly.GetName().Name == "CustomRegions")
		// 	{
		// 		if (assembly.GetName().Version < version)
		// 		{
		// 			__log.LogWarning("Please update your CRS to use all CloudAdjustment features! v0.9.43 is needed");
		// 		}
		// 		else
		// 		{
		// 			CloudAdjustment.CRS = true;
		// 		}
		// 	}
		// }

	}

	//private static bool CRS;
	internal static float __offset = 0f;
	internal static float __offsetMin = -1000f;
	internal static float __offsetMax = 1620f;
	internal static float __startAltitude = 20000f;
	internal static float __endAltitude = 31400f;

	private static void World_LoadMapConfig(On.World.orig_LoadMapConfig orig, World self, SlugcatStats.Name slugcatNumber)
	{
		//reset to the default values, before attempting to change them
		__offset = 0f;
		__offsetMin = -500f;
		__offsetMax = 1620f;
		__startAltitude = 20000f;
		__endAltitude = 31400f;
		if (self.game != null && self.game.IsStorySession)
		{
			string? Cloudy = CloudSearch(self.region.name);
			if (Cloudy != null)
			{
				CloudAssign(Cloudy);
			}
		}
		orig(self, slugcatNumber);
	}


	//public static float startAltitude = 20000f;
	//public static float endAltitude = 31400f
	//divide these ^ by 20, basically


	private static Vector2 BackgroundScene_RoomToWorldPos(On.BackgroundScene.orig_RoomToWorldPos orig, BackgroundScene self, Vector2 inRoomPos)
	{
		bool NewValue = false;
		bool Clouds = false;
		Vector2 a = self.room.world.GetAbstractRoom(self.room.abstractRoom.index).mapPos / 3f + new Vector2(10f, 10f);


		for (int k = 0; k < self.room.roomSettings.effects.Count; k++)
		{
			if (self.room.roomSettings.effects[k].type == Modules.Effects.Enums_Effects.CloudAdjustment)
			{

				if (self.room.game.IsArenaSession)
				{
					a.y = Mathf.Lerp(900, 4000, (float)Math.Pow(self.room.roomSettings.effects[k].amount, 2.5));
					NewValue = true;
				}

				if (self.room.game.IsStorySession)
				{
					//a.y = Mathf.Lerp(startAltitude / 20f - 100, endAltitude / 10f, (float)Math.Pow(self.room.roomSettings.effects[k].amount, 2.5))

					a.y += Mathf.Lerp(__offsetMin, __offsetMax, self.room.roomSettings.effects[k].amount);
					NewValue = true;

				}

			}

			if (self.room.roomSettings.effects[k].type == RoomSettings.RoomEffect.Type.AboveCloudsView)
			{ Clouds = true; }

		}


		if (Clouds && __offset != 0f)
		{
			a.y += __offset;
			NewValue = true;
		}

		if (!NewValue || !Clouds)
		{ return orig(self, inRoomPos); }

		else
		{

			return a * 20f + inRoomPos - new Vector2((float)self.room.world.GetAbstractRoom(self.room.abstractRoom.index).size.x * 20f,
				(float)self.room.world.GetAbstractRoom(self.room.abstractRoom.index).size.y * 20f) / 2f;
		}

	}

	private static void AboveCloudsView_ctor(On.AboveCloudsView.orig_ctor orig, AboveCloudsView self, Room room, RoomSettings.RoomEffect effect)
	{
		orig(self, room, effect);

		if (self.room.game.IsStorySession)
		{
			//if these variables are changed from default, change them
			bool Change = false;
			if (__startAltitude != 20000f)
			{
				self.startAltitude = __startAltitude;
				Change = true;
			}
			if (__endAltitude != 31400f)
			{
				self.endAltitude = __endAltitude;
				Change = true;
			}

			if (Change)
			{
				self.sceneOrigo = new Vector2(2514f, (self.startAltitude + self.endAltitude) / 2f);
				Debug.Log("Cloud offset is" + __offset);
			}
		}
	}


	/// <summary>
	/// returns the path to the Properties.txt file if it exists, otherwise returns null
	/// </summary>
	public static string? CloudSearch(string region)
	{
		if (region == null)
		{ return null; }
		// if (CRS)
		// {
		// 	foreach (KeyValuePair<string, string> keyValuePair in CustomRegions.Mod.API.ActivatedPacks)
		// 	{
		// 		path = CustomRegions.Mod.API.BuildPath(keyValuePair.Value, "RegionID", region, "Properties.txt");

		// 		if (File.Exists(path))
		// 		{ break; }

		// 		else { path = null; }
		// 	}

		// }
		
		string? path = null;
		path = AssetManager.ResolveFilePath($"world/{region}/properties.txt");
		return File.Exists(path) ? null : null;
	}

	/// <summary>
	/// sets the static members if they're found in the Properties
	/// </summary>
	public static void CloudAssign(string path)
	{
		if (File.Exists(path))
		{
			foreach (string text in File.ReadLines(path))
			{
				string[] array = System.Text.RegularExpressions.Regex.Split(text, ": ");
				switch (array[0])
				{
				case "CloudOffset":
					float.TryParse(array[1], out __offset);
					break;

				case "CloudSliderMin":
					float.TryParse(array[1], out __offsetMin);
					break;

				case "CloudSliderMax":
					float.TryParse(array[1], out __offsetMax);
					break;

				case "CloudStartAltitude":
					float.TryParse(array[1], out __startAltitude);
					break;

				case "CloudEndAltitude":
					float.TryParse(array[1], out __endAltitude);
					break;
				}


			}

		}

	}



}
