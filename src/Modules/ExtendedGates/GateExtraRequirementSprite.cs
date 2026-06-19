using MoreSlugcats;

namespace RegionKit.Modules.ExtendedGates
{
	public class GateExtraRequirementSprite : CosmeticSprite
	{
		public bool side;
		public RegionGate gate;
		public GateKarmaGlyph referenceGlyph;
		public ExtraRequirement requirement;
		protected Vector2 offsetFromGlyph;

		protected virtual FAtlasElement GetSprite() => requirement.SpriteElement;

		protected float flicker => referenceGlyph.flicker;
		protected float fade;
		protected float lastFade;
		protected float goalFade
		{
			get { return redSine > 0f ? 1f : referenceGlyph.goalFade; }
			set => referenceGlyph.goalFade = value;
		}
		protected virtual Color myDefaultColor => referenceGlyph.myDefaultColor;
		protected float sinAdder;
		public bool symbolDirty = true;
		public Color color;
		public Color lastColor;
		protected float extraRedSine = 0f;
		protected float redSine
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

		protected virtual bool FlashRed => !referenceGlyph.PlayNoEnergyAnimation && gate.letThroughDir == side && gate.PlayersInZone() > 0 && !requirement.CompletedAtGate(gate);

		public GateExtraRequirementSprite(bool side, RegionGate gate, GateKarmaGlyph referenceGlyph, ExtraRequirement requirement, Vector2 offsetFromGlyph)
		{
			this.side = side;
			this.gate = gate;
			this.referenceGlyph = referenceGlyph;
			this.requirement = requirement;
			room = gate.room;
			this.offsetFromGlyph = offsetFromGlyph;
			pos = referenceGlyph.pos + offsetFromGlyph;
			lastPos = pos;
			color = GetToColor;
			lastColor = color;
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

			if (extraRedSine > 0f)
			{
				float twoPi = 2 * Mathf.PI;
				float wrappedPhase = extraRedSine / 25f % twoPi;
				if (wrappedPhase < 0) wrappedPhase += twoPi;
				extraRedSine = wrappedPhase * 25f;
				extraRedSine -= 3f;
				if (extraRedSine < 0f) extraRedSine = 0f;
			}
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
				sLeaser.sprites[1].element = GetSprite();
				symbolDirty = false;
			}
			sLeaser.sprites[0].scale = Mathf.Lerp(20f, 30f, f) / 16f;
			sLeaser.sprites[0].alpha = f * Mathf.Lerp(0.55f, 0.6f, UnityEngine.Random.value) * 0.6f;
			sLeaser.sprites[1].scale = requirement.SpriteScale(f);
			sLeaser.sprites[1].alpha = f * 0.9f;
			base.DrawSprites(sLeaser, rCam, timeStacker, camPos);
		}

		public override void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
		{
			referenceGlyph.ApplyPalette(sLeaser, rCam, palette);
		}
	}
}
