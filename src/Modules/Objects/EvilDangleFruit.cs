﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MoreSlugcats;

namespace RegionKit.Modules.Objects
{
	public static class EvilDangleFruit
	{
		public const string EVIL_DANGLE_FRUIT_IDENTIFIER = "EVILMODE";

		internal static void Apply()
		{
			try
			{
				On.Room.Loaded += Room_Loaded;
				IL.PlayerSessionRecord.AddEat += PlayerSessionRecord_AddEat;
				On.SlugcatStats.NourishmentOfObjectEaten += SlugcatStats_NourishmentOfObjectEaten;
				On.SLOracleBehaviorHasMark.TypeOfMiscItem += SLOracleBehaviorHasMark_TypeOfMiscItem;
				On.SLOracleBehaviorHasMark.MoonConversation.AddEvents += MoonConversation_AddEvents;
			}
			catch (Exception e)
			{
				LogError(e);
			}
		}

		internal static void Undo()
		{
			try
			{
				On.Room.Loaded -= Room_Loaded;
				IL.PlayerSessionRecord.AddEat -= PlayerSessionRecord_AddEat;
				On.SlugcatStats.NourishmentOfObjectEaten -= SlugcatStats_NourishmentOfObjectEaten;
				On.SLOracleBehaviorHasMark.TypeOfMiscItem -= SLOracleBehaviorHasMark_TypeOfMiscItem;
				On.SLOracleBehaviorHasMark.MoonConversation.AddEvents -= MoonConversation_AddEvents;
			}
			catch (Exception e)
			{
				LogError(e);
			}
		}

		public static bool IsEvilDangleFruit(this AbstractPhysicalObject abstractPhysicalObject)
		{
			return abstractPhysicalObject.type == AbstractPhysicalObject.AbstractObjectType.DangleFruit
				&& abstractPhysicalObject is DangleFruit.AbstractDangleFruit { rotted: false, unrecognizedAttributes: not null }
				&& abstractPhysicalObject.unrecognizedAttributes.Contains(EVIL_DANGLE_FRUIT_IDENTIFIER, StringComparer.OrdinalIgnoreCase);
		}

		private static void Room_Loaded(On.Room.orig_Loaded orig, Room self)
		{
			bool firstTimeRealized = self.abstractRoom.firstTimeRealized;
			orig(self);
			if (self.game != null)
			{
				List<PlacedObject> objs = self.roomSettings.placedObjects;
				for (int i = 0; i < objs.Count; i++)
				{
					PlacedObject obj = objs[i];
					if (obj.active && firstTimeRealized && obj.type == _Enums.EvilDangleFruit && (self.game.session is not StoryGameSession sgs || !sgs.saveState.ItemConsumed(self.world, false, self.abstractRoom.index, i)))
					{
						var abstrFruit = new DangleFruit.AbstractDangleFruit(self.world, null, self.GetWorldCoordinate(obj.pos), self.game.GetNewID(), self.abstractRoom.index, i, false, obj.data as PlacedObject.ConsumableObjectData) { isConsumed = false };
						if (abstrFruit.unrecognizedAttributes == null)
						{
							abstrFruit.unrecognizedAttributes = [EvilDangleFruit.EVIL_DANGLE_FRUIT_IDENTIFIER];
						}
						else
						{
							Array.Resize(ref abstrFruit.unrecognizedAttributes, abstrFruit.unrecognizedAttributes.Length + 1);
							abstrFruit.unrecognizedAttributes[abstrFruit.unrecognizedAttributes.Length - 1] = EvilDangleFruit.EVIL_DANGLE_FRUIT_IDENTIFIER;
						}
						self.abstractRoom.AddEntity(abstrFruit);
					}
				}
			}
		}

		private static int SlugcatStats_NourishmentOfObjectEaten(On.SlugcatStats.orig_NourishmentOfObjectEaten orig, SlugcatStats.Name slugcatIndex, IPlayerEdible eatenobject)
		{
			if (eatenobject is DangleFruit fruit && fruit.abstractPhysicalObject.IsEvilDangleFruit())
			{
				try
				{
					// I tried to IL hook it, that didn't work so we're going to pretend it's a fly and if that crashes then oh well
					var fakeCrit = new AbstractCreature(null, StaticWorld.GetCreatureTemplate(CreatureTemplate.Type.Fly), null, default, default);
					var fakeFly = new Fly(fakeCrit, null);
					return orig(slugcatIndex, fakeFly);
				}
				catch { }
			}
			return orig(slugcatIndex, eatenobject);
		}

		private static void PlayerSessionRecord_AddEat(ILContext il)
		{
			var c = new ILCursor(il);
			c.GotoNext(x => x.MatchLdsfld<AbstractPhysicalObject.AbstractObjectType>(nameof(AbstractPhysicalObject.AbstractObjectType.EggBugEgg)));
			c.GotoNext(MoveType.AfterLabel, x => x.MatchBrfalse(out _));
			c.Emit(OpCodes.Ldarg_1);
			c.EmitDelegate((bool orig, PhysicalObject obj) => orig || IsEvilDangleFruit(obj.abstractPhysicalObject));
		}

		private static SLOracleBehaviorHasMark.MiscItemType SLOracleBehaviorHasMark_TypeOfMiscItem(On.SLOracleBehaviorHasMark.orig_TypeOfMiscItem orig, SLOracleBehaviorHasMark self, PhysicalObject testItem)
		{
			if (IsEvilDangleFruit(testItem.abstractPhysicalObject))
			{
				return _Enums.EvilDangleFruitDialogue;
			}
			return orig(self, testItem);
		}

		private static void MoonConversation_AddEvents(On.SLOracleBehaviorHasMark.MoonConversation.orig_AddEvents orig, SLOracleBehaviorHasMark.MoonConversation self)
		{
			orig(self);
			if (self.id == Conversation.ID.Moon_Misc_Item && self.describeItem == _Enums.EvilDangleFruitDialogue)
			{
				self.events.Add(new Conversation.TextEvent(self, 10, self.Translate("It's the fruiting body of a relatively common plant. You should eat it, it's delicious!"), 0));
			}
		}
	}
}
