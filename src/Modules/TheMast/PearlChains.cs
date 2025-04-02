using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using DevInterface;
using MonoMod.RuntimeDetour;
using RWCustom;
using UnityEngine;
using Random = UnityEngine.Random;


//Made by Slime_Cubed and Doggo
namespace RegionKit.Modules.TheMast
{

	internal static class PearlChains
	{
		public static void Apply()
		{
			On.DevInterface.ConsumableRepresentation.ctor += ConsumableRepresentation_ctor;
			On.DevInterface.ObjectsPage.CreateObjRep += ObjectsPage_CreateObjRep;
			On.Room.Loaded += Room_Loaded;
			On.PlacedObject.GenerateEmptyData += PlacedObject_GenerateEmptyData;
			On.Player.TossObject += Player_TossObject;
			On.Player.CanBeSwallowed += Player_CanBeSwallowed;
			On.SLOracleBehaviorHasMark.TypeOfMiscItem += SLOracleBehaviorHasMark_TypeOfMiscItem;
			On.SLOracleBehaviorHasMark.MoonConversation.AddEvents += MoonConversation_AddEvents;
			new Hook(
				typeof(Player).GetMethod("Grabability", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance),
				typeof(PearlChains).GetMethod(nameof(Player_Grabability), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static).MakeGenericMethod(Type.GetType("Player+ObjectGrabability, Assembly-CSharp"))
			);
			On.ScavengerAI.CollectScore_PhysicalObject_bool += ScavengerAI_CollectScore_1;
			On.ItemSymbol.SpriteNameForItem += ItemSymbol_SpriteNameForItem;
			On.RainWorld.Start += RainWorld_Start;
			On.SaveState.AbstractPhysicalObjectFromString += SaveState_AbstractPhysicalObjectFromString;
		}

		private static void RainWorld_Start(On.RainWorld.orig_Start orig, RainWorld self)
		{
			orig(self);

			// Make sure sprites are loaded
			CustomAtlases.FetchAtlas("TheMast");
		}

		private static string ItemSymbol_SpriteNameForItem(On.ItemSymbol.orig_SpriteNameForItem orig, AbstractPhysicalObject.AbstractObjectType itemType, int intData)
		{
			if (itemType == _Enums.PearlChain)
				return "assets/regionkit/sprites/symbol_pearlchain";
			else
				return orig(itemType, intData);
		}

		private static int ScavengerAI_CollectScore_1(On.ScavengerAI.orig_CollectScore_PhysicalObject_bool orig, ScavengerAI self, PhysicalObject obj, bool weaponFiltered)
		{
			if (obj is PearlChain pc)
			{
				// Don't take pearl chains down from the ceiling
				if (!(pc.abstractPhysicalObject as AbstractPearlChain)!.isConsumed)
				{
					return 0;
				}
				// Don't take items offered to other scavengers
				if (self.scavenger.room != null)
				{
					SocialEventRecognizer.OwnedItemOnGround ownedItemOnGround = self.scavenger.room.socialEventRecognizer.ItemOwnership(obj);
					if (ownedItemOnGround != null && ownedItemOnGround.offeredTo != null && ownedItemOnGround.offeredTo != self.scavenger)
					{
						return 0;
					}
				}
				// Don't take if a weapon is wanted
				if (weaponFiltered && self.NeedAWeapon)
				{
					return 0;
				}
				return 8;
			}
			return orig(self, obj, weaponFiltered);
		}

		public static T Player_Grabability<T>(Func<Player, PhysicalObject, T> orig, Player self, PhysicalObject obj)
		{
			if (obj is PearlChain) return (T)(object)1;
			return (T)orig.Method.Invoke(null, new object[] { self, obj });
		}

		public static AbstractPhysicalObject? SaveState_AbstractPhysicalObjectFromString(On.SaveState.orig_AbstractPhysicalObjectFromString orig, World world, string objString)
		{
			//TODO: almost definitely breaks
			//nevermind??
			AbstractPhysicalObject? apo = orig(world, objString);
			if (apo is not null && apo.type == _Enums.PearlChain)
			{
				try
				{
					string[] data = Regex.Split(objString, "<oA>");
					apo = new AbstractPearlChain(world, _Enums.PearlChain, null!, apo.pos, apo.ID, int.Parse(data[3]), int.Parse(data[4]), null!, int.Parse(data[5]));
				}
				catch (Exception e)
				{
					LogError(new Exception("Failed to load PearlChain", e));
					apo = null;
				}
			}
			return apo;
		}

