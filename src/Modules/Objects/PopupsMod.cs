using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Pom.Pom;

namespace RegionKit.Modules.Objects;

internal static class PopupsMod
{
	public static void Register()
	{
		var settings = new List<ManagedField>
		{
			new StringField("message", "message", displayName:"Message"),
			new FloatField("delay", 0, 60, 0, displayName:"Delay Seconds"),
			new FloatField("duration", 1, 60, 6, displayName:"Duration Seconds"),
			new IntegerField("entrance", -1, 20, -1, displayName:"Entrance Requirement"),
			new IntegerField("karma", 1, 10, 1, displayName:"Karma Requirement"),
			new BooleanField("darken", true, displayName:"Darken Screen"),
			new BooleanField("hidehud", true, displayName:"Hide HUD"),
			new IntegerField("cooldown", -1, 40, 1, displayName:"Cooldown Cycles"),
		};

		RegisterFullyManagedObjectType(settings.ToArray(), typeof(PopupTrigger), "RoomPopupTrigger", RK_POM_CATEGORY);

		settings.Add(new Vector2Field("handle", new Vector2(-100, 40), Vector2Field.VectorReprType.circle));
		RegisterFullyManagedObjectType(settings.ToArray(), typeof(ResizeablePopupTrigger), "ResizeablePopupTrigger", RK_POM_CATEGORY);
		settings.Pop();

		settings.Add(new Vector2Field("handle", new Vector2(40, 60), Vector2Field.VectorReprType.rect));
		RegisterFullyManagedObjectType(settings.ToArray(), typeof(RectanglePopupTrigger), "RectanglePopupTrigger", RK_POM_CATEGORY);
		settings.Pop();
	}


	public class PopupTrigger : UAD
	{
		protected PlacedObject pObj;
		protected ManagedData data => pObj.data as ManagedData;
		protected int placedObjectIndex;
		protected bool queuedUp;
		private int delay;

		public PopupTrigger(Room room, PlacedObject pObj)
		{
			this.room = room;
			this.pObj = pObj;
			placedObjectIndex = room.roomSettings.placedObjects.IndexOf(pObj);

			if (data.GetValue<int>("cooldown") != 0 && room.game.session is StoryGameSession)
			{
				if ((room.game.session as StoryGameSession)!.saveState.ItemConsumed(room.world, false, room.abstractRoom.index, placedObjectIndex))
				{
					Destroy();
				}
			}
			delay = Mathf.RoundToInt(data.GetValue<float>("delay") * 40f); // implements own delay because message system is weird
		}

		public override void Update(bool eu)
		{
			base.Update(eu);
			if (slatedForDeletetion || !room.BeingViewed || !ShouldFire())
			{
				delay = Mathf.RoundToInt(data.GetValue<float>("delay") * 40f);
				return;
			}
			else delay--;

			if (delay <= 0 && room.game.session.Players[0].realizedCreature != null && room.game.cameras[0].hud != null && room.game.cameras[0].hud.textPrompt != null && room.game.cameras[0].hud.textPrompt.messages.Count < 1)
			{
				room.game.cameras[0].hud.textPrompt.AddMessage(room.game.manager.rainWorld.inGameTranslator.Translate(data.GetValue<string>("message")), 0, Mathf.RoundToInt(data.GetValue<float>("duration") * 40f), data.GetValue<bool>("darken"), data.GetValue<bool>("hidehud"));
				Consume();
			}
		}

		public virtual bool ShouldFire()
		{
			// Any logic, could be reworked into any/all option
			for (int i = 0; i < room.game.Players.Count; i++)
			{
				int entrance = data.GetValue<int>("entrance");
				if (room.game.Players[i].Room == room.abstractRoom
				&& (entrance < 0 || room.game.Players[i].pos.abstractNode == entrance)
				&& room.game.Players[i].realizedCreature != null
				&& !room.game.Players[i].realizedCreature.inShortcut
				&& (room.game.Players[i].realizedCreature as Player)?.Karma >= data.GetValue<int>("karma") - 1
				&& !room.game.GameOverModeActive) return true;
			}
			return false;
		}

		public virtual void Consume()
		{
			Debug.Log("CONSUMED: PopupObject");
			if (data.GetValue<int>("cooldown") != 0 && room.world.game.session is StoryGameSession)
			{
				(room.world.game.session as StoryGameSession)!.saveState.ReportConsumedItem(room.world, false, room.abstractRoom.index, placedObjectIndex, data.GetValue<int>("cooldown"));
			}
			Destroy();
		}
	}

	class ResizeablePopupTrigger : PopupTrigger
	{
		public ResizeablePopupTrigger(Room room, PlacedObject pObj) : base(room, pObj)
		{
		}
		public override bool ShouldFire()
		{
			if (!base.ShouldFire()) return false;
			for (int i = 0; i < room.game.Players.Count; i++)
			{
				if (DistLess(room.game.Players[i].realizedCreature.mainBodyChunk.pos, pObj.pos, data.GetValue<Vector2>("handle").magnitude))
					return true;
			}
			return false;
		}
	}

	class RectanglePopupTrigger : PopupTrigger
	{
		public RectanglePopupTrigger(Room room, PlacedObject pObj) : base(room, pObj)
		{
		}
		public override bool ShouldFire()
		{
			if (!base.ShouldFire()) return false;
			var rect = new Rect(pObj.pos.x, pObj.pos.y, data.GetValue<Vector2>("handle").x, data.GetValue<Vector2>("handle").y);

			for (int i = 0; i < room.game.Players.Count; i++)
			{
				if (rect.Contains(room.game.Players[i].realizedCreature.mainBodyChunk.pos))
					return true;
			}
			return false;
		}
	}
}
