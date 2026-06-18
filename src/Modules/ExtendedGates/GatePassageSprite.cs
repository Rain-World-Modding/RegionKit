using MoreSlugcats;

namespace RegionKit.Modules.ExtendedGates
{
	public class GatePassageSprite : CosmeticSprite
	{
		public bool side;
		public RegionGate gate;
		public GateKarmaGlyph referenceGlyph;
		public WinState.EndgameID passage;
		public WinState.EndgameTracker? passageTracker;

		private FAtlasElement passageSprite;
		private Vector2 offsetFromGlyph;

		private float flicker => referenceGlyph.flicker;
		private float fade;
		private float lastFade;
		private float goalFade
		{
			get { return redSine > 0f ? 1f : referenceGlyph.goalFade; }
			set => referenceGlyph.goalFade = value;
		}
		private Color myDefaultColor => referenceGlyph.myDefaultColor;
		private float sinAdder;
		public bool symbolDirty = true;
		public Color color;
		public Color lastColor;
		private bool extraRedSineActive = false;
		private float _extraRedSine = 0f;
		internal float extraRedSine
		{
			get { return _extraRedSine; }
			set { _extraRedSine = value; extraRedSineActive = true; }
		}
		internal float redSine
		{
			get { return referenceGlyph.redSine + extraRedSine; }
		}

		public Color GetToColor
		{
			get
			{
				if (referenceGlyph.PlayNoEnergyAnimation || referenceGlyph.animationFinished && referenceGlyph.ShouldPlayCitizensIDAnimation() < 0 || FlashRed)
				{
					return Color.Lerp(myDefaultColor, new Color(1f, 0f, 0f), 0.4f + 0.5f * Mathf.Sin(sinAdder / 12f));
				}
				return myDefaultColor;
			}
		}

		private bool FlashRed => !referenceGlyph.PlayNoEnergyAnimation && gate.letThroughDir == side && gate.PlayersInZone() > 0 && (passageTracker == null || !passageTracker.GoalFullfilled);

		public GatePassageSprite(bool side, RegionGate gate, GateKarmaGlyph referenceGlyph, WinState.EndgameID passage, Vector2 offsetFromGlyph)
		{
			this.side = side;
			this.gate = gate;
			this.referenceGlyph = referenceGlyph;
			this.passage = passage;
			room = gate.room;

			passageTracker = room.game.GetStorySession.saveState.deathPersistentSaveData.winState.GetTracker(passage, false);
			if (!Futile.atlasManager.TryGetElementWithName(passage.value + "B", out passageSprite!) || passageSprite == null)
			{
				LogWarning("Could not find passage sprite for " + passage.value + "!");
				passageSprite = Futile.atlasManager.GetElementWithName("Sandbox_QuestionMark");
			}

			this.offsetFromGlyph = offsetFromGlyph;
			pos = referenceGlyph.pos + offsetFromGlyph;
			lastPos = pos;
			color = GetToColor;
			lastColor = color;
			gate.room.AddObject(this);
		}

		public override void Update(bool eu)
		{
			base.Update(eu);
			if (gate.unlocked || slatedForDeletetion)
			{
				Destroy();
				return;
			}

			pos = referenceGlyph.pos + offsetFromGlyph;
			lastFade = fade;
			fade = LerpAndTick(fade, Mathf.Min(goalFade, 1f - flicker), 0.01f, 0.05f);
			lastColor = color;
			color = Color.Lerp(color, GetToColor, 0.2f);
			if (referenceGlyph.requirement == RegionGate.GateRequirement.DemoLock || ModManager.MSC && referenceGlyph.requirement == MoreSlugcatsEnums.GateRequirement.OELock || redSine > 0f)
			{
				color = referenceGlyph.col;
			}
			if (referenceGlyph.PlayNoEnergyAnimation || FlashRed)
			{
				sinAdder += 1f;
			}

			if (_extraRedSine > 0f && !extraRedSineActive)
			{
				float twoPi = 2 * Mathf.PI;
				float wrappedPhase = _extraRedSine / 25f % twoPi;
				if (wrappedPhase < 0) wrappedPhase += twoPi;
				_extraRedSine = wrappedPhase * 25f;
				_extraRedSine -= 3f;
				if (_extraRedSine < 0f) _extraRedSine = 0f;
			}
			extraRedSineActive = false;
		}

		public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
		{
			sLeaser.sprites = new FSprite[2];
			sLeaser.sprites[0] = new FSprite("Futile_White", true)
			{
				shader = rCam.game.rainWorld.Shaders["LightSource"]
			};
			sLeaser.sprites[1] = new FSprite("pixel", true)
			{
				shader = rCam.game.rainWorld.Shaders["GateHologram"],
				anchorY = 0.75f
			};
			symbolDirty = true;
			AddToContainer(sLeaser, rCam, rCam.ReturnFContainer("Foreground"));
		}

		public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
		{
			float f = Mathf.Lerp(lastFade, fade, timeStacker);
			for (int i = 0; i < 2; i++)
			{
				sLeaser.sprites[i].x = Mathf.Lerp(lastPos.x, pos.x, timeStacker) - camPos.x;
				sLeaser.sprites[i].y = Mathf.Lerp(lastPos.y, pos.y, timeStacker) - camPos.y;
				sLeaser.sprites[i].isVisible = f > 0f;
				sLeaser.sprites[i].color = Color.Lerp(lastColor, color, timeStacker);
			}
			if (symbolDirty)
			{
				sLeaser.sprites[1].element = passageSprite;
				symbolDirty = false;
			}
			sLeaser.sprites[0].scale = Mathf.Lerp(20f, 30f, f) / 16f;
			sLeaser.sprites[0].alpha = f * Mathf.Lerp(0.55f, 0.6f, UnityEngine.Random.value) * 0.6f;
			sLeaser.sprites[1].alpha = f * 0.9f;
			base.DrawSprites(sLeaser, rCam, timeStacker, camPos);
		}

		public override void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
		{
			referenceGlyph.ApplyPalette(sLeaser, rCam, palette);
		}
	}
}