		public static void MoonConversation_AddEvents(On.SLOracleBehaviorHasMark.MoonConversation.orig_AddEvents orig, SLOracleBehaviorHasMark.MoonConversation self)
		{
			orig(self);
			if (self.id == Conversation.ID.Moon_Misc_Item)
			{
				InGameTranslator.LanguageID lang = Custom.rainWorld.inGameTranslator.currentLanguage;
				if (self.describeItem == _Enums.MiscItemPearlChain)
				{
					self.events.Add(new Conversation.TextEvent(self, 10, Translator.GetString("PearlChain-1", lang), 0));
					self.events.Add(new Conversation.TextEvent(self, 30, Translator.GetString("PearlChain-2", lang), 0));
					//self.events.Add(new Conversation.TextEvent(self, 10, "Some data pearls with holes burned through their centers, tied with some sort<LINE>of twined plant fiber. I can't read the pearls, they're far too damaged.", 0));
					//self.events.Add(new Conversation.TextEvent(self, 30, "Did you get this from the scavengers? I'd advise that you don't take pearls from them,<LINE><PLAYERNAME> - they value them quite highly and can be dangerous if provoked.", 0));
				}
				else if (self.describeItem == _Enums.MiscItemSinglePearlChain)
				{
					self.events.Add(new Conversation.TextEvent(self, 10, Translator.GetString("SinglePearlChain-1", lang), 0));
					self.events.Add(new Conversation.TextEvent(self, 30, Translator.GetString("SinglePearlChain-2", lang), 0));
					//self.events.Add(new Conversation.TextEvent(self, 10, "A data pearl with a hole burned through its center, tied with some sort<LINE>of twined plant fiber. I can't read the pearl, it's far too damaged.", 0));
					//self.events.Add(new Conversation.TextEvent(self, 30, "Did you get this from the scavengers? I'd advise that you don't take pearls from them,<LINE><PLAYERNAME> - they value them quite highly and can be dangerous if provoked.", 0));
				}
				else return;
				self.State.miscItemsDescribed.Add(_Enums.MiscItemSinglePearlChain);//[//][(int)EnumExt_PearlChains.MiscItemSinglePearlChain] = true;
				self.State.miscItemsDescribed.Add(_Enums.MiscItemPearlChain);//[(int)EnumExt_PearlChains.MiscItemPearlChain]// = true;
			}
		}

		public static SLOracleBehaviorHasMark.MiscItemType SLOracleBehaviorHasMark_TypeOfMiscItem(On.SLOracleBehaviorHasMark.orig_TypeOfMiscItem orig, SLOracleBehaviorHasMark self, PhysicalObject testItem)
		{
			if (testItem is PearlChain pc) return (pc.pearlCount == 1) ? _Enums.MiscItemSinglePearlChain : _Enums.MiscItemPearlChain;
			return orig(self, testItem);
		}

		public static bool Player_CanBeSwallowed(On.Player.orig_CanBeSwallowed orig, Player self, PhysicalObject testObj)
		{
			return (testObj is PearlChain) || orig(self, testObj);
		}

		public static void Player_TossObject(On.Player.orig_TossObject orig, Player self, int grasp, bool eu)
		{
			PhysicalObject tossObj = self.grasps[grasp].grabbed;
			if (tossObj is PearlChain pc)
			{
				pc.Tossed();
				if (pc.bodyChunks.Length > 1)
				{
					Vector2 endPos = tossObj.bodyChunks[1].pos;
					Vector2 midPos = tossObj.bodyChunks[2].pos;
					orig(self, grasp, eu);
					tossObj.bodyChunks[1].pos = endPos;
					tossObj.bodyChunks[2].pos = midPos;
				}
				else
					orig(self, grasp, eu);
			}
			else
				orig(self, grasp, eu);
		}

		public static void PlacedObject_GenerateEmptyData(On.PlacedObject.orig_GenerateEmptyData orig, PlacedObject self)
		{
			if (self.type == _Enums.PlacedPearlChain)
				self.data = new PlacedPearlChainData(self);
			else
				orig(self);
		}

