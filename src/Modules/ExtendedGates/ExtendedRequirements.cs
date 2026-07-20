using MoreSlugcats;

namespace RegionKit.Modules.ExtendedGates
{
	public static class ExtendedRequirements
	{
		public class CommsMark : ExtraRequirement
		{
			public override string BaseKeyword => "TheMark";

			public override FAtlasElement SpriteElement => Futile.atlasManager.GetElementWithName("smallKarmaComsmark");
			public override float SpriteScale(float fade) => 0.5f;

			public override bool Completed(SaveState saveState) => saveState.deathPersistentSaveData.theMark;
		}

		public class Glow : ExtraRequirement
		{
			public override string BaseKeyword => "TheGlow";

			public override FAtlasElement SpriteElement => Futile.atlasManager.GetElementWithName("smallKarmaGlow");
			public override float SpriteScale(float fade) => 0.5f;

			public override bool Completed(SaveState saveState) => saveState.theGlow;
		}

		public class KarmaReinforced : ExtraRequirement
		{
			public override string BaseKeyword => "Reinforced";

			public override FAtlasElement SpriteElement => Futile.atlasManager.GetElementWithName("FlowerMarker");

			public override bool Completed(SaveState saveState) => saveState.deathPersistentSaveData.reinforcedKarma;
			public override bool CompletedAtGate(RegionGate gate) => gate.room.game.Players[0].realizedCreature is Player p && p.KarmaIsReinforced;
		}


		public class Passage : ExtraRequirement
		{
			public WinState.EndgameID passage;
			private readonly FAtlasElement passageSprite;

			public Passage(WinState.EndgameID passage) : base()
			{
				this.passage = passage;

				if (Futile.atlasManager.TryGetElementWithName(passage.value + "B", out FAtlasElement? el) && el != null)
				{
					passageSprite = el;
				}
				else
				{
					LogWarning("Could not find passage sprite for " + passage.value + "!");
					passageSprite = Futile.atlasManager.GetElementWithName("Sandbox_QuestionMark");
				}
			}

			public override FAtlasElement SpriteElement => passageSprite;

			public override string BaseKeyword => $"Passage-{passage.value}";

			public override bool Completed(SaveState saveState)
			{
				WinState winState = saveState.deathPersistentSaveData.winState;
				WinState.EndgameTracker? tracker = winState.GetTracker(passage, false);
				return tracker != null && !tracker.GoalFullfilled;
			}
		}

		internal sealed class DelegateDriven : ExtraRequirement
		{
			private readonly string keyword;
			private readonly string spriteName;
			private readonly float spriteScale;

			private readonly Func<SaveState, bool>? saveStateCond;
			private readonly Func<RegionGate, bool>? regionGateCond;

			public DelegateDriven(string keyword, string spriteName, float spriteScale, Func<SaveState, bool>? saveStateCond, Func<RegionGate, bool>? regionGateCond)
			{
				this.keyword = keyword;
				this.spriteName = spriteName;
				this.spriteScale = spriteScale;
				this.saveStateCond = saveStateCond;
				this.regionGateCond = regionGateCond;
			}

			public override string BaseKeyword => keyword;

			public override FAtlasElement SpriteElement => Futile.atlasManager.GetElementWithName(spriteName);
			public override float SpriteScale(float fade) => spriteScale;

			public override bool Completed(SaveState saveState)
			{
				return saveStateCond?.Invoke(saveState) ?? true;
			}

			public override bool CompletedAtGate(RegionGate gate)
			{
				return regionGateCond?.Invoke(gate) ?? base.CompletedAtGate(gate);
			}
		}
	}
}
