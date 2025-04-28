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
                    .SetUADFactory((Room room, EffectExtraData data, bool firstTimeRealized) => new SuffocationUAD(room, data))
                    .SetCategory("RegionKit")
                    .Register();
            }
            catch (Exception ex)
            {
                LogWarning(string.Format("Error on eff examples init {0}", ex));
            }
        }
    }

    internal class SuffocationUAD : UpdatableAndDeletable
    {
		private readonly ConditionalWeakTable<Creature, float[]> BreathData = new();
        public EffectExtraData EffectData { get; }
        public SuffocationUAD(Room room, EffectExtraData effectData)
        {
			this.room = room;
            EffectData = effectData;
			if (effectData.Amount == 0f)
			{
				Destroy();
			}
        }

        public override void Update(bool eu)
        {
			if (slatedForDeletetion) return;
			float multiplier = EffectData.Amount;
			foreach (List<PhysicalObject>? list in room.physicalObjects)
			{
				foreach (PhysicalObject obj in list)
				{
					if (obj is BubbleGrass bubbleGrass)
					{
						if (bubbleGrass.firstChunk.submersion <= 0.9f && bubbleGrass.grabbedBy.Count > 0 && bubbleGrass.grabbedBy[0].grabber is Player grabPlayer)
						{
							bubbleGrass.AbstrBubbleGrass.oxygenLeft = Mathf.Max(0f, bubbleGrass.AbstrBubbleGrass.oxygenLeft - 0.0009090909f * multiplier);
						}
					}
				}
			}
        }

		public void LungOverride(Player player)
		{
			float multiplier = EffectData.Amount;
			if (slatedForDeletetion || multiplier == 0f || player.room != room || player.submerged) return;

			var data = BreathData.GetValue(player, (_) => [player.airInLungs, player.airInLungs]);
			data[1] = data[0];

			bool holdingBubbleGrass = player.grasps.Any(x => x?.grabbed is BubbleGrass);
			if (!holdingBubbleGrass)
			{
				if (data[0] < player.slugcatStats.drownThreshold)
				{
					player.lungsExhausted = true;
				}
				float baseValue = 1f / (40f * (player.lungsExhausted ? 4.5f : 9f) * ((player.airInLungs < player.slugcatStats.drownThreshold) ? 1.5f : 1f) * (room.game.setupValues.lungs / 100f));
				player.airInLungs = Mathf.Max(0f, data[0] - baseValue * multiplier * player.slugcatStats.lungsFac);
				if (player.airInLungs == 0f) player.Die();
			}
			else
			{
				player.airInLungs = 1f;
			}

			data[0] = player.airInLungs;
		}
    }

    public static class SuffocationHooks
    {
        static void Update(On.Player.orig_LungUpdate orig, Player self)
        {
            orig(self);
			if (self.room != null && self.room.updateList.FirstOrDefault(x => x is SuffocationUAD) is SuffocationUAD suffocation)
			{
				suffocation.LungOverride(self);
			}
        }

		internal static void Apply()
		{
			On.Player.LungUpdate += Update;
		}

		internal static void Undo()
		{
			On.Player.LungUpdate -= Update;
		}
    }
}