		// Create pearl chains corresponding to placed objects
		public static void Room_Loaded(On.Room.orig_Loaded orig, Room self)
		{
			if (self.game == null)
			{
				orig(self);
				return;
			}

			bool firstTimeRealized = self.abstractRoom.firstTimeRealized;
			orig(self);
			if (firstTimeRealized)
			{
				for (int i = 0; i < self.roomSettings.placedObjects.Count; i++)
				{
					PlacedObject pObj = self.roomSettings.placedObjects[i];
					if (!pObj.active) continue;
					if (pObj.type != _Enums.PlacedPearlChain) continue;
					if (self.world.regionState?.ItemConsumed(self.abstractRoom.index, i) ?? false) continue;
					AbstractPearlChain apo = new AbstractPearlChain(self.world, _Enums.PearlChain, null!, self.GetWorldCoordinate(pObj.pos), self.game.GetNewID(), self.abstractRoom.index, i, (pObj.data as PlacedPearlChainData)!, (pObj.data as PlacedPearlChainData)?.length ?? 0);
					apo.isConsumed = false;
					self.abstractRoom.AddEntity(apo);
				}
			}
		}

		public static void ObjectsPage_CreateObjRep(On.DevInterface.ObjectsPage.orig_CreateObjRep orig, ObjectsPage self, PlacedObject.Type tp, PlacedObject pObj)
		{
			if (tp == _Enums.PlacedPearlChain)
			{
				// From ObjectsPage.CreateObjRep
				if (pObj == null)
				{
					pObj = new PlacedObject(tp, null);
					pObj.pos = self.owner.room.game.cameras[0].pos + Vector2.Lerp(self.owner.mousePos, new Vector2(-683f, 384f), 0.25f) + Custom.DegToVec(Random.value * 360f) * 0.2f;
					self.RoomSettings.placedObjects.Add(pObj);
				}
				ConsumableRepresentation rep = new ConsumableRepresentation(self.owner, tp.ToString() + "_Rep", self, pObj, tp.ToString());
				self.tempNodes.Add(rep);
				self.subNodes.Add(rep);
			}
			else
				orig(self, tp, pObj);
		}

