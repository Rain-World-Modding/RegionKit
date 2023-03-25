using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MonoMod.RuntimeDetour;
using MonoMod.RuntimeDetour.HookGen;
using UnityEngine.PlayerLoop;
using static AboveCloudsView;
using static RegionKit.Modules.BackgroundBuilder.Data;
using static RoofTopView;
using DistantBuilding = AboveCloudsView.DistantBuilding;

namespace RegionKit.Modules.BackgroundBuilder
{
	internal static class Init
	{
		public static void Apply()
		{
			_CommonHooks.PostRoomLoad += _CommonHooks_PostRoomLoad;
			On.AboveCloudsView.ctor += AboveCloudsView_ctor;
			On.BackgroundScene.RoomToWorldPos += BackgroundScene_RoomToWorldPos;
			On.AboveCloudsView.CloseCloud.DrawSprites += CloseCloud_DrawSprites;
		}

		private static void CloseCloud_DrawSprites(On.AboveCloudsView.CloseCloud.orig_DrawSprites orig, CloseCloud self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
		{
			//pixel fix
			orig(self, sLeaser, rCam, timeStacker, camPos);
			sLeaser.sprites[0].scaleY += 2f;
		}

		private static Vector2 BackgroundScene_RoomToWorldPos(On.BackgroundScene.orig_RoomToWorldPos orig, BackgroundScene self, Vector2 inRoomPos)
		{
			if (self.slatedForDeletetion) return Vector2.zero;
			return orig(self, inRoomPos) + (self.room.roomSettings.BackgroundData().roomOffset * 20);
		}

		private static void AboveCloudsView_ctor(On.AboveCloudsView.orig_ctor orig, AboveCloudsView self, Room room, RoomSettings.RoomEffect effect)
		{
			orig(self, room, effect);
			Debug.Log("above ctor");

			Data.RoomBackgroundData data = self.room.roomSettings.BackgroundData();
			if (data.type != Data.BackgroundTemplateType.AboveCloudsView) return;

			if (data.backgroundData is not Data.CloudsBackgroundData cloudsData) return;

			Debug.Log("data is AboveCloudsView");

			self.startAltitude = cloudsData.startAltitude ?? self.startAltitude;
			self.endAltitude = cloudsData.endAltitude ?? self.endAltitude;
			self.cloudsStartDepth = cloudsData.cloudsStartDepth ?? self.cloudsStartDepth;
			self.cloudsEndDepth = cloudsData.cloudsEndDepth ?? self.cloudsEndDepth;
			self.distantCloudsEndDepth = cloudsData.distantCloudsEndDepth ?? self.distantCloudsEndDepth;

			self.sceneOrigo = new Vector2(2514f, (self.startAltitude + self.endAltitude) / 2f);

			bool redoBuildings = cloudsData.backgroundElementText.Count > 0;
			bool redoClouds = cloudsData.cloudsCount != null || cloudsData.distantCloudsCount != null 
				|| cloudsData.cloudsStartDepth != null || cloudsData.cloudsEndDepth != null
				|| cloudsData.distantCloudsEndDepth != null;

			foreach (BackgroundScene.BackgroundSceneElement element in self.elements.ToList())
			{
				if ((redoBuildings && element is DistantBuilding or DistantLightning or FlyingCloud) 
					|| (redoClouds && element is DistantCloud or CloseCloud))
				{
					element.Destroy();
					self.elements.Remove(element);
				}
			}

			if (redoBuildings)
			{
				foreach (string str in cloudsData.backgroundElementText)
				{
					BackgroundScene.BackgroundSceneElement? bselement = ElementFromString(self, str, out string tag);
					if (bselement != null)
					{
						self.AddElement(bselement);

						if (bselement is DistantLightning lightning && tag == "pebblesLightning")
						{ self.pebblesLightning = lightning; }

						else if (bselement is DistantBuilding building && tag == "spireLights")
						{ self.spireLights = building; }
					}
				}
			}

			if (redoClouds)
			{
				int num = (int)(cloudsData.cloudsCount ?? 7f);
				for (int i = 0; i < num; i++)
				{
					float cloudDepth = i / (float)(num - 1);
					CloseCloud cloud = new CloseCloud(self, new Vector2(0f, 0f), cloudDepth, i);
					self.AddElement(cloud);
				}

				num = (int)(cloudsData.distantCloudsCount ?? 11f);
				for (int j = 0; j < num; j++)
				{
					Debug.Log("adding distant cloud");
					float num15 = j / (float)(num - 1);
					DistantCloud dcloud = new DistantCloud(self, new Vector2(0f, -40f * self.cloudsEndDepth * (1f - num15)), num15, j);
					self.AddElement(dcloud);
				}
			}
		}

		public static string DefaultElementString(string str)
		{
			switch (str)
			{
			case "DistantBuilding":
				return "DistantBuilding: AtC_Structure1, -100, -100, 1, 0";

			case "DistantLightning":
				return "DistantLightning: AtC_Light1, -100, -100, 1, 100";

			case "FlyingCloud":
				return "FlyingCloud: 0, 75, 1, 0, 0.10, 0.7, 0.9";

			default:
				return "";
			}
		}

		public static BackgroundScene.BackgroundSceneElement? ElementFromString(BackgroundScene self, string str, out string tag)
		{
			tag = "";

			string[] parts = Regex.Split(str, ": ");

			if (parts.Length < 2) return null;

			Dictionary<string, object> args = ArgTypes(Regex.Split(parts[1], ", "), parts[0]);

			if (parts.Length > 2)
			{ tag = parts[2]; }

			Vector2 neutralPos = Vector2.zero;
			Vector2 posFromDrawPos = Vector2.zero;
			float depth = 0;
			if (args.ContainsKey("posx") && args.ContainsKey("posy"))
			{
				neutralPos = new Vector2((float)args["posx"], (float)args["posy"]);
				if (args.ContainsKey("depth"))
				{
					depth = (float)args["depth"];
					posFromDrawPos = self.PosFromDrawPosAtNeutralCamPos(neutralPos, depth);

				}
			}

			if (self is AboveCloudsView acv)
			{
				return parts[0] switch
				{
					"DistantBuilding" => new DistantBuilding(acv, (string)args["name"], posFromDrawPos, depth, (float)args["atmodepthadd"]),
					"DistantLightning" => new DistantLightning(acv, (string)args["name"], posFromDrawPos, depth, (float)args["minusDepth"]),
					"FlyingCloud" => new FlyingCloud(acv, posFromDrawPos, depth, (int)args["index"], (float)args["flattened"], (float)args["alpha"], (float)args["shaderInputColor"]),
					_ => null,
				};
			}
			else if (self is RoofTopView rtv)
			{
				if (self.room.roomSettings.BackgroundData().backgroundData is RoofTopBackgroundData data)
				{
					posFromDrawPos += new Vector2(0f, data.floorLevel);
					neutralPos += new Vector2(0f, data.floorLevel);
				}

				return parts[0] switch
				{
					"RFDistantBuilding" => new RoofTopView.DistantBuilding(rtv, (string)args["name"], posFromDrawPos, (float)args["depth"], (float)args["atmodepthadd"]),
					"RFBuilding" => new Building(rtv, (string)args["name"], posFromDrawPos, (float)args["depth"], (float)args["atmodepthadd"]),
					"Rubble" => new Rubble(rtv, (string)args["name"], neutralPos, (float)args["depth"], (int)args["seed"]),
					"Floor" => new Floor(rtv, (string)args["name"], neutralPos, (float)args["fromDepth"], (float)args["fromDepth"]),
					_ => null,
				};
			}
			else { return null; }

		}

		public static Dictionary<string, object> ArgTypes(string[] args, BackgroundScene.BackgroundSceneElement element)
		{
			return ArgTypes(args, element.GetType().Name);
		}

		public static Dictionary<string, object> ArgTypes(string[] args, string type)
		{
			float f;
			int i;
			bool b;

		Dictionary<string,object> dict = new Dictionary<string, object>();
			switch (type)
			{
			case "DistantBuilding":
			case "DistantLightning":
			case "Building":
			case "DustWave":
				if (args.Length != 5) break;

				dict["name"] = args[0];
				if (!float.TryParse(args[1], out f)) break; dict["posx"] = f;
				if (!float.TryParse(args[2], out f)) break; dict["posy"] = f;
				if (!float.TryParse(args[3], out f)) break; dict["depth"] = f;
				if (!float.TryParse(args[4], out f)) break; dict[(type == "DistantLightning")? "minusDepth" : "atmodepthadd"] = f;;

				break;


			case "Rubble":
				if (args.Length != 4) break;

				dict["name"] = args[0];
				if (!float.TryParse(args[1], out f)) break; dict["posx"] = f;
				if (!float.TryParse(args[2], out f)) break; dict["posy"] = f;
				if (!float.TryParse(args[3], out f)) break; dict["depth"] = f;
				if (!int.TryParse(args[4], out i)) break; dict["seed"] = i;

				break;

			case "Smoke":

				if (!float.TryParse(args[0], out f)) break; dict["posx"] = f;
				if (!float.TryParse(args[1], out f)) break; dict["posy"] = f;
				if (!float.TryParse(args[2], out f)) break; dict["depth"] = f;
				if (!float.TryParse(args[3], out f)) break; dict["flattened"] = f;
				if (!float.TryParse(args[4], out f)) break; dict["alpha"] = f;
				if (!float.TryParse(args[5], out f)) break; dict["shaderInputColor"] = f;
				if (!bool.TryParse(args[6], out b)) break; dict["shaderInputColor"] = b;
				break;

			case "Floor":
				if (args.Length != 5) break;

				dict["name"] = args[0];
				if (!float.TryParse(args[1], out f)) break; dict["posx"] = f;
				if (!float.TryParse(args[2], out f)) break; dict["posy"] = f;
				if (!float.TryParse(args[3], out f)) break; dict["fromDepth"] = f;
				if (!float.TryParse(args[4], out f)) break; dict["toDepth"] = f;

				break;

			case "FlyingCloud":
				if (args.Length != 7) break;
				if (!float.TryParse(args[0], out f)) break; dict["posx"] = f;
				if (!float.TryParse(args[1], out f)) break; dict["posy"] = f;
				if (!float.TryParse(args[2], out f)) break; dict["depth"] = f;
				if (!int.TryParse(args[3], out i)) break; dict["index"] = i;
				if (!float.TryParse(args[4], out f)) break; dict["flattened"] = f;
				if (!float.TryParse(args[5], out f)) break; dict["alpha"] = f;
				if (!float.TryParse(args[6], out f)) break; dict["shaderInputColor"] = f;


				break;
			default:
				break;
			}

			return dict;
		}

		private static void _CommonHooks_PostRoomLoad(Room obj)
		{
			if (obj.roomSettings.BackgroundData().type == Data.BackgroundTemplateType.AboveCloudsView)
			{ }
		}
	}
}
