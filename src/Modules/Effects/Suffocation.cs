using System.Collections.Generic;
using System.Runtime.CompilerServices;
using EffExt;

namespace RegionKit.Modules.Effects
{
	// By @.k0r1 / Korii1 and Alduris
	internal static class SuffocationBuilder
    {
        internal static void __RegisterBuilder()
        {
            try
            {
                new EffectDefinitionBuilder("Suffocation")
                    .SetCategory("RegionKit")
                    .Register();
            }
            catch (Exception ex)
            {
                LogWarning(string.Format("Error on eff examples init {0}", ex));
            }
        }
    }

    public static class SuffocationHooks
    {
		private static readonly ConditionalWeakTable<Creature, float[]> BreathData = new();
		private static readonly ConditionalWeakTable<AbstractCreature, StrongBox<bool>> ImmuneData = new();

		internal static void Apply()
		{
			On.AbstractCreature.ctor += AbstractCreature_ctor;
			On.AbstractCreature.setCustomFlags += AbstractCreature_setCustomFlags;
			On.Player.LungUpdate += Player_Update;
			On.AirBreatherCreature.Update += AirBreatherCreature_Update;
			On.BubbleGrass.Update += BubbleGrass_Update;
		}

		internal static void Undo()
		{
			On.AbstractCreature.ctor -= AbstractCreature_ctor;
			On.AbstractCreature.setCustomFlags -= AbstractCreature_setCustomFlags;
			On.Player.LungUpdate -= Player_Update;
			On.AirBreatherCreature.Update -= AirBreatherCreature_Update;
			On.BubbleGrass.Update -= BubbleGrass_Update;
		}

		private static void AbstractCreature_ctor(On.AbstractCreature.orig_ctor orig, AbstractCreature self, World world, CreatureTemplate creatureTemplate, Creature realizedCreature, WorldCoordinate pos, EntityID ID)
		{
			orig(self, world, creatureTemplate, realizedCreature, pos, ID);
			ImmuneData.Add(self, new(false));
		}

		private static void AbstractCreature_setCustomFlags(On.AbstractCreature.orig_setCustomFlags orig, AbstractCreature self)
		{
			ImmuneData.Remove(self);
			bool found = false;
			if (self.spawnData is not null && self.spawnData.StartsWith("{"))
			{
				var list = self.spawnData[1..^1].Split(',', '|');
				foreach (var item in list)
				{
					if (item.Trim().Equals("SuffocationImmune", StringComparison.InvariantCultureIgnoreCase))
					{
						ImmuneData.Add(self, new(true));
						found = true;
						break;
					}
				}
			}
			if (!found)
			{
				ImmuneData.Add(self, new(false));
			}
			orig(self);
		}


		public static void Player_Update(On.Player.orig_LungUpdate orig, Player self)
        {
            orig(self);
			float multiplier;
			if (!self.dead && self.room != null && (multiplier = self.room.roomSettings.GetEffectAmount(_Enums.Suffocation)) > 0f)
			{
				if (self.submerged)
				{
					BreathData.Remove(self);
					return;
				}

				var data = BreathData.GetValue(self, (_) => [self.airInLungs, self.airInLungs]);
				data[1] = data[0];

				bool holdingBubbleGrass = self.grasps.Any(x => x?.grabbed is BubbleGrass bg && bg.oxygen > 0f);
				if (!holdingBubbleGrass)
				{
					if (data[0] < self.slugcatStats.drownThreshold)
					{
						self.lungsExhausted = true;
					}
					float baseValue = 1f / (40f * (self.lungsExhausted ? 4.5f : 9f) * ((self.airInLungs < self.slugcatStats.drownThreshold) ? 1.5f : 1f) * (self.room.game.setupValues.lungs / 100f));
					self.airInLungs = Mathf.Max(0f, data[1] - baseValue * multiplier * self.slugcatStats.lungsFac);
					if (self.airInLungs == 0f) self.Die();
				}
				else
				{
					self.airInLungs = 1f;
				}

				data[0] = self.airInLungs;
			}
			else
			{
				BreathData.Remove(self);
			}
        }

		private static void AirBreatherCreature_Update(On.AirBreatherCreature.orig_Update orig, AirBreatherCreature self, bool eu)
		{
			orig(self, eu);
			float multiplier;
			if (!self.dead && self.room != null && (multiplier = self.room.roomSettings.GetEffectAmount(_Enums.Suffocation)) > 0f && (!ImmuneData.TryGetValue(self.abstractCreature, out StrongBox<bool> immune) || !immune.Value))
			{
				if (self.Submersion == 1f || self.firstChunk.sandSubmersion >= 0.9f)
				{
					BreathData.Remove(self);
					return;
				}

				var data = BreathData.GetValue(self, (_) => [self.lungs, self.lungs]);
				data[1] = data[0];

				self.lungs = Mathf.Max(-1f, data[1] - 1f / self.Template.lungCapacity);

				if (self.lungs < 0.3f)
				{
					if (UnityEngine.Random.value < 0.025f)
					{
						self.LoseAllGrasps();
					}
					if (self.lungs <= 0f && UnityEngine.Random.value < 0.1f)
					{
						self.Stun(UnityEngine.Random.Range(0, 18));
					}
					if (self.lungs < -0.5f && UnityEngine.Random.value < 1f / Custom.LerpMap(self.lungs, -0.5f, -1f, 90f, 30f))
					{
						self.Die();
					}
				}

				data[0] = self.lungs;
			}
			else
			{
				BreathData.Remove(self);
			}
		}

		private static void BubbleGrass_Update(On.BubbleGrass.orig_Update orig, BubbleGrass self, bool eu)
		{
			orig(self, eu);
			float multiplier;
			if (self.room != null && (multiplier = self.room.roomSettings.GetEffectAmount(_Enums.Suffocation)) > 0f)
			{
				if (self.firstChunk.submersion <= 0.9f && self.grabbedBy.Count > 0 && self.grabbedBy[0].grabber is Player grabPlayer)
				{
					self.AbstrBubbleGrass.oxygenLeft = Mathf.Max(0f, self.AbstrBubbleGrass.oxygenLeft - 0.0009090909f * multiplier);
				}
			}
		}
    }
}
