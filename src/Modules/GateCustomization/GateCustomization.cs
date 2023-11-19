using System.Diagnostics.Eventing.Reader;
using System.IO;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using static RegionKit.Modules.GateCustomization.GateDataRepresentations;
using static RegionKit.Modules.GateCustomization.RegionGateCWT;
using Random = UnityEngine.Random;

namespace RegionKit.Modules.GateCustomization;

internal static class GateCustomization
{
	// Quick summary of how this works:
	// I store the ManagedData of the PlacedObjects in a CWT for each Gate.
	// The Gate can then get the data without having to search for the PlacedObject.
	// Alot of the hooks just copy some of the code from the original but changes the position of stuff.
	// Not whole methods are copied obviously, only the required parts.
	// Not sure if the way I coded this is the best but it works and all Rain World mods have some level of jank in the code.

	private static bool shadersLoaded = false;
	
	public static void Enable()
	{
		On.RegionGate.ctor += RegionGate_ctor;
		On.RegionGate.Update += RegionGate_Update;
		On.RegionGate.ChangeDoorStatus += RegionGate_ChangeDoorStatus;
		On.RegionGate.AllPlayersThroughToOtherSide += RegionGate_AllPlayersThroughToOtherSide;
		On.RegionGate.DetectZone += RegionGate_DetectZone;

		On.WaterGate.ctor += WaterGate_ctor;

		On.ElectricGate.ctor += ElectricGate_ctor;
		On.ElectricGate.Update += ElectricGate_Update;

		On.RegionGateGraphics.ctor += RegionGateGraphics_ctor;
		On.RegionGateGraphics.Update += RegionGateGraphics_Update;
		On.RegionGateGraphics.DrawSprites += RegionGateGraphics_DrawSprites;

		On.RegionGateGraphics.DoorGraphic.ctor += DoorGraphic_ctor;
		On.RegionGateGraphics.DoorGraphic.InitiateSprites += DoorGraphic_InitiateSprites;
		On.RegionGateGraphics.DoorGraphic.DrawSprites += DoorGraphic_DrawSprites;

		On.GateKarmaGlyph.ctor += GateKarmaGlyph_ctor;
		On.GateKarmaGlyph.UpdateDefaultColor += GateKarmaGlyph_UpdateDefaultColor;

		On.HUD.Map.GateMarker.ctor += GateMarker_ctor;

		IL.RegionGate.Update += IL_RegionGate_Update;
		IL.RegionGateGraphics.Update += IL_RegionGateGraphics_Update;
	}

	public static void Disable()
	{
		On.RegionGate.ctor -= RegionGate_ctor;
		On.RegionGate.Update -= RegionGate_Update;
		On.RegionGate.ChangeDoorStatus -= RegionGate_ChangeDoorStatus;
		On.RegionGate.AllPlayersThroughToOtherSide -= RegionGate_AllPlayersThroughToOtherSide;
		On.RegionGate.DetectZone -= RegionGate_DetectZone;

		On.WaterGate.ctor -= WaterGate_ctor;

		On.ElectricGate.ctor -= ElectricGate_ctor;
		On.ElectricGate.Update -= ElectricGate_Update;

		On.RegionGateGraphics.ctor -= RegionGateGraphics_ctor;
		On.RegionGateGraphics.Update -= RegionGateGraphics_Update;
		On.RegionGateGraphics.DrawSprites -= RegionGateGraphics_DrawSprites;

		On.RegionGateGraphics.DoorGraphic.ctor -= DoorGraphic_ctor;
		On.RegionGateGraphics.DoorGraphic.InitiateSprites -= DoorGraphic_InitiateSprites;
		On.RegionGateGraphics.DoorGraphic.DrawSprites -= DoorGraphic_DrawSprites;

		On.GateKarmaGlyph.ctor -= GateKarmaGlyph_ctor;
		On.GateKarmaGlyph.UpdateDefaultColor -= GateKarmaGlyph_UpdateDefaultColor;

		On.HUD.Map.GateMarker.ctor -= GateMarker_ctor;

		IL.RegionGate.Update -= IL_RegionGate_Update;
		IL.RegionGateGraphics.Update -= IL_RegionGateGraphics_Update;
	}
		
	public static void LoadShaders(RainWorld rainWorld)
	{
		if (!shadersLoaded)
		{
			// Custom shaders
			AssetBundle assetBundle = AssetBundle.LoadFromFile(AssetManager.ResolveFilePath("assets/regionkit/gatecustomization"));
			rainWorld.Shaders["ColoredSprite2Lit"] = FShader.CreateShader("ColoredSprite2Lit", assetBundle.LoadAsset<Shader>("Assets/ColoredSprite2Lit.shader"));

			shadersLoaded = true;
		}
	}

