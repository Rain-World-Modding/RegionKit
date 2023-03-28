//todo: spawn positions don't work
//todo: most settings don't work
namespace RegionKit.Modules.ShelterBehaviors;

public class HoldToTriggerTutorialObject : UpdatableAndDeletable
{
	///<inheritdoc/>
	public HoldToTriggerTutorialObject(Room room, PlacedObject pObj)
	{
		this.room = room;
		_placedObject = pObj;
		_placedObjectIndex = room.roomSettings.placedObjects.IndexOf(pObj);
		if (room.game.Players.Count == 0) this.Destroy();
		// player loaded in room
		foreach (var p in room.game.Players)
		{
			if (p.pos.room == room.abstractRoom.index)
			{
				this.Destroy();
			}
		}

		// recently displayed
		if (room.game.session is StoryGameSession)
		{
			if ((room.game.session as StoryGameSession)!.saveState.ItemConsumed(room.world, false, room.abstractRoom.index, _placedObjectIndex))
			{
				this.Destroy();
			}
		}

	}
	///<inheritdoc/>
	public override void Update(bool eu)
	{
		base.Update(eu);
		if (base.slatedForDeletetion) return;
		if (this.room.game.session.Players.Count < 1 || this.room.game.cameras.Length < 1) return;
		if (!room.BeingViewed) _message = 0;
		else if (this.room.game.session.Players[0].realizedCreature != null && this.room.game.cameras[0].hud != null && this.room.game.cameras[0].hud.textPrompt != null && this.room.game.cameras[0].hud.textPrompt.messages.Count < 1)
		{
			switch (this._message)
			{
			case 0:
				this.room.game.cameras[0].hud.textPrompt.AddMessage(this.room.game.manager.rainWorld.inGameTranslator.Translate("This place is safe from the rain and most predators"), 20, 160, true, true);
				this._message++;
				break;
			case 1:
				this.room.game.cameras[0].hud.textPrompt.AddMessage(this.room.game.manager.rainWorld.inGameTranslator.Translate("With enough food, hold DOWN to hibernate"), 40, 160, false, true);
				this._message++;
				break;
			default:
				this.Consume();
				break;
			}
		}
	}
	private int _message;
	private PlacedObject _placedObject;
	private int _placedObjectIndex;
	/// <summary>
	/// Consumes the shelter, making it broken for the next few cycles
	/// </summary>
	public void Consume()
	{
		Debug.Log("CONSUMED: HoldToTriggerTutorialObject ;)");
		if (room.world.game.session is StoryGameSession)
		{
			(room.world.game.session as StoryGameSession)!.saveState.ReportConsumedItem(room.world, false, room.abstractRoom.index, this._placedObjectIndex, (_placedObject.data as HoldToTriggerTutorialData)!.cooldown);
		}
		this.Destroy();
	}
}
