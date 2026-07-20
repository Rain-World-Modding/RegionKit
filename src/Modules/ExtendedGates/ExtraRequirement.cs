using HUD;

namespace RegionKit.Modules.ExtendedGates
{
	/// <summary>
	/// Defines an extra requirement to use for gates
	/// </summary>
	public abstract class ExtraRequirement
	{
		private const int AmountPerRow = 4;
		private const float HorizontalSpacing = 25f;
		private const float VerticalSpacing = 30f;

		public ExtraRequirement() { }

		/// <summary>
		/// The keyword to append after Left- or Right- in the extra requirement tag
		/// </summary>
		public abstract string BaseKeyword { get; }

		/// <summary>
		/// A check for whether or not the condition is fulfilled. Shows on the map and, by default, is checked at the gate.
		/// To define a separate condition to be checked at the gate itself, override <see cref="CompletedAtGate(RegionGate)"/>.
		/// </summary>
		/// <param name="saveState"></param>
		/// <returns></returns>
		public abstract bool Completed(SaveState saveState);

		/// <summary>
		/// The actual condition checked when determining whether or not the gate can be passed through.
		/// By default, it calls <see cref="Completed(SaveState)"/>, which is implementation-dependent.
		/// </summary>
		/// <param name="gate">The region gate</param>
		/// <returns>Whether or not the player(s) are able to pass</returns>
		public virtual bool CompletedAtGate(RegionGate gate)
		{
			return Completed(gate.room.game.GetStorySession.saveState);
		}

		/// <summary>
		/// Sprite element to show on the gate and map
		/// </summary>
		public abstract FAtlasElement SpriteElement { get; }

		/// <summary>
		/// Scale multiplier for sprite. Aim to be approximately 24 pixels tall or wide.
		/// </summary>
		/// <param name="fade">Fade value for lerping if desired. Only used by gates, 0f otherwise.</param>
		/// <returns>Scale multiplier for sprite</returns>
		public virtual float SpriteScale(float fade) => 1f;

		internal void SpawnGateSymbol(RegionGate gate, bool side, int index, int total)
		{
			GateKarmaGlyph parentGlyph = gate.karmaGlyphs[side ? 0 : 1];
			bool lastRow = index / AmountPerRow == total / AmountPerRow;
			int amountThisRow = lastRow ? total % AmountPerRow : AmountPerRow;
			float x = index % AmountPerRow - (amountThisRow - 1) / 2f;
			x *= HorizontalSpacing;
			float y = VerticalSpacing * (index / AmountPerRow) + 60f;
			gate.room.AddObject(CreateGateSprite(side, gate, parentGlyph, new Vector2(x, y)));

			if (index % AmountPerRow == 0)
			{
				float toMove = VerticalSpacing * 2f / 3f;
				parentGlyph.lastPos.y -= toMove;
				parentGlyph.pos.y -= toMove;
			}
		}

		/// <summary>
		/// Creates the cosmetic sprite used by the gate. Can be overridden if desired to use a subclass with custom logic.
		/// </summary>
		/// <param name="side">Which side of the gate the lock is on. True is left side.</param>
		/// <param name="gate">The region gate</param>
		/// <param name="referenceGlyph">The gate symbol this sprite will be above</param>
		/// <param name="offsetFromGlyph">The calculated offset relative to the gate symbol</param>
		/// <returns>The cosmetic sprite to add</returns>
		protected virtual GateExtraRequirementSprite CreateGateSprite(bool side, RegionGate gate, GateKarmaGlyph referenceGlyph, Vector2 offsetFromGlyph)
		{
			return new GateExtraRequirementSprite(side, gate, referenceGlyph, this, offsetFromGlyph);
		}

		internal void SpawnMapSymbol(Map.GateMarker gateMarker, int index, int total)
		{
			float radius = 40f;
			float x = Mathf.Sin(Mathf.PI * 2f * index / total) * radius;
			float y = Mathf.Cos(Mathf.PI * 2f * index / total) * radius;
			gateMarker.map.mapObjects.Add(CreateMapSprite(gateMarker, new Vector2(x, y)));
		}

		/// <summary>
		/// Creates the map element used by the gate marker on the map. Can be overridden if desired to use a subclass with custom logic.
		/// </summary>
		/// <param name="referenceMarker">The marker the sprite will be around</param>
		/// <param name="offsetFromMarker">The calculated offset relative to the gate symbol</param>
		/// <returns>The HUD element to add</returns>
		protected virtual MapExtraRequirementSprite CreateMapSprite(Map.GateMarker referenceMarker, Vector2 offsetFromMarker)
		{
			return new MapExtraRequirementSprite(referenceMarker, this, offsetFromMarker);
		}
	}
}