	private static void RegionGate_ctor(On.RegionGate.orig_ctor orig, RegionGate self, Room room)
	{
		// Get data from the placed objects and store them into a CWT
		self.GetData().commonGateData = GetPlacedObjectData(room, "CommonGateData");
		self.GetData().waterGateData = GetPlacedObjectData(room, "WaterGateData");
		self.GetData().electricGateData = GetPlacedObjectData(room, "ElectricGateData");

		orig(self, room);
	}

	private static void RegionGate_Update(On.RegionGate.orig_Update orig, RegionGate self, bool eu)
	{
		orig(self, eu);

		RegionGateData regionGateData = self.GetData();

		// This code for the removal of doors was pretty much just copy pasted from CGGateCustomization
		// Seems to work?

		if (regionGateData.commonGateData != null)
		{
			if (regionGateData.commonGateData.GetValue<bool>("noDoor0"))
			{
				self.doors[0].closeSpeed = 0f;
				self.graphics.doorGraphs[0].lastClosedFac = self.doors[0].closedFac;
				self.goalDoorPositions[0] = self.doors[0].closedFac;
			}

			if (regionGateData.commonGateData.GetValue<bool>("noDoor2"))
			{
				self.doors[2].closeSpeed = 0f;
				self.graphics.doorGraphs[2].lastClosedFac = self.doors[2].closedFac;
				self.goalDoorPositions[2] = self.doors[2].closedFac;
			}

			if (regionGateData.commonGateData.GetValue<bool>("dontCutSong"))
			{
				if (self.startCounter == 60)
				{
					if (self.room.game.manager.musicPlayer != null && self.room.game.manager.musicPlayer.song is Music.GhostSong ghostSong)
					{
						ghostSong.stopAtGate = false;
					}
				}
			}
		}
	}

	private static void RegionGate_ChangeDoorStatus(On.RegionGate.orig_ChangeDoorStatus orig, RegionGate self, int door, bool open)
	{
		RegionGateData regionGateData = self.GetData();

		if (regionGateData.commonGateData != null)
		{
			IntVector2 gateTilePosition = regionGateData.commonGateData.GetTilePosition(self.room);
			int num = gateTilePosition.x - 10 + door * 9;

			for (int i = 0; i < 2; i++)
			{
				for (int j = gateTilePosition.y - 4; j <= gateTilePosition.y + 4; j++)
				{
					self.room.GetTile(num + i, j).Terrain = (open ? Room.Tile.TerrainType.Air : Room.Tile.TerrainType.Solid);
				}
			}
		}
		else
		{
			// Should be okay to not always call orig? 
			// Not a commonly hooked method I'd assume.
			orig(self, door, open);
		}
	}

	private static bool RegionGate_AllPlayersThroughToOtherSide(On.RegionGate.orig_AllPlayersThroughToOtherSide orig, RegionGate self)
	{
		RegionGateData regionGateData = self.GetData();

		if (regionGateData.commonGateData != null)
		{
			IntVector2 gateTilePosition = regionGateData.commonGateData.GetTilePosition(self.room);

			for (int i = 0; i < self.room.game.Players.Count; i++)
			{
				if (self.room.game.Players[i].pos.room == self.room.abstractRoom.index && (!self.letThroughDir || self.room.game.Players[i].pos.x < gateTilePosition.x + 3) && (self.letThroughDir || self.room.game.Players[i].pos.x > gateTilePosition.x - 4))
				{
					return false;
				}
			}
			return true;
		}
		else
		{
			return orig(self);
		}
	}

	private static int RegionGate_DetectZone(On.RegionGate.orig_DetectZone orig, RegionGate self, AbstractCreature crit)
	{
		// This hook needs to be changed if irregular door placements is to be implemented

		// Also not sure if it's a good idea to not always call orig in this method but
		// isn't it unavoidable to have to do it like this?

		RegionGateData regionGateData = self.GetData();

		if (regionGateData.commonGateData != null)
		{
			IntVector2 gateTilePosition = regionGateData.commonGateData.GetTilePosition(self.room);

			if (crit.pos.room != self.room.abstractRoom.index)
			{
				return -1;
			}
			if (crit.pos.x < gateTilePosition.x - 8)
			{
				return 0;
			}
			if (crit.pos.x < gateTilePosition.x)
			{
				return 1;
			}
			if (crit.pos.x < gateTilePosition.x + 8)
			{
				return 2;
			}
			return 3;
		}
		else
		{
			return orig(self, crit);
		}
	}