		private static FieldInfo _ConsumableRepresentation_controlPanel = typeof(ConsumableRepresentation).GetField("controlPanel", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
		public static void ConsumableRepresentation_ctor(On.DevInterface.ConsumableRepresentation.orig_ctor orig, ConsumableRepresentation self, DevUI owner, string IDstring, DevUINode parentNode, PlacedObject pObj, string name)
		{
			orig(self, owner, IDstring, parentNode, pObj, name);
			if (pObj.type == _Enums.PlacedPearlChain)
			{
				// Replace the default control panel with one specific to pearl chains
				var cp = (ConsumableRepresentation.ConsumableControlPanel)_ConsumableRepresentation_controlPanel.GetValue(self);
				self.subNodes.Remove(cp);
				cp.ClearSprites();
				cp = new PearlChainControlPanel(owner, "Consumable_Panel", self, new Vector2(0f, 100f), "Consumable: PlacedPearlChain");
				self.subNodes.Add(cp);
				cp.pos = (pObj.data as PlacedObject.ConsumableObjectData)!.panelPos;
				_ConsumableRepresentation_controlPanel.SetValue(self, cp);
			}
			//System.Collections.
		}

		

		public class PearlChain : PlayerCarryableItem, IDrawable
		{
			public int pearlCount;
			public bool canBeStolen;

			private int[] _pearlColors;

			private float _darkness;
			private float[,] _gleams;
			private float _gleamProg;
			private int _gleamWait;
			private float _gleamSpeed;
			private float _swallowed;
			private int _tossedCounter;
			const float chainRadius = 1f;

			private const float _pearlSpacing = 10f;
			private AbstractConsumable Consumable => (abstractPhysicalObject as AbstractConsumable)!;

			public PearlChain(AbstractPearlChain apc, bool attached) : base(apc)
			{
				// TODO: Allow both chunks of the chain to be grabbed
				// TODO: Define scavenger interactions
				var state = UnityEngine.Random.state;
				UnityEngine.Random.InitState(apc.ID.RandomSeed);
				pearlCount = Random.Range(2, 5);
				if (apc.length > 0) pearlCount = apc.length;
				_pearlColors = new int[pearlCount];

				// Init main body chunks
				if (pearlCount > 1)
				{
					bodyChunks = new BodyChunk[3];
					bodyChunkConnections = new BodyChunkConnection[3];
					float mass = 0.07f * Math.Min(pearlCount, 4) / 2f;
					for (int i = 0; i < 2; i++)
						bodyChunks[i] = new BodyChunk(this, i, Vector2.zero, 5f, mass);
					bodyChunkConnections[0] = new BodyChunkConnection(bodyChunks[0], bodyChunks[1], _pearlSpacing * (pearlCount - 1), BodyChunkConnection.Type.Pull, 0.95f, -1f);
					// Init midpoint chunk
					bodyChunks[2] = new BodyChunk(this, 2, Vector2.zero, 5f, 0.07f);
					for (int i = 0; i < 2; i++)
						bodyChunkConnections[i + 1] = new BodyChunkConnection(bodyChunks[i], bodyChunks[2], _pearlSpacing / 2f * (pearlCount - 1), BodyChunkConnection.Type.Pull, 0.95f, 0f);
				}
				else
				{
					bodyChunks = new BodyChunk[1];
					bodyChunks[0] = new BodyChunk(this, 0, Vector2.zero, 5f, 0.07f);
					bodyChunkConnections = new BodyChunkConnection[0];
				}

				_gleams = new float[pearlCount, 2];
				for (int i = 0; i < pearlCount; i++)
					_pearlColors[i] = PearlVars.RandomColor();

				// Init physical properties
				airFriction = 0.999f;
				gravity = 0.9f;
				bounce = 0.4f;
				surfaceFriction = 0.4f;
				collisionLayer = 2;
				waterFriction = 0.98f;
				buoyancy = 0.4f;
				for (int i = 0; i < bodyChunks.Length; i++)
					bodyChunks[i].loudness = 3f;

				Random.state = state;
			}

			public override void Update(bool eu)
			{
				base.Update(eu);
				if (_gleamWait <= 0)
				{
					_gleamWait = Random.Range(40, 80);
					_gleamSpeed = 1f / Random.Range(5f, 15f);
					_gleamProg = 0f;
				}
				else
				{
					_gleamProg = Mathf.Min(2f, _gleamProg + _gleamSpeed);
					if (_gleamProg >= 2f)
						_gleamWait--;
				}
				for (int i = 0; i < pearlCount; i++)
				{
					_gleams[i, 1] = _gleams[i, 0];
					_gleams[i, 0] = Mathf.Sin(Mathf.Max(_gleamProg - i / Mathf.Max(15f, _gleams.Length + 1)) * Mathf.PI) * Random.value;
				}

				bool beingSwallowed = false;
				if (grabbedBy.Count > 0 && grabbedBy[0].grabber is Player ply && ply.swallowAndRegurgitateCounter > 60 && ply.objectInStomach == null && ply.input[0].pckp)
				{
					int itemToSwallow = -1;
					for (int i = 0; i < 2; i++)
						if (ply.grasps[i] != null && ply.CanBeSwallowed(ply.grasps[i].grabbed))
						{
							itemToSwallow = i;
							break;
						}
					if (itemToSwallow > -1 && ply.grasps[itemToSwallow]?.grabbed == this)
						beingSwallowed = true;
				}

				_swallowed = Custom.LerpAndTick(_swallowed, beingSwallowed ? 1f : 0f, 0.05f, 0.05f);
				if (_swallowed > 0f && !Consumable.isConsumed)
					Consumable.Consume();

				CheckTheft();

				// Keep anchor from snapping shortly after being thrown
				if (_tossedCounter > 0)
				{
					_tossedCounter--;
					if (grabbedBy.Count > 0) _tossedCounter = 0;
				}

				// Don't collide with other items
				CollideWithObjects = grabbedBy.Count == 0;
			}

			private bool _lastGrabbed;
			private void CheckTheft()
			{
				if (!canBeStolen) return;

				// Make scavengers bristle when a pearl chain is grabbed
				if (!Consumable.isConsumed)
				{
					bool grabbed = false;
					if (grabbedBy.Count > 0)
					{
						if (grabbedBy[0].grabber is Player theif)
						{
							grabbed = true;
							if (!_lastGrabbed)
							{
								List<AbstractCreature> creatures = room.abstractRoom.creatures;
								for (int i = 0; i < creatures.Count; i++)
								{
									AbstractCreature creature = creatures[i];
									if (!(creature.realizedCreature is Scavenger scav)) continue;

									if (Random.value > Custom.LerpMap(creature.personality.nervous + creature.personality.aggression, 0f, 2f, 0.25f, 1f)) return;

									bool canBeSeen = true;
									if (creature.Room.realizedRoom != null)
										canBeSeen = creature.Room.realizedRoom.VisualContact(scav.bodyChunks[2].pos, theif.bodyChunks[0].pos);

									if (scav.Consious && canBeSeen && (scav.graphicsModule is ScavengerGraphics sg))
									{
										sg.ShockReaction(0.75f);
										scav.AI.MakeLookHere(theif.bodyChunks[0].pos);
									}
								}
							}
						}
					}
					_lastGrabbed = grabbed;
					return;
				}

				// Decrease rep if a scavenger sees you grab this item
				// Log this item as stolen
				if (grabbedBy.Count > 0)
				{
					if (grabbedBy[0].grabber is Player theif)
						PlayerStole(theif);
					return;
				}
			}

			public void PlayerStole(Player theif)
			{
				if (!canBeStolen) return;
				Consumable.Consume();
				canBeStolen = false;
				room.socialEventRecognizer.AddStolenProperty(Consumable.ID);
				List<AbstractCreature> creatures = room.abstractRoom.creatures;
				int witnesses = 0;
				for (int i = 0; i < creatures.Count; i++)
				{
					if (creatures[i].creatureTemplate.type == CreatureTemplate.Type.Scavenger && creatures[i].realizedCreature != null && creatures[i].realizedCreature.Consious)
					{
						float likeOfPlayer = room.game.session.creatureCommunities.LikeOfPlayer(CreatureCommunities.CommunityID.Scavengers, room.game.world.RegionNumber, theif.playerState.playerNumber);

						room.game.session.creatureCommunities.InfluenceLikeOfPlayer(CreatureCommunities.CommunityID.Scavengers, room.game.world.RegionNumber, theif.playerState.playerNumber, Custom.LerpMap(likeOfPlayer, -0.5f, 0.9f, -0.2f, -0.1f), 0.5f, 0.0f);
						if (++witnesses >= 4) break;
					}
				}
				if (witnesses > 0)
				{
					LogMessage($"pearl chain theft noticed by {witnesses} scavengers!");
					if (witnesses == 4) LogWarning("Good luck.");
				}
			}

			public override void PlaceInRoom(Room placeRoom)
			{
				base.PlaceInRoom(placeRoom);
				NewRoom(placeRoom);
				if (!Consumable.isConsumed && Consumable.placedObjectIndex >= 0 && Consumable.placedObjectIndex < placeRoom.roomSettings.placedObjects.Count)
				{
					PlacedObject pObj = placeRoom.roomSettings.placedObjects[Consumable.placedObjectIndex];

					// Attach to ceiling
					Vector2 center = pObj.pos;
					for (int i = 0; i < bodyChunks.Length; i++)
						bodyChunks[i].HardSetPosition(center - Vector2.up * (bodyChunks.Length - 1 - i) * _pearlSpacing);

					// Add string connecting chain to ceiling
					room.AddObject(new PearlAnchor(this, center));
					canBeStolen = true;
				}
				else
				{
					// Create on the ground
					Vector2 center = placeRoom.MiddleOfTile(abstractPhysicalObject.pos);
					float rot = Random.value * Mathf.PI * 2f;
					Vector2 dir = new Vector2(Mathf.Cos(rot), Mathf.Sin(rot));
					for (int i = 0; i < bodyChunks.Length; i++)
						bodyChunks[i].HardSetPosition(center + dir * i);
				}
			}

			// Make a sound when hitting a surface
			public override void TerrainImpact(int chunk, IntVector2 direction, float speed, bool firstContact)
			{
				base.TerrainImpact(chunk, direction, speed, firstContact);
				if (firstContact && speed > 2f)
					room.PlaySound(SoundID.SS_AI_Marble_Hit_Floor, firstChunk, false, Custom.LerpMap(speed, 0f, 8f, 0.2f, 1f), 1f);
			}

			public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer? newContatiner)
			{
				if (newContatiner == null) newContatiner = rCam.ReturnFContainer("Items");
				foreach (FSprite sprite in sLeaser.sprites)
					newContatiner.AddChild(sprite);
			}

			public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
			{
				_darkness = rCam.room.Darkness(firstChunk.pos);
				sLeaser.sprites[0].color = palette.blackColor;
			}

			public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
			{
				Vector2 startPos = Vector2.Lerp(bodyChunks[0].lastPos, bodyChunks[0].pos, timeStacker);
				Vector2 endPos = (bodyChunks.Length > 1) ? Vector2.Lerp(bodyChunks[1].lastPos, bodyChunks[1].pos, timeStacker) : startPos;
				Vector2 midPos = (bodyChunks.Length > 1) ? Vector2.Lerp(bodyChunks[2].lastPos, bodyChunks[2].pos, timeStacker) : startPos;
				Vector2 lastPos = new Vector2();

				TriangleMesh cord = (TriangleMesh)sLeaser.sprites[0];

				// Draw pearls
				for (int i = 0; i < pearlCount; i++)
				{
					// Do gleam
					float gleam = Mathf.Lerp(_gleams[i, 1], _gleams[i, 0], timeStacker);
					Color baseCol = PearlVars.GetColor(_pearlColors[i]);
					Color? highlightCol = PearlVars.GetHighlightColor(_pearlColors[i]);
					if (highlightCol == null)
						highlightCol = Custom.RGB2RGBA(baseCol * Mathf.Lerp(1.3f, 0.5f, _darkness), 1f);
					highlightCol = Color.Lerp(highlightCol.Value, Color.white, Mathf.Lerp(0.5f + 0.5f * gleam, 0.2f + 0.8f * gleam, _darkness));
					baseCol = Color.Lerp(Custom.RGB2RGBA(baseCol * Mathf.Lerp(1f, 0.2f, _darkness), 1f), Color.white, gleam);
					sLeaser.sprites[i * 3 + 1].color = baseCol;
					sLeaser.sprites[i * 3 + 2].color = highlightCol.Value;

					// Update glow
					sLeaser.sprites[i * 3 + 3].alpha = gleam * 0.5f;
					sLeaser.sprites[i * 3 + 3].scale = 20f * gleam / 16f;

					float t = i / (float)Math.Max(pearlCount - 1, 1);
					t *= 1f - _swallowed;
					Vector2 pos = LerpUnclamped(LerpUnclamped(startPos, midPos, 2f * t), LerpUnclamped(midPos, endPos, 2f * t - 1f), t);

					// Set position
					sLeaser.sprites[i * 3 + 1].SetPosition(pos - camPos);
					sLeaser.sprites[i * 3 + 2].SetPosition(pos - camPos + new Vector2(-0.5f, 1.5f));
					sLeaser.sprites[i * 3 + 3].SetPosition(pos - camPos);

					// Update cord position
					if (i > 0)
					{
						Vector2 delta = pos - lastPos;
						Vector2 right = new Vector2(delta.y, -delta.x);
						right.Normalize();

						int j = (i - 1) * 4;
						cord.MoveVertice(j, lastPos + right * chainRadius - camPos);
						cord.MoveVertice(j + 1, lastPos - right * chainRadius - camPos);
						cord.MoveVertice(j + 2, pos + right * chainRadius - camPos);
						cord.MoveVertice(j + 3, pos - right * chainRadius - camPos);
					}
					lastPos = pos;
				}

				if (slatedForDeletetion || room != rCam.room)
					sLeaser.CleanSpritesAndRemove();
			}

			private static Vector2 LerpUnclamped(Vector2 a, Vector2 b, float t)
			{
				return a * (1f - t) + b * t;
			}

			public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
			{
				sLeaser.sprites = new FSprite[1 + pearlCount * 3];
				TriangleMesh.Triangle[] tris = new TriangleMesh.Triangle[(pearlCount - 1) * 2];
				for (int i = 0; i < pearlCount - 1; i++)
				{
					int j = i * 4;
					tris[i * 2] = new TriangleMesh.Triangle(j, j + 1, j + 2);
					tris[i * 2 + 1] = new TriangleMesh.Triangle(j + 1, j + 2, j + 3);
				}
				sLeaser.sprites[0] = new TriangleMesh("Futile_White", tris, false);
				for (int i = 0; i < pearlCount; i++)
				{
					sLeaser.sprites[i * 3 + 1] = new FSprite("JetFishEyeA", true);
					sLeaser.sprites[i * 3 + 2] = new FSprite("tinyStar", true);
					sLeaser.sprites[i * 3 + 3] = new FSprite("Futile_White", true)
					{
						shader = rCam.game.rainWorld.Shaders["FlatLightBehindTerrain"]
					};
				}
				AddToContainer(sLeaser, rCam, null);
			}

			public void Tossed()
			{
				_tossedCounter = 30;
			}

			// A chain segment that connects a pearl with the ceiling
			public class PearlAnchor : UpdatableAndDeletable, IDrawable
			{
				public float anchored = 1f;
				public PearlChain chain;
				public Vector2 anchorPos;
				private Vector2 _lastAnchorPos;
				public float length;
				private int _stayAnchored;

				public PearlAnchor(PearlChain chain, Vector2 topPearlPos)
				{
					_stayAnchored = 40;
					this.chain = chain;
					Vector2? attachPoint = SharedPhysics.ExactTerrainRayTracePos(chain.room, topPearlPos, topPearlPos + Vector2.up * 500f);
					if (attachPoint.HasValue)
					{
						anchorPos = attachPoint.Value;
						length = Vector2.Distance(anchorPos, topPearlPos);
					}
					else
					{
						LogMessage("Pearl chain spawned broken!");
						length = -1f;
						anchorPos = topPearlPos;
					}
					_lastAnchorPos = anchorPos;
				}

				public override void Update(bool eu)
				{
					if (_stayAnchored > 0)
						_stayAnchored--;
					_lastAnchorPos = anchorPos;

					if (chain.room == null || chain.room != room)
					{
						chain.Consumable.Consume();
						Destroy();
					}
					if (chain.slatedForDeletetion) Destroy();

					int reps = (_stayAnchored > 0) ? 10 : 1;
					for (int i = 0; i < reps; i++)
					{
						if (i > 0) chain.Update(eu);
						base.Update(eu);
						BodyChunk chunk = chain.bodyChunks[(chain.bodyChunks.Length == 1) ? 0 : 1];
						if (anchored == 1f)
						{
							if (length == -1f ||
								(_stayAnchored == 0 && chain._tossedCounter == 0 && Vector2.Distance(chunk.pos, anchorPos) > length * 1.5f) ||
								chain.Consumable.isConsumed)
							{
								room.AddObject(new ExplosionSpikes(room, anchorPos, 5, 4f, 4f, 4f, 15f, Color.white));
								room.PlaySound(SoundID.Spore_Bee_Spark, anchorPos, 1.3f, 1f);
								anchorPos = Vector2.Lerp(anchorPos, chunk.pos, 0.7f);
								anchored = 0.95f;
								chain.Consumable.Consume();
								length = -1f;
							}
							else
							{
								Vector2 delta = anchorPos - chunk.pos;
								float mag = delta.magnitude;
								if (mag > length) chunk.vel += (mag - length) * (delta / mag) * 0.5f;
								chunk.vel *= 0.97f;
							}
						}
						else
						{
							anchored = Math.Max(0, anchored - 1f / 10f);
							anchorPos = Vector2.Lerp(anchorPos, chunk.pos, 0.3f + 0.7f * Mathf.Pow(1f - anchored, 4f));
							if (anchored == 0f)
								Destroy();
						}
					}
				}

				public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer? newContatiner)
				{
					if (newContatiner == null) newContatiner = rCam.ReturnFContainer("Midground");
					newContatiner.AddChild(sLeaser.sprites[0]);
				}

				public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
				{
					sLeaser.sprites[0].color = palette.blackColor;
				}

				public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
				{
					BodyChunk chunk = chain.bodyChunks[(chain.bodyChunks.Length == 1) ? 0 : 1];
					Vector2 pearlPos = Vector2.Lerp(chunk.lastPos, chunk.pos, timeStacker);
					Vector2 delta = Vector2.Lerp(_lastAnchorPos, anchorPos, timeStacker) - pearlPos;
					FSprite sprite = sLeaser.sprites[0];
					sprite.x = pearlPos.x - camPos.x;
					sprite.y = pearlPos.y - camPos.y;
					sprite.scaleX = delta.magnitude + chainRadius;
					sprite.scaleY = chainRadius * 2f;
					sprite.rotation = -Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;

					if (slatedForDeletetion)
						sLeaser.CleanSpritesAndRemove();
				}

				public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
				{
					sLeaser.sprites = new FSprite[1];
					sLeaser.sprites[0] = new FSprite("pixel")
					{
						anchorX = 0f,
						anchorY = 0.5f
					};
					AddToContainer(sLeaser, rCam, null);
				}
			}

