using EffExt;

namespace RegionKit.Modules.Effects
{
	// By @.k0r1 / Korii1
	internal static class SuffocationBuilder
    {
        internal static void __RegisterBuilder()
        {
            try
            {
                new EffectDefinitionBuilder("Suffocation")
                    .SetUADFactory((Room room, EffectExtraData data, bool firstTimeRealized) => new SuffocationUAD(data))
                    .SetCategory("RegionKit")
                    .Register();
                Suffocation.Setup();
            }
            catch (Exception ex)
            {
                LogWarning(string.Format("Error on eff examples init {0}", ex));
            }
        }
    }
    internal class SuffocationUAD : UpdatableAndDeletable
    {
        public EffectExtraData EffectData { get; }
        public SuffocationUAD(EffectExtraData effectData)
        {
            this.EffectData = effectData;
        }
        //public override void Update(bool eu)
        //{
        //}
    }
    public class Suffocation
    {
        static Dictionary<Player, float[]> PlayerData = new Dictionary<Player, float[]>();
        public static readonly RoomSettings.RoomEffect.Type RoomEffect = new RoomSettings.RoomEffect.Type("Suffocation", true);
        static int? GetBubbleGrassIndex(Player self)
        {
            for (int i = 0; i < self.grasps.Length; i++)
            {
                if (self.grasps[i]?.grabbed.abstractPhysicalObject is BubbleGrass.AbstractBubbleGrass bubbleGrass && bubbleGrass.oxygenLeft > 0f)
                    return i;
            }
            return null;
        }
        static void Update(On.Player.orig_LungUpdate orig, Player self)
        {
			// INIT
            orig(self);
            if (!PlayerData.ContainsKey(self))
            {
                float[] PlayerData = new float[3];
                PlayerData[0] = self.airInLungs;
                PlayerData[1] = self.airInLungs;
                PlayerData[2] = self.airInLungs * 0.1f;

                Suffocation.PlayerData[self] = PlayerData;
            }
            if (self.room == null) { return; }
            if (self.submerged) { return; }

            // CHECKS
            float[] CurrentPlayerData = PlayerData[self];
            Creature.Grasp[] grasps = self.grasps;

            if ((CurrentPlayerData[0] < .89 && self.airInLungs == CurrentPlayerData[1] && CurrentPlayerData[0] < CurrentPlayerData[1]) || self.airInLungs < CurrentPlayerData[0])
            {
                CurrentPlayerData.SetValue(self.airInLungs, 0);
            }
            if (CurrentPlayerData[0] <= CurrentPlayerData[2])
            {
                self.lungsExhausted = true;
            }

            int? bubbleGrassIndex = GetBubbleGrassIndex(self);
            float roomMultiplier = self.room.roomSettings.GetEffectAmount(RoomEffect);
            float multiplier = ((roomMultiplier == 0f) ? 0.001f : -0.01f * roomMultiplier);

            // RUN
            if (bubbleGrassIndex != null)
            {
                BubbleGrass.AbstractBubbleGrass abstractBubbleGrass = (BubbleGrass.AbstractBubbleGrass)self.grasps[(int)bubbleGrassIndex].grabbed.abstractPhysicalObject;
                float newValue = Mathf.Clamp(abstractBubbleGrass.oxygenLeft + multiplier * 5, 0f, 1f);
                if (UnityEngine.Random.value < .2 && newValue < abstractBubbleGrass.oxygenLeft)
                {
                    Bubble bubble = new Bubble(self.grasps[(int)bubbleGrassIndex].grabbed.firstChunk.pos + Custom.RNV() * UnityEngine.Random.value * 4f,
                        Custom.RNV() * Mathf.Lerp(6f, 16f, UnityEngine.Random.value) * Mathf.InverseLerp(0f, 0.45f, abstractBubbleGrass.oxygenLeft), true, true);
                    self.room.AddObject(bubble);
                    bubble.age = 600 - UnityEngine.Random.Range(20, UnityEngine.Random.Range(30, 80));
                }
                abstractBubbleGrass.oxygenLeft = newValue;
            }
            else
            {
                float newValue = Mathf.Clamp(CurrentPlayerData[0] + multiplier, 0f, CurrentPlayerData[1]);
                CurrentPlayerData.SetValue(newValue, 0);
                self.airInLungs = newValue;
                if (newValue == 0f)
                {
                    self.Die();
                }
            }
        }
		internal static void Setup()
		{
			On.Player.LungUpdate += Update;
		}
    }
}