	private static void WaterGate_ctor(On.WaterGate.orig_ctor orig, WaterGate self, Room room)
	{
		orig(self, room);

		RegionGateData regionGateData = self.GetData();

		if (regionGateData.commonGateData != null)
		{
			IntVector2 gateTilePosition = regionGateData.commonGateData.GetTilePosition(room);

			room.RemoveObject(self.waterFalls[0]);
			room.RemoveObject(self.waterFalls[1]);

			self.waterFalls[0] = new WaterFall(room, new IntVector2(gateTilePosition.x - 6, gateTilePosition.y + 11), 0f, 3);
			self.waterFalls[1] = new WaterFall(room, new IntVector2(gateTilePosition.x + 3, gateTilePosition.y + 11), 0f, 3);

			room.AddObject(self.waterFalls[0]);
			room.AddObject(self.waterFalls[1]);

			self.waterFalls[0].setFlow = 0f;
			self.waterFalls[1].setFlow = 0f;
		}
	}

	private static void ElectricGate_ctor(On.ElectricGate.orig_ctor orig, ElectricGate self, Room room)
	{
		orig(self, room);

		RegionGateData regionGateData = self.GetData();

		if (regionGateData.commonGateData != null)
		{
			IntVector2 gateTilePosition = regionGateData.commonGateData.GetTilePosition(room);

			for (int i = 0; i < 4; i++)
			{
				for (int j = 0; j < 2; j++)
				{
					room.RemoveObject(self.lamps[i, j]);

					bool flag = i < 2;
					bool flag2 = i % 2 == 0;
					if (flag)
					{
						flag2 = !flag2;
					}
					// Lamp layout looks like this
					// 2  3
					//
					// 1  0

					self.lamps[i, j].pos = new Vector2((gateTilePosition.x + (flag2 ? -5f : 4f)) * 20f + 10f, (gateTilePosition.y + (flag ? -3f : 3f)) * 20f + 10f);

					room.AddObject(self.lamps[i, j]);
				}
			}
		}

		if (regionGateData.electricGateData != null)
		{
			if (regionGateData.electricGateData.GetValue<bool>("lampColorOverride"))
			{
				for (int i = 0; i < 4; i++)
				{
					for (int j = 0; j < 2; j++)
					{
						self.lamps[i, j].color = Color.HSVToRGB(
							regionGateData.electricGateData.GetValue<float>("lampHue"),
							regionGateData.electricGateData.GetValue<float>("lampSaturation"),
							1f
							//regionGateData.electricGateData.GetValue<float>("lampBrightness")
							);
					}
				}
			}
		}
	}

	private static void ElectricGate_Update(On.ElectricGate.orig_Update orig, ElectricGate self, bool eu)
	{
		orig(self, eu);

		RegionGateData regionGateData = self.GetData();

		if (regionGateData.electricGateData != null)
		{
			self.bustedLamp = -1;
			for (int i = 0; i < 4; i++)
			{
				if (!regionGateData.electricGateData.GetValue<bool>($"lamp{i}"))
				{
					for (int j = 0; j < 2; j++)
					{
						self.lamps[i, j].setAlpha = new float?(0f);
						self.lamps[i, j].setRad = new float?(0f);
					}
				}
			}
		}
	}

