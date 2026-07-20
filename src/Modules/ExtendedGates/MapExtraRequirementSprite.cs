using HUD;
using Menu;

namespace RegionKit.Modules.ExtendedGates
{
	public class MapExtraRequirementSprite : Map.FadeInMarker
	{
		public ExtraRequirement requirement;
		public Map.GateMarker referenceMarker;
		protected Vector2 offsetFromMarker;
		protected bool showAsCompleted;

		private bool lastVisible;

		protected virtual FAtlasElement GetSprite() => requirement.SpriteElement;

		protected SaveState? ActualSaveState
		{
			get
			{
				if (map.hud.owner is UpdatableAndDeletable uad && uad.room != null && uad.room.game.IsStorySession)
				{
					return uad.room.game.GetStorySession.saveState;
				}
				return map.GetSaveState();
			}
		}

		protected virtual bool FadeRed => !showAsCompleted;

		public MapExtraRequirementSprite(Map.GateMarker referenceMarker, ExtraRequirement requirement, Vector2 offsetFromMarker) : base(referenceMarker.map, referenceMarker.room, referenceMarker.inRoomPos, 3f)
		{
			this.referenceMarker = referenceMarker;
			this.requirement = requirement;
			this.offsetFromMarker = offsetFromMarker;

			SaveState? saveState = ActualSaveState;
			if (saveState != null)
			{
				showAsCompleted = requirement.Completed(saveState);
			}


			symbolSprite = new FSprite(GetSprite())
			{
				isVisible = false
			};
			referenceMarker.map.inFrontContainer.AddChild(symbolSprite);
		}

		public override void Update()
		{
			base.Update();
			if (lastVisible != map.visible)
			{
				SaveState? saveState = ActualSaveState;
				if (saveState != null)
				{
					showAsCompleted = requirement.Completed(saveState);
				}
			}
			lastVisible = map.visible;
			inRoomPos = referenceMarker.inRoomPos;
		}

		public override void Draw(float timeStacker)
		{
			base.Draw(timeStacker);
			bkgFade.isVisible = map.visible;
			symbolSprite.isVisible = map.visible;
			if (!map.visible)
			{
				return;
			}
			float f = Mathf.Lerp(map.lastFade, map.fade, timeStacker) * Mathf.Lerp(lastFade, fade, timeStacker);
			Vector2 pos = map.RoomToMapPos(inRoomPos, room, timeStacker) + offsetFromMarker;
			bkgFade.x = pos.x;
			bkgFade.y = pos.y;
			bkgFade.alpha = f * 0.25f;
			symbolSprite.x = pos.x;
			symbolSprite.y = pos.y;
			symbolSprite.alpha = f;
			symbolSprite.color = Color.Lerp(FadeRed ? new Color(1f, 0f, 0f) : MenuColorEffect.rgbDarkGrey, MenuColorEffect.rgbWhite, 0.5f + 0.5f * Mathf.Sin((map.counter + timeStacker) / 14f));
			symbolSprite.scale = requirement.SpriteScale(0f) * 0.8f;
			bkgFade.scale = 30f / 8f;
		}
	}
}