			private static class PearlVars
			{
				private static Color[] _colors = new Color[]
				{
					new Color(0.7f, 0.7f, 0.7f), Color.black,
                    //new Color(1f, 0.6f, 0.9f), Color.white,
                    //new Color(0.2f, 0.75f, 0.2f), new Color(0.45f, 1.0f, 0.45f),
                    //new Color(0.82f, 0.71f, 0.2f), new Color(1.0f, 1.0f, 0.54f),
                    //new Color(0.18f, 0.65f, 0.68f), Color.black
                };
				private static int[] _weights = new int[]
				{
					1//87, 10, 1, 1, 1
                };
				private static int _totalWeight = -1;

				public static Color GetColor(int color) => _colors[(color == -1) ? 0 : color * 2];
				public static Color? GetHighlightColor(int color)
				{
					Color col = _colors[(color == -1) ? 1 : color * 2 + 1];
					if (col == Color.black) return null;
					return col;
				}

				public static int RandomColor()
				{
					if (_totalWeight == -1) InitWeights();
					int color = Array.BinarySearch(_weights, Random.Range(0, _totalWeight + 1));
					if (color < 0) color = ~color;
					return color;
				}

				private static void InitWeights()
				{
					int total = 0;
					// Make _weights into a CDF
					for (int i = 0; i < _weights.Length; i++)
					{
						total += _weights[i];
						_weights[i] = total;
					}
					_totalWeight = total;
				}
			}
		}