	private static void RegionGateGraphics_ctor(On.RegionGateGraphics.orig_ctor orig, RegionGateGraphics self, RegionGate gate)
	{
		orig(self, gate);

		RegionGateData regionGateData = gate.GetData();

		if (regionGateData != null && gate is WaterGate)
		{
			if (regionGateData.waterGateData != null)
			{
				if (self.water != null)
				{
					gate.room.drawableObjects.Remove(self.water);
				}

				if (regionGateData.waterGateData.GetValue<bool>("water"))
				{
					// Should maybe create a new water class since the water covers the whole screen left to right

					self.water = new Water(gate.room, regionGateData.waterGateData.GetTilePosition(gate.room).y + 33);
					gate.room.drawableObjects.Add(self.water);
					self.water.cosmeticLowerBorder = regionGateData.waterGateData.GetTilePosition(gate.room).y * 20 - 20f;
				}
				else
				{
					self.water = null;
				}
			}

			if (regionGateData.commonGateData != null)
			{
				Vector2 gatePosition = regionGateData.commonGateData.GetPosition(gate.room);

				self.heaterPositions = new Vector2[2];

				self.heaterPositions[0].y = gatePosition.y - 220f;
				self.heaterPositions[1].y = gatePosition.y - 220f;
				self.heaterPositions[0].x = gatePosition.x - 100f;
				self.heaterPositions[1].x = gatePosition.x + 80f;

				self.heatersHeat = new float[2, 3];
				self.heaterQuads = new Vector2[2, 2][];

				Vector2 size = Futile.atlasManager.GetElementWithName("RegionGate_Heater").sourceRect.size;

				// This code is confusing but I don't have to understand it
				// Maybe replace with an IL hook? Prob not neccesary
				for (int j = 0; j < 2; j++)
				{
					Vector2 a = self.heaterPositions[j];
					Vector2[] array = new Vector2[]
					{
					a + new Vector2(-size.x / 2f, -size.y / 2f),
					a + new Vector2(-size.x / 2f, size.y / 2f),
					a + new Vector2(size.x / 2f, size.y / 2f),
					a + new Vector2(size.x / 2f, -size.y / 2f)
					};
					if (j == 1)
					{
						Vector2 vector = array[0];
						array[0] = array[3];
						array[3] = vector;
						vector = array[1];
						array[1] = array[2];
						array[2] = vector;
					}
					for (int k = 0; k < 2; k++)
					{
						self.heaterQuads[j, k] = new Vector2[4];
						for (int l = 0; l < 4; l++)
						{
							array[l] += Custom.RNV() * Random.value;
							self.heaterQuads[j, k][l] = array[l] + Custom.RNV() * Random.value;
							array[l] += Custom.RNV() * Random.value;
						}
					}
				}
			}
		}
	}

	private static void RegionGateGraphics_Update(On.RegionGateGraphics.orig_Update orig, RegionGateGraphics self)
	{
		orig(self);

		RegionGateData regionGateData = self.gate.GetData();

		if (regionGateData.waterGateData != null)
		{
			if (!regionGateData.waterGateData.GetValue<bool>("bubbleFX"))
			{
				self.bubCounter = 0;
			}

			for (int i = 0; i < 2; i++)
			{
				if ((self.gate.letThroughDir ? 0 : 1) == i)
				{
					if (regionGateData.waterGateData.GetValue<HeaterData>($"heater{i}") != HeaterData.Nrml)
					{
						self.steamLoop.volume = 0;
					}
				}
			}
		}

		if (regionGateData.commonGateData != null)
		{
			IntVector2 gateTilePosition = regionGateData.commonGateData.GetTilePosition(self.gate.room);
			self.steamLoop.pos = new Vector2(gateTilePosition.x + (self.gate.letThroughDir ? -5f : 4f) * 20f + 10f, gateTilePosition.y * 20f - 210f);
		}

		if (regionGateData.electricGateData != null)
		{
			for (int i = 0; i < 2; i++)
			{
				if ((self.gate.letThroughDir ? 0 : 1) == i)
				{
					if (regionGateData.electricGateData.GetValue<bool>($"steamer{i}broken"))
					{
						self.electricSteam = -1f;
					}
				}
			}
		}
	}

