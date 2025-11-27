using System.Reflection;
using EffExt;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;

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
                    .AddFloatField("speed", 0f, 5f, 0.01f, 1f, "Speed")
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
            public readonly float Amount = effectData.Amount < 0.8f ? effectData.Amount : 1f;
			public readonly int Delay = effectData.GetInt("delay");
            public readonly float Speed = effectData.GetFloat("speed");
			public int StartTime = -1;
		}
	}

	public static class LocustSwarm
	{

		static void Update(On.Player.orig_Update orig, Player self, bool firstUpdate)
		{
			orig(self, firstUpdate);
			if (self.room == null) return;
			LocustSwarmBuilder.LocustSwarmUAD? locustUAD = self.room.updateList.OfType<LocustSwarmBuilder.LocustSwarmUAD>().FirstOrDefault();
			RoomSettings.RoomEffect? locustEffect = self.room.roomSettings.GetEffect(RoomSettings.RoomEffect.Type.LocustSwarmConfig);
			if (locustUAD == null || locustEffect == null || locustUAD.Amount < locustEffect.amount) return;
			if (self.room.world?.rainCycle.RainApproaching >= 0.5f)
			{
				locustUAD.StartTime = -1;
				return;
			}
			if (locustUAD.StartTime < 0)
				locustUAD.StartTime = self.room.game.timeInRegionThisCycle;
			if (self.room.game.timeInRegionThisCycle - locustUAD.Delay * 40 < locustUAD.StartTime) return;

            if (self.room.game.timeInRegionThisCycle % 40 != 0)
                return;
            // Gradually increase the density and volume
            // amount: Density
            // 0: Volume, 1: Sound Distance, 2: Shadows, 3: Grounded
            if (locustEffect.amount < 1)
                locustEffect.amount = Math.Min(locustEffect.amount + 0.01f * locustUAD.Speed, locustUAD.Amount);
            if (locustEffect.amount > 0.4f)
                locustEffect.extraAmounts[2] = Math.Min(locustEffect.extraAmounts[2] + 0.015f * locustUAD.Speed, 1f);
            if (locustEffect.extraAmounts[1] < 1)
                locustEffect.extraAmounts[1] = Math.Min(locustEffect.extraAmounts[1] + 0.0101f * locustUAD.Speed, locustUAD.Amount);
            else
                locustEffect.extraAmounts[0] = Math.Min(Math.Min(locustEffect.extraAmounts[0] + 0.01f * locustUAD.Speed, locustUAD.Amount), 0.7f);
        }

        // Attack even if under a structure at above 70% density
		static float Hook_ExposedToSky(On.LocustSystem.orig_ExposedToSky orig, LocustSystem self, Creature feature)
		{
			if (self.room == null || self.room.world?.rainCycle.RainApproaching >= 0.5f) return orig(self, feature);
			LocustSwarmBuilder.LocustSwarmUAD? locustUAD = self.room.updateList.OfType<LocustSwarmBuilder.LocustSwarmUAD>().FirstOrDefault();
			RoomSettings.RoomEffect? locustEffect = self.room.roomSettings.GetEffect(RoomSettings.RoomEffect.Type.LocustSwarmConfig);
			if (locustUAD == null || locustEffect == null || locustEffect.amount < 0.7f) return orig(self, feature);
			return 1f;
		}
        
        // Increase the swarm size of attacking locusts above 85% density
        static void Hook_SwarmUpdate(On.LocustSystem.Swarm.orig_Update orig, LocustSystem.Swarm self)
        {
            orig(self);
            LocustSwarmBuilder.LocustSwarmUAD? locustUAD = self.owner.room.updateList.OfType<LocustSwarmBuilder.LocustSwarmUAD>().FirstOrDefault();
            RoomSettings.RoomEffect? locustEffect = self.owner.room.roomSettings.GetEffect(RoomSettings.RoomEffect.Type.LocustSwarmConfig);
            if (locustUAD == null || locustEffect == null || locustEffect.amount < 0.85f) return;
            self.maxLocusts = 150;
        }

        static float Hook_GetAvoidanceRadius(On.LocustSystem.orig_GetAvoidanceRadius orig, LocustSystem self, AbstractPhysicalObject obj)
        {
            return orig(self, obj); //TODO: Make Locusts ignore mud/paint/spores at 100% density, maybe have a follow radius that shrinks until attacking
        }

        // Ignore creature mass above 70% density, kill everyone
        static bool Hook_CanKill(On.LocustSystem.orig_CanKill orig, LocustSystem self, Creature creature)
        {
            if (self.room == null || self.room.world?.rainCycle.RainApproaching >= 0.5f) return orig(self, creature);
            LocustSwarmBuilder.LocustSwarmUAD? locustUAD = self.room.updateList.OfType<LocustSwarmBuilder.LocustSwarmUAD>().FirstOrDefault();
            RoomSettings.RoomEffect? locustEffect = self.room.roomSettings.GetEffect(RoomSettings.RoomEffect.Type.LocustSwarmConfig);
            if (locustUAD == null || locustEffect == null || locustEffect.amount < 0.7f) return orig(self, creature);
            return true;
        }

        // Allow pathfinding around obstacles above 70% density. Various methods and fields here are referenced via IL code
        /*public static Dictionary<LocustSystem.Swarm, Vector2[]> SwarmPaths = new Dictionary<LocustSystem.Swarm, Vector2[]>();
        public static bool CanPathfinding(LocustSystem.Swarm self)
        {
            LogInfo("LocustSwarm.cs: AllowPathfinding called");
            LocustSwarmBuilder.LocustSwarmUAD? locustUAD = self.owner.room.updateList.OfType<LocustSwarmBuilder.LocustSwarmUAD>().FirstOrDefault();
            RoomSettings.RoomEffect? locustEffect = self.owner.room.roomSettings.GetEffect(RoomSettings.RoomEffect.Type.LocustSwarmConfig);
            return locustUAD != null
                   && locustEffect != null
                   && locustEffect.amount >= 0.7f
                   && self.owner.room.world?.rainCycle.RainApproaching < 0.5f;
        }

        public static void DoPathfind(LocustSystem.Swarm self)
        {
            LogInfo("LocustSwarm.cs: DoPathfind called");
        }*/

        internal static void Setup()
        {
			On.LocustSystem.ExposedToSky += Hook_ExposedToSky;
            On.LocustSystem.Swarm.Update += Hook_SwarmUpdate;
            On.LocustSystem.GetAvoidanceRadius += Hook_GetAvoidanceRadius;
            On.LocustSystem.CanKill += Hook_CanKill;
			On.Player.Update += Update;
            
            //LocustSwarmILHooks.Setup();
        }
	}

    /*internal static class LocustSwarmILHooks  // TODO: Implement pathfinding via IL hooks :(
    {                                           // I hate IL hooks I hate IL hooks I hate IL hooks
        internal static void Setup()            // I tried everything this just seems to do nothing no matter what I try
        {                                       // -Blake
            IL.LocustSystem.Swarm.Update += ILSwarmUpdate;
        }

        static void ILSwarmUpdate(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            try
            {
                // Implement custom logic if pathfinding is allowed and skip the code, or run the original code
                if (c.TryGotoNext(MoveType.After,
                        x => x.MatchLdloc(out _),
                        x => x.MatchAnd(),
                        x => x.MatchBrfalse(out _)))
                {
                    LogDebug("LocustSwarm.cs: BEGIN STACK");

                    //ILLabel skip = il.DefineLabel();
                    //c.Prev.OpCode = OpCodes.Brtrue;
                    //c.Emit(OpCodes.Ldarg_0);
                    //c.Emit(OpCodes.Callvirt, typeof(LocustSwarm).GetMethod("CanPathfind"));
                    //c.Emit(OpCodes.Brfalse, skip);
                    //c.MarkLabel(skip);
                    
                    LogDebug("LocustSwarm.cs: END STACK");
                }
                else
                    LogWarning("LocustSwarm.cs: Failed to inject IL hook in Swarm.Update() - IL instruction not found");
            }
            catch (Exception e)
            {
                LogWarning($"LocustSwarm.cs: Failed to inject IL hook in Swarm.Update() - {e}");
            }
        }
    }*/
}