		public class AbstractPearlChain : AbstractConsumable
		{
			public int length;

			public AbstractPearlChain(World world, AbstractObjectType type, PhysicalObject realizedObject, WorldCoordinate pos, EntityID ID, int originRoom, int placedObjectIndex, PlacedObject.ConsumableObjectData consumableData, int length) : base(world, type, realizedObject, pos, ID, originRoom, placedObjectIndex, consumableData)
			{
				this.length = length;
			}

			public override void Realize()
			{
				if (realizedObject != null) return;

				// Realize pearl chain
				realizedObject = new PearlChain(this, !isConsumed);

				// Realize all stuck objects
				for (int i = 0; i < stuckObjects.Count; i++)
				{
					if (stuckObjects[i].A.realizedObject == null && stuckObjects[i].A != this)
						stuckObjects[i].A.Realize();
					if (stuckObjects[i].B.realizedObject == null && stuckObjects[i].B != this)
						stuckObjects[i].B.Realize();
				}
			}

			public override string ToString()
			{
				return string.Concat(base.ToString(), "<oA>", length);
			}
		}

		public class PlacedPearlChainData : PlacedObject.ConsumableObjectData
		{
			public int length;

			public PlacedPearlChainData(PlacedObject owner) : base(owner)
			{
			}

			public override string ToString()
			{
				return string.Concat(base.ToString(), "~", length);
			}