	private static void RegionGateGraphics_DrawSprites(On.RegionGateGraphics.orig_DrawSprites orig, RegionGateGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
	{
		orig(self, sLeaser, rCam, timeStacker, camPos);

		RegionGateData regionGateData = self.gate.GetData();

		if (self.gate is ElectricGate && regionGateData.electricGateData != null)
		{
			Vector2 electricGateDataPosition = regionGateData.electricGateData.GetPosition(self.gate.room, SnapMode.NoSnap);

			if (regionGateData.electricGateData.GetValue<bool>("battery"))
			{
				sLeaser.sprites[self.BatteryMeterSprite].x = electricGateDataPosition.x - camPos.x;
				sLeaser.sprites[self.BatteryMeterSprite].y = electricGateDataPosition.y - camPos.y;

				sLeaser.sprites[self.BatteryMeterSprite].isVisible = true;
			}
			else
			{
				sLeaser.sprites[self.BatteryMeterSprite].isVisible = false;
			}

			if (regionGateData.electricGateData.GetValue<bool>("batteryColorOverride"))// && (self.gate as ElectricGate).batteryLeft < 1.1f)
			{
				float num = (self.gate as ElectricGate).batteryChanging ? 1f : 0f;

				sLeaser.sprites[self.BatteryMeterSprite].color = Color.Lerp(
					Custom.HSL2RGB(
						regionGateData.electricGateData.GetValue<float>("batteryHue") + Random.value * (0.05f * num + 0.025f),
						regionGateData.electricGateData.GetValue<float>("batterySaturation"),
						(regionGateData.electricGateData.GetValue<float>("batteryLightness") + Random.value * 0.2f * num) * Mathf.Lerp(1f, 0.25f, self.darkness)),
					self.blackColor,
					0.5f);
			}
		}

		if (self.gate is WaterGate && regionGateData.waterGateData != null)
		{
			for (int i = 0; i < 2; i++)
			{
				if (regionGateData.waterGateData.GetValue<HeaterData>($"heater{i}") != HeaterData.Nrml)
				{
					if ((self.gate.letThroughDir ? 0 : 1) == i)
					{
						sLeaser.sprites[self.HeatDistortionSprite].isVisible = false;

						if (self.heaterLightsource != null)
						{
							self.heaterLightsource.setAlpha = new float?(0f);
						}
					}

					if (regionGateData.waterGateData.GetValue<HeaterData>($"heater{i}") == HeaterData.Brokn)
					{
						Color color = Color.Lerp(self.blackColor, Color.Lerp(self.blackColor, self.fogColor, 0.3f), 0.8f);
						sLeaser.sprites[self.HeaterSprite(i, 0)].color = self.blackColor;
						sLeaser.sprites[self.HeaterSprite(i, 1)].color = color;
					}


				}
				if (regionGateData.waterGateData.GetValue<HeaterData>($"heater{i}") == HeaterData.Hiddn)
				{
					sLeaser.sprites[self.HeaterSprite(i, 0)].isVisible = false;
					sLeaser.sprites[self.HeaterSprite(i, 1)].isVisible = false;
				}
				else
				{
					sLeaser.sprites[self.HeaterSprite(i, 0)].isVisible = true;
					sLeaser.sprites[self.HeaterSprite(i, 1)].isVisible = true;
				}
			}
		}
	}

	private static void DoorGraphic_ctor(On.RegionGateGraphics.DoorGraphic.orig_ctor orig, RegionGateGraphics.DoorGraphic self, RegionGateGraphics rgGraphics, RegionGate.Door door)
	{
		orig(self, rgGraphics, door);

		RegionGateData regionGateData = rgGraphics.gate.GetData();

		if (regionGateData.commonGateData != null)
		{
			Vector2 gatePosition = regionGateData.commonGateData.GetPosition(rgGraphics.gate.room);

			self.posZ = new Vector2((-9f + 9f * (float)door.number) * 20f + gatePosition.x - 10, gatePosition.y + 90f);

			// Sound fix
			self.rustleLoop = new StaticSoundLoop(SoundID.Gate_Clamps_Moving_LOOP, self.posZ, rgGraphics.gate.room, 0f, 1f);
			self.screwTurnLoop = new StaticSoundLoop((rgGraphics.gate is WaterGate) ? SoundID.Gate_Water_Screw_Turning_LOOP : SoundID.Gate_Electric_Screw_Turning_LOOP, self.posZ, rgGraphics.gate.room, 0f, 1f);
			self.Reset();
		}
	}

	private static void DoorGraphic_InitiateSprites(On.RegionGateGraphics.DoorGraphic.orig_InitiateSprites orig, RegionGateGraphics.DoorGraphic self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
	{
		orig(self, sLeaser, rCam);

		RegionGateData regionGateData = self.rgGraphics.gate.GetData();

		if (regionGateData.commonGateData != null)
		{
			if (regionGateData.commonGateData.GetValue<bool>($"door{self.door.number}Lit"))
			{
				for (int i = self.TotalSprites * self.door.number; i < self.TotalSprites * (self.door.number + 1); i++)
				{
					sLeaser.sprites[i].shader = self.rgGraphics.gate.room.game.rainWorld.Shaders["ColoredSprite2Lit"];
				}
			}

			if (regionGateData.commonGateData.GetValue<bool>($"noDoor{self.door.number}"))
			{
				for (int i = self.TotalSprites * self.door.number; i < self.TotalSprites * (self.door.number + 1); i++)
				{
					sLeaser.sprites[i].isVisible = false;
				}
			}
		}
	}

	private static void DoorGraphic_DrawSprites(On.RegionGateGraphics.DoorGraphic.orig_DrawSprites orig, RegionGateGraphics.DoorGraphic self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
	{
		orig(self, sLeaser, rCam, timeStacker, camPos);

		RegionGateData regionGateData = self.rgGraphics.gate.GetData();

		if (regionGateData.commonGateData != null)
		{
			bool lit = regionGateData.commonGateData.GetValue<bool>($"door{self.door.number}Lit");
			if ((lit ? "ColoredSprite2Lit" : "ColoredSprite2") != sLeaser.sprites[0 + self.door.number * self.TotalSprites].shader.name)
			{
				for (int i = self.TotalSprites * self.door.number; i < self.TotalSprites * (self.door.number + 1); i++)
				{
					sLeaser.sprites[i].shader = self.rgGraphics.gate.room.game.rainWorld.Shaders[lit ? "ColoredSprite2Lit" : "ColoredSprite2"];
				}
			}
		}
	}

	private static void GateKarmaGlyph_ctor(On.GateKarmaGlyph.orig_ctor orig, GateKarmaGlyph self, bool side, RegionGate gate, RegionGate.GateRequirement requirement)
	{
		orig(self, side, gate, requirement);

		RegionGateData regionGateData = gate.GetData();

		if (regionGateData.commonGateData != null)
		{
			IntVector2 gateTilePosition = regionGateData.commonGateData.GetTilePosition(gate.room);

			self.pos = gate.room.MiddleOfTile(gateTilePosition.x + (side ? 4 : -5), gateTilePosition.y + 2);
			self.lastPos = self.pos;
		}
	}

	private static void GateKarmaGlyph_UpdateDefaultColor(On.GateKarmaGlyph.orig_UpdateDefaultColor orig, GateKarmaGlyph self)
	{
		orig(self);

		RegionGateData regionGateData = self.gate.GetData();

		if (regionGateData.commonGateData != null)
		{
			if (regionGateData.commonGateData.GetValue<bool>("colorOverride"))
			{
				self.myDefaultColor = Color.HSVToRGB(
					regionGateData.commonGateData.GetValue<float>("hue"),
					regionGateData.commonGateData.GetValue<float>("saturation"),
					regionGateData.commonGateData.GetValue<float>("brightness")
					);

				if (self.gate.unlocked)
				{
					self.myDefaultColor = Color.Lerp(self.myDefaultColor, new Color(0.2f, 0.8f, 1f, 1f), 0.6f);
				}
			}
		}
	}

	private static void GateMarker_ctor(On.HUD.Map.GateMarker.orig_ctor orig, HUD.Map.GateMarker self, HUD.Map map, int room, RegionGate.GateRequirement karma, bool showAsOpen)
	{
		orig(self, map, room, karma, showAsOpen);

		// This probably isn't the best way to implement this.
		// I'd prefer if an external file wasn't required
		// The better option would be to load the settings file manually?

		// todo: Remove modify file, load settings file in Map.ctor I think

		string path = AssetManager.ResolveFilePath("world/gates/gatemapinfo.txt");
		if (!File.Exists(path))
		{
			// It dont exist
			return;
		}

		string[] lines = File.ReadAllLines(path);

		for (int i = 0; i < lines.Length; i++)
		{
			// Format: 
			// GATE_XX_YY,tilePosX,tilePosY

			string[] s = lines[i].Split(',');

			int x = int.Parse(s[1]);
			int y = int.Parse(s[2]);

			if (map.mapData.NameOfRoom(room) == s[0])
			{
				self.inRoomPos.x = x * 20f;
				self.inRoomPos.y = y * 20f;
			}
		}
	}

	private static void IL_RegionGateGraphics_Update(ILContext il)
	{
		// todo: This IL hook is a mess, maybe rewrite it

		var cursor = new ILCursor(il);

		#region WaterGate

		int test = 0;

		// Matches
		// 216 FloatRect confines = new FloatRect((float)(17 + 9 * l) * 20f, 0f, (float)(22 + 9 * l) * 20f, 420f);
		cursor.GotoNext(
			x => x.MatchLdloca(out _),
			x => x.MatchLdcI4(17),
			x => x.MatchLdcI4(9),
			x => x.MatchLdloc(out _),
			x => x.MatchMul(),
			x => x.MatchAdd(),
			x => x.MatchConvR4(),
			x => x.MatchLdcR4(20),
			x => x.MatchMul(),
			x => x.MatchLdcR4(0),
			x => x.MatchLdcI4(22),
			x => x.MatchLdcI4(9),
			x => x.MatchLdloc(out test),
			x => x.MatchMul(),
			x => x.MatchAdd(),
			x => x.MatchConvR4(),
			x => x.MatchLdcR4(20),
			x => x.MatchMul(),
			x => x.MatchLdcR4(420),
			x => x.MatchCall(nameof(FloatRect), ".ctor")
			);

		cursor.RemoveRange(20);

		cursor.Emit(OpCodes.Ldarg_0);
		cursor.Emit(OpCodes.Ldloc_S, (byte)test);

		cursor.EmitDelegate<Func<RegionGateGraphics, int, FloatRect>>((self, l) =>
		{
			RegionGateData regionGateData = self.gate.GetData();

			if (regionGateData.commonGateData != null)
			{
				IntVector2 gateTilePosition = regionGateData.commonGateData.GetTilePosition(self.gate.room);

				return new FloatRect((float)(gateTilePosition.x - 7 + 9 * l) * 20f, (gateTilePosition.y - 12) * 20f, (float)(gateTilePosition.x + 9 * l) * 20f, (gateTilePosition.y + 9) * 20f);
			}
			else
			{
				return new FloatRect((float)(17 + 9 * l) * 20f, 0f, (float)(22 + 9 * l) * 20f, 420f);
			}
		});

		cursor.Emit(OpCodes.Stloc_S, (byte)8);


		//Matches
		// 223 this.smoke.EmitSmoke(pos, Custom.DegToVec(Random.value * 360f), confines, Mathf.Pow(this.heatersHeat[l, 2], 0.75f));
		cursor.GotoNext(
			x => x.MatchLdarg(0),
			x => x.MatchLdfld<RegionGateGraphics>(nameof(RegionGateGraphics.smoke)),
			x => x.MatchLdloc(out _),
			x => x.MatchCall(out _),
			x => x.MatchLdcR4(360),
			x => x.MatchMul()
			);

		cursor.Emit(OpCodes.Ldarg_0);

		cursor.EmitDelegate<Func<RegionGateGraphics, bool>>((self) =>
		{
			RegionGateData regionGateData = self.gate.GetData();

			if (regionGateData.waterGateData != null)
			{
				for (int i = 0; i < 2; i++)
				{
					if ((self.gate.letThroughDir ? 0 : 1) == i)
					{
						if (regionGateData.waterGateData.GetValue<HeaterData>($"heater{i}") != HeaterData.Nrml)
						{
							// Skip sound and steam
							return true;
						}
					}
				}
			}

			return false;
		});


		ILLabel label2 = il.DefineLabel();

		cursor.Emit(OpCodes.Brtrue, label2); // Skip spawning steam and playing sound if heater is broken.

		cursor.Index += 27;
		cursor.MarkLabel(label2);

		#endregion

		#region ElectricGate

		// Matches
		// 285 FloatRect confines2 = new FloatRect((float)(17 + 9 * num4) * 20f, 0f, (float)(22 + 9 * num4) * 20f, 420f);
		cursor.GotoNext(
			x => x.MatchLdloca(out _),
			x => x.MatchLdcI4(17),
			x => x.MatchLdcI4(9),
			x => x.MatchLdloc(out _),
			x => x.MatchMul(),
			x => x.MatchAdd(),
			x => x.MatchConvR4(),
			x => x.MatchLdcR4(20),
			x => x.MatchMul(),
			x => x.MatchLdcR4(0),
			x => x.MatchLdcI4(22),
			x => x.MatchLdcI4(9),
			x => x.MatchLdloc(out _),
			x => x.MatchMul(),
			x => x.MatchAdd(),
			x => x.MatchConvR4(),
			x => x.MatchLdcR4(20),
			x => x.MatchMul(),
			x => x.MatchLdcR4(420),
			x => x.MatchCall(nameof(FloatRect), ".ctor")
			);

		cursor.RemoveRange(20);

		cursor.Emit(OpCodes.Ldarg_0);
		cursor.Emit(OpCodes.Ldloc_S, (byte)11);

		cursor.EmitDelegate<Func<RegionGateGraphics, int, FloatRect>>((self, num4) =>
		{
			RegionGateData regionGateData = self.gate.GetData();

			if (regionGateData.commonGateData != null)
			{
				IntVector2 gateTilePosition = regionGateData.commonGateData.GetTilePosition(self.gate.room);

				return new FloatRect((float)(gateTilePosition.x - 7 + 9 * num4) * 20f, (gateTilePosition.y - 12) * 20f, (float)(gateTilePosition.x + 9 * num4) * 20f, (gateTilePosition.y + 9) * 20f);
			}
			else
			{
				return new FloatRect((float)(17 + 9 * num4) * 20f, 0f, (float)(22 + 9 * num4) * 20f, 420f);
			}
		});

		cursor.Emit(OpCodes.Stloc_S, (byte)12);


		// Steam spawnpos, wasnt needed for the water gate since it uses the heater positions which where already modified

		// Matches
		// 291 Vector2 pos2 = new Vector2(10f + (this.gate.letThroughDir ? 19f : 28f) * 20f, 30f) + new Vector2(Mathf.Lerp(-1f, 1f, Random.value) * 15f, Mathf.Lerp(-1f, 1f, Random.value) * 10f);
		cursor.GotoNext(
			x => x.MatchLdcR4(10),
			x => x.MatchLdarg(0),
			x => x.MatchLdfld<RegionGateGraphics>(nameof(RegionGateGraphics.gate)),
			x => x.MatchLdfld<RegionGate>(nameof(RegionGate.letThroughDir)),
			x => x.MatchBrtrue(out _),
			x => x.MatchLdcR4(28),
			x => x.MatchBr(out _),
			x => x.MatchLdcR4(19),
			x => x.MatchLdcR4(20),
			x => x.MatchMul(),
			x => x.MatchAdd(),
			x => x.MatchLdcR4(30)
			);

		// For some reason the game shits itself if I try to use a RemoveRange(28) here even tough that should be corect?
		// So instead I have to do this garbage with a branch.
		// I probably did something wrong but cant bother to fix it since this works

		ILLabel label = il.DefineLabel();

		cursor.Emit(OpCodes.Br, label);

		cursor.Index += 28;
		cursor.MarkLabel(label);

		cursor.Emit(OpCodes.Ldarg_0);

		cursor.EmitDelegate<Func<RegionGateGraphics, Vector2>>((self) =>
		{
			RegionGateData regionGateData = self.gate.GetData();

			if (regionGateData.commonGateData != null)
			{
				IntVector2 gateTilePosition = regionGateData.commonGateData.GetTilePosition(self.gate.room);
				return new Vector2((gateTilePosition.x + (self.gate.letThroughDir ? -5f : 4f)) * 20f + 10f, gateTilePosition.y * 20 - 210f) + new Vector2(Mathf.Lerp(-1f, 1f, Random.value) * 15f, Mathf.Lerp(-1f, 1f, Random.value) * 10f);
			}
			else
			{
				return new Vector2(10f + (self.gate.letThroughDir ? 19f : 28f) * 20f, 30f) + new Vector2(Mathf.Lerp(-1f, 1f, Random.value) * 15f, Mathf.Lerp(-1f, 1f, Random.value) * 10f);
			}
		});

		cursor.Emit(OpCodes.Stloc_S, (byte)13);

		#endregion
	}

	private static void IL_RegionGate_Update(ILContext il)
	{
		ILCursor cursor = new ILCursor(il);

		ILLabel label = il.DefineLabel();

		// Matches
		// 168 if (this.mode == RegionGate.Mode.MiddleClosed)
		cursor.GotoNext(MoveType.After,
			x => x.MatchBrfalse(out label)
			);

		cursor.Emit(OpCodes.Ldarg_0);

		cursor.EmitDelegate<Func<RegionGate, bool>>((self) =>
		{
			RegionGateData regionGateData = self.GetData();

			if (regionGateData.commonGateData != null)
			{
				if (regionGateData.commonGateData.GetValue<bool>("singleUse"))
				{
					return self.GetData().used;
				}
				else
				{
					return false;
				}
			}
			return false;
		});


		cursor.Emit(OpCodes.Brtrue, label);


		// Matches
		// 178 if (this.startCounter > 60)
		cursor.GotoNext(MoveType.After,
			x => x.MatchLdarg(0),
			x => x.MatchLdfld<RegionGate>(nameof(RegionGate.startCounter)),
			x => x.MatchLdcI4(60),
			x => x.MatchBle(out _)
			);

		cursor.Emit(OpCodes.Ldarg_0);

		cursor.EmitDelegate<Action<RegionGate>>((self) =>
		{
			self.GetData().used = true;
		});
	}

	// Dunno why I documented this function, wanted to try it I guess
	/// <summary>
	/// Returns the <see cref = "ManagedData"/> of the first found <see cref="PlacedObject"/> with the name of <paramref name="placedObjectName"/> in the specified <see cref="Room"/>. If no <see cref="PlacedObject"/> is found it returns <see langword="null"/>.
	/// </summary>
	/// <param name="room"></param>
	/// <param name="placedObjectName">The name of the <see cref="PlacedObject"/></param>
	public static ManagedData? GetPlacedObjectData(Room room, string placedObjectName)
	{
		for (int i = 0; i < room.roomSettings.placedObjects.Count; i++)
		{
			if (room.roomSettings.placedObjects[i].type == new PlacedObject.Type(placedObjectName, false))
			{
				return room.roomSettings.placedObjects[i].data as ManagedData;
			}
		}
		return null;
	}
}

