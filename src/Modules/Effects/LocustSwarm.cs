using System.Reflection;
using EffExt;
using Mono.Cecil.Cil;
using MonoMod.Cil;

namespace RegionKit.Modules.Effects
{
	internal static class LocustSwarmBuilder
	{
		internal static void __RegisterBuilder()
		{
			try
			{
				new EffectDefinitionBuilder("LocustSwarm")
					.AddIntField("delay", 0, 300, 0, "Swarm Delay")
					.SetUADFactory((Room room, EffectExtraData data, bool firstTimeRealized) => new LocustSwarmUAD(data))
					.SetCategory("RegionKit")
					.Register();
				LocustSwarm.Setup();
			}
			catch (Exception e)
			{
				LogWarning($"Error registering locust swarm threat: {e.Message}");
			}
		}

		internal class LocustSwarmUAD(EffectExtraData effectData) : UpdatableAndDeletable
		{
			public EffectExtraData EffectData { get; } = effectData;
			public int Delay = effectData.GetInt("delay");
			public int StartTime = -1;
			public float Rampup = 0.001f;
		}
	}

	public class LocustSwarm
	{

		static void Update(On.Player.orig_Update orig, Player self, bool firstUpdate)
		{
			orig(self, firstUpdate);
			if (self.room == null) return;
			LocustSwarmBuilder.LocustSwarmUAD? locustUAD = self.room.updateList.OfType<LocustSwarmBuilder.LocustSwarmUAD>().FirstOrDefault();
			RoomSettings.RoomEffect? locustEffect = self.room.roomSettings.GetEffect(RoomSettings.RoomEffect.Type.LocustSwarmConfig);
			if (locustUAD == null || locustEffect == null) return;
			if (self.room.world?.rainCycle.RainApproaching >= 0.5f)
			{
				locustUAD.StartTime = -1;
				locustUAD.Rampup = 0.001f;
				return;
			}
			if (locustUAD.StartTime < 0)
				locustUAD.StartTime = self.room.game.timeInRegionThisCycle;
			if ((self.room.game.timeInRegionThisCycle - locustUAD.StartTime) / 40 <= 0) return;

            if (self.room.game.timeInRegionThisCycle % 40 == 0)
            {
                if (locustEffect.amount < 1)
                    locustEffect.amount = Math.Min(locustEffect.amount+0.01f, 1f);
                if (locustEffect.extraAmounts[1] < 1)
                    locustEffect.extraAmounts[1] = Math.Min(locustEffect.extraAmounts[1]+0.0101f, 1f);
                else
                    locustEffect.extraAmounts[0] = Math.Min(locustEffect.extraAmounts[0]+0.01f, 1f);
            }
		}

		static float Hook_ExposedToSky(On.LocustSystem.orig_ExposedToSky orig, LocustSystem self, Creature feature)
		{
			if (self.room == null || self.room.world?.rainCycle.RainApproaching >= 0.5f) return orig(self, feature);
			LocustSwarmBuilder.LocustSwarmUAD? locustUAD = self.room.updateList.OfType<LocustSwarmBuilder.LocustSwarmUAD>().FirstOrDefault();
			RoomSettings.RoomEffect? locustEffect = self.room.roomSettings.GetEffect(RoomSettings.RoomEffect.Type.LocustSwarmConfig);
			if (locustUAD == null || locustEffect == null || locustEffect.amount < 0.75f) return orig(self, feature);
			return 1f;
		}
        
        static void Hook_SwarmUpdate(On.LocustSystem.Swarm.orig_Update orig, LocustSystem.Swarm self)
        {
            orig(self);
            LocustSwarmBuilder.LocustSwarmUAD? locustUAD = self.owner.room.updateList.OfType<LocustSwarmBuilder.LocustSwarmUAD>().FirstOrDefault();
            RoomSettings.RoomEffect? locustEffect = self.owner.room.roomSettings.GetEffect(RoomSettings.RoomEffect.Type.LocustSwarmConfig);
            if (locustUAD == null || locustEffect == null || locustEffect.amount < 0.85f) return;
            self.maxLocusts = 150;
            
        }
	
		internal static void Setup()
        {
			On.LocustSystem.ExposedToSky += Hook_ExposedToSky;
            On.LocustSystem.Swarm.Update += Hook_SwarmUpdate;
			On.Player.Update += Update;
        }
	}
}