			public override void FromString(string s)
			{
				base.FromString(s);
				string[] array = Regex.Split(s, "~");
				if ((array.Length >= 5) && int.TryParse(array[4], out int length))
					this.length = length;
			}
		}

		public class PearlChainControlPanel : ConsumableRepresentation.ConsumableControlPanel, IDevUISignals
		{
			private DevUILabel _dispLength;
			private Button _incLength;
			private Button _decLength;

			public PearlChainControlPanel(DevUI owner, string IDstring, DevUINode parentNode, Vector2 pos, string name) : base(owner, IDstring, parentNode, pos, name)
			{
				size.y = size.y + 20f;
				_dispLength = new DevUILabel(owner, "PearlChain_Length_Label", this, new Vector2(5f, 45f), 150f, string.Empty);
				subNodes.Add(_dispLength);
				_incLength = new Button(owner, "PearlChain_IncLen_Button", this, new Vector2(160f, 45f), 40f, "+");
				subNodes.Add(_incLength);
				_decLength = new Button(owner, "PearlChain_DecLen_Button", this, new Vector2(205f, 45f), 40f, "-");
				subNodes.Add(_decLength);
			}

			public override void Refresh()
			{
				base.Refresh();
				int length = ((parentNode as ConsumableRepresentation)!.pObj.data as PlacedPearlChainData)!.length;
				if (length <= 0)
					_dispLength.Text = "RANDOM";
				else
					_dispLength.Text = length.ToString();
			}

			public void Signal(DevUISignalType type, DevUINode sender, string message)
			{
				PlacedPearlChainData chainData = ((parentNode as ConsumableRepresentation)!.pObj.data as PlacedPearlChainData)!;
				string idstring = sender.IDstring;
				switch (idstring)
				{
				case "PearlChain_IncLen_Button":
					chainData.length++;
					break;
				case "PearlChain_DecLen_Button":
					chainData.length--;
					if (chainData.length < 0) chainData.length = 0;
					break;
				}
				Refresh();
			}
		}
	}
}
