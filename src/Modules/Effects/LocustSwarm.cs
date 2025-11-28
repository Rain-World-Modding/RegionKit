using EffExt;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MoreSlugcats;
using Watcher;

// NOTE: Pathfinding has been disabled until I can convert Vector2 coordinates into tile coordinates
namespace RegionKit.Modules.Effects
{
	internal static class LocustSwarmBuilder
	{
		internal static void __RegisterBuilder()
		{
			try
			{
				new EffectDefinitionBuilder("LocustSwarm")
					.SetUADFactory((Room room, EffectExtraData data, bool firstTimeRealized) => new LocustSwarm.UAD(data))
					.SetCategory("RegionKit")
					.Register();
				LocustSwarm.Setup();
			}
			catch (Exception e)
			{
				LogWarning($"Error registering locust swarm threat: {e.Message}");
			}
		}
	}

	public static class LocustSwarm
	{
        public class UAD(EffectExtraData effectData) : UpdatableAndDeletable
        {
            public readonly float Amount = effectData.Amount < 0.8f ? effectData.Amount : 1f;
            public float StartDensity = -1;
            public float StartVolume = -1;
            public float StartDistance = -1;
            public float StartShadows = -1;
            public float PrecycleIntensity = -1;
            //public Dictionary<LocustSystem.Swarm, LocustSwarm.PathingInformation> SwarmPaths = new Dictionary<LocustSystem.Swarm, LocustSwarm.PathingInformation>();
        }
        
        /*public class PathingInformation(LocustSystem.Swarm swarm)
        {
            public List<WorldCoordinate> Nodes = new List<WorldCoordinate>();
            public QuickPathFinder QPF = new(swarm.owner.room.GetTilePosition(swarm.center),
                swarm.owner.room.GetTilePosition(swarm.target.mainBodyChunk.pos),
                swarm.owner.room.aimap,
                GetCreatureTemplate(CreatureTemplate.Type.Fly));
        }*/

        private static List<Room> SwarmRooms = new List<Room>();
        private static int StartTime = -1;
		static void Update(On.Player.orig_Update orig, Player self, bool firstUpdate)
        {
			orig(self, firstUpdate);
            foreach (Room room in self.room.world.activeRooms)
                RoomUpdate(room);

            return;
            void RoomUpdate(Room room)
            {
                if (!HasLocustEffect(room, out UAD? uad, out RoomSettings.RoomEffect? effect) ||
                    uad!.Amount < effect!.amount) return;
                if (uad.StartDensity < 0)
                {
                    uad.StartDensity = effect.amount;
                    uad.StartVolume = effect.extraAmounts[0];
                    uad.StartDistance = effect.extraAmounts[1];
                    uad.StartShadows = effect.extraAmounts[2];
                }

                float progress;
                if (room.world.rainCycle.RainApproaching >= 0.5f || room.world.rainCycle.preTimer > 0)
                {
                    StartTime = -1;
                    if (room.world.rainCycle.preTimer <= 0)
                    {
                        progress = (float)room.world.rainCycle.timer / room.world.rainCycle.cycleLength;
                        if (effect.amount > 0)
                            effect.amount = Mathf.Lerp(uad.StartDensity,
                                Math.Max(uad.StartDensity, 0.7f * uad.Amount), progress);
                        effect.extraAmounts[0] = uad.StartVolume;
                        effect.extraAmounts[1] = Mathf.Lerp(uad.StartDistance,
                            Math.Max(uad.StartDistance, (effect.amount > 0 ? 0.75f : 0.45f) * uad.Amount), progress);
                        effect.extraAmounts[2] = Mathf.Lerp(uad.StartShadows,
                            Math.Max(uad.StartShadows, 0.45f * uad.Amount), progress);
                    }
                    else
                    {
                        if (uad.PrecycleIntensity < 0)
                            uad.PrecycleIntensity = room.world.rainCycle.preCycleRain_Intensity;
                        progress = Mathf.Clamp(room.world.rainCycle.preCycleRain_Intensity,
                            uad.PrecycleIntensity-0.005f, // Round off the effects slightly. It doesn't do much in long rooms, but it helps.
                            uad.PrecycleIntensity+0.005f);
                        uad.PrecycleIntensity = progress;
                        if (effect.amount > 0)
                            effect.amount = Mathf.Lerp(uad.StartDensity,
                                Math.Max(uad.StartDensity, uad.Amount), progress);
                        effect.extraAmounts[0] = Mathf.Lerp(uad.StartVolume,
                            Math.Max(uad.StartVolume, (effect.amount > 0 ? 0.5f : 0.25f) * uad.Amount), progress);
                        effect.extraAmounts[1] = Mathf.Lerp(uad.StartDistance,
                            Math.Max(uad.StartDistance, (effect.amount > 0 ? uad.Amount : 0.7f)), progress);
                        effect.extraAmounts[2] = Mathf.Lerp(uad.StartShadows,
                            Math.Max(uad.StartShadows, uad.Amount), progress);
                    }
                    return;
                }
                if (StartTime < 0)
                    StartTime = room.game.timeInRegionThisCycle;
                progress = Mathf.Clamp((room.game.timeInRegionThisCycle - StartTime) / 1400f, 0f, 1f); // 40fps * 35s = 1400
                if (effect.amount > 0)
                    effect.amount = Mathf.Lerp(Math.Max(uad.StartDensity, 0.7f * uad.Amount),
                        Math.Max(uad.StartDensity, uad.Amount), progress);
                effect.extraAmounts[0] = Mathf.Lerp(uad.StartVolume,
                    Math.Max(uad.StartVolume, (effect.amount > 0 ? 0.5f : 0.25f) * uad.Amount), progress);
                effect.extraAmounts[1] = Mathf.Lerp(Math.Max(uad.StartDistance, 0.75f * uad.Amount),
                    Math.Max(uad.StartDistance, (effect.amount > 0 ? uad.Amount : 0.7f)), progress);
                effect.extraAmounts[2] = Mathf.Lerp(Math.Max(uad.StartShadows, 0.45f * uad.Amount),
                    Math.Max(uad.StartShadows, uad.Amount), progress);
            }
        }

        static void Hook_LSUpdate(On.LocustSystem.orig_Update orig, LocustSystem self, bool eu)
        {
            orig(self, eu);
            if (self.room == null
                || self.room.game.timeInRegionThisCycle % 40 != 0
                || !HasLocustEffect(self.room, out UAD? uad, out RoomSettings.RoomEffect? effect)
                || effect!.amount < 0.95f)
                return;
            foreach (var crit in self.room.abstractRoom.creatures.Select(c => c.realizedCreature))
                crit.repelLocusts = 0;
            //foreach (LocustSystem.Swarm swarm in self.disbandingSwarms)
            //    uad!.SwarmPaths.Remove(swarm);
            //foreach (var swarm in self.swarms.Where(swarm => uad!.SwarmPaths.ContainsKey(swarm)))
            //    uad!.SwarmPaths[swarm].QPF.Update();
        }

        public static bool HasLocustEffect(Room room)
        {
            return room.updateList.OfType<UAD>().Any()
                && room.roomSettings.GetEffect(RoomSettings.RoomEffect.Type.LocustSwarmConfig) != null;
        }

        public static bool HasLocustEffect(Room room, out RoomSettings.RoomEffect? effect)
        {
            effect = room.roomSettings.GetEffect(RoomSettings.RoomEffect.Type.LocustSwarmConfig);
            return (room.updateList.OfType<UAD>().Any() && effect != null);
        }

        public static bool HasLocustEffect(Room room, out UAD? uad)
        {
            uad = room.updateList.OfType<UAD>().FirstOrDefault();
            return  (uad != null && room.roomSettings.GetEffect(RoomSettings.RoomEffect.Type.LocustSwarmConfig) != null);
        }
        public static bool HasLocustEffect(Room room, out UAD? uad, out RoomSettings.RoomEffect? effect)
        {
            uad = room.updateList.OfType<UAD>().FirstOrDefault();
            effect = room.roomSettings.GetEffect(RoomSettings.RoomEffect.Type.LocustSwarmConfig);
            return uad != null && effect != null;
        }

        // Attack even if under a structure at above 70% density
		static float Hook_ExposedToSky(On.LocustSystem.orig_ExposedToSky orig, LocustSystem self, Creature feature)
		{
			return self.room != null
                && HasLocustEffect(self.room, out RoomSettings.RoomEffect? effect)
                && effect!.amount > 0.7f ? 1f : orig(self, feature);
        }
        
        // Increase the swarm size of attacking locusts above 85% density
        static void Hook_SwarmUpdate(On.LocustSystem.Swarm.orig_Update orig, LocustSystem.Swarm self)
        {
            orig(self);
            if (!HasLocustEffect(self.owner.room, out RoomSettings.RoomEffect? effect) || effect!.amount < 0.85f)
                return;
            self.maxLocusts = 150;
        }

        // Ignore creature mass above 70% density, kill everyone
        internal static List<CreatureTemplate.Type> LocustImmune = [
            CreatureTemplate.Type.BrotherLongLegs,
            CreatureTemplate.Type.DaddyLongLegs,
            CreatureTemplate.Type.Deer,
            CreatureTemplate.Type.GarbageWorm,
            CreatureTemplate.Type.KingVulture,
            CreatureTemplate.Type.Leech,
            CreatureTemplate.Type.MirosBird,
            CreatureTemplate.Type.Overseer,
            CreatureTemplate.Type.PoleMimic,
            CreatureTemplate.Type.RedCentipede,
            CreatureTemplate.Type.SeaLeech,
            CreatureTemplate.Type.Vulture,
            DLCSharedEnums.CreatureTemplateType.AquaCenti,
            DLCSharedEnums.CreatureTemplateType.Inspector,
            DLCSharedEnums.CreatureTemplateType.JungleLeech,
            DLCSharedEnums.CreatureTemplateType.MirosVulture,
            DLCSharedEnums.CreatureTemplateType.TerrorLongLegs,
            MoreSlugcatsEnums.CreatureTemplateType.HunterDaddy,
            MoreSlugcatsEnums.CreatureTemplateType.TrainLizard,
            WatcherEnums.CreatureTemplateType.Angler,
            WatcherEnums.CreatureTemplateType.BasiliskLizard,
            WatcherEnums.CreatureTemplateType.BigMoth,
            WatcherEnums.CreatureTemplateType.BigSandGrub,
            WatcherEnums.CreatureTemplateType.BlizzardLizard,
            WatcherEnums.CreatureTemplateType.BoxWorm,
            WatcherEnums.CreatureTemplateType.DrillCrab,
            WatcherEnums.CreatureTemplateType.FireSprite,
            WatcherEnums.CreatureTemplateType.IndigoLizard,
            WatcherEnums.CreatureTemplateType.Loach,
            WatcherEnums.CreatureTemplateType.Rattler,
            WatcherEnums.CreatureTemplateType.RippleSpider,
            WatcherEnums.CreatureTemplateType.RotLoach,
            WatcherEnums.CreatureTemplateType.SandGrub,
            WatcherEnums.CreatureTemplateType.SkyWhale,
            WatcherEnums.CreatureTemplateType.Tardigrade,
            WatcherEnums.CreatureTemplateType.TowerCrab
        ];
        static bool Hook_CanKill(On.LocustSystem.orig_CanKill orig, LocustSystem self, Creature creature)
        {
            if (self.room == null
                || !HasLocustEffect(self.room, out RoomSettings.RoomEffect? effect)
                || effect!.amount < 0.7f) return orig(self, creature);
            return !LocustImmune.Contains(creature.Template.type);
        }

        static bool Hook_IsTargetValid(On.LocustSystem.Swarm.orig_IsTargetValid orig, LocustSystem.Swarm self)
        {
            if (self.owner.room == null
                || !HasLocustEffect(self.owner.room, out RoomSettings.RoomEffect? effect)
                || effect!.amount < 0.95f) return orig(self);
            return self.target != null
                   && self.target.room == self.owner.room
                   && self.target.abstractCreature.rippleLayer == 0
                   && (double) Mathf.Abs(self.target.mainBodyChunk.pos.x - self.center.x) < 300.0;
        }

        static float Hook_SwarmScore(On.LocustSystem.orig_SwarmScore_Creature orig, LocustSystem self, Creature crit)
        {
            if (self.room != null && HasLocustEffect(self.room, out RoomSettings.RoomEffect? effect) && effect!.amount >= 0.95f)
                crit.repelLocusts = 0;
            return orig(self, crit);
        }

        static float Hook_GetAvoidance(On.LocustSystem.orig_GetAvoidanceRadius orig, LocustSystem self, AbstractPhysicalObject obj)
        {
            if (self.room == null
                || !HasLocustEffect(self.room, out RoomSettings.RoomEffect? effect)
                || effect!.amount < 0.95f) return orig(self, obj);
            return Mathf.Clamp((1 - ((self.room.game.timeInRegionThisCycle - 
                (self.room.world.rainCycle.preCycleRain_Intensity > 0 ? self.room.game.timeInRegionThisCycle : StartTime)
                - 1400f) / 1200)), 0f, 1f) * 1000;
        }

        // Convenience method for IL hooking
        public static bool ShouldKillFaster(LocustSystem.Swarm self)
        {
            return HasLocustEffect(self.owner.room, out RoomSettings.RoomEffect? effect)
                   && effect!.amount > 0.7f;
        }
        
        // Allow pathfinding around obstacles above 70% density.
        public static bool CanPathfind(LocustSystem.Swarm self)
        {
            return false;
            //return HasLocustEffect(self.owner.room, out RoomSettings.RoomEffect? effect)
            //       && effect!.amount >= 0.75f;
        }
        
        /*public static void DoPathfind(LocustSystem.Swarm self)
        {
            LogDebug("LocustSwarm.cs: DoPathfind");
            UAD uad = self.owner.room.updateList.OfType<UAD>().FirstOrDefault()!;
            if (!uad.SwarmPaths.ContainsKey(self))
                uad.SwarmPaths.Add(self, new PathingInformation(self));
            
            PathingInformation path = uad.SwarmPaths[self];
            if (path.Nodes.Count < 1)
            {
                if (path.QPF.status == 1)
                    foreach (IntVector2 tile in path.QPF.ReturnPath().tiles)
                        path.Nodes.Add(self.owner.room.GetWorldCoordinate(tile));
                if (path.QPF.status == -1 && self.owner.room.game.timeInRegionThisCycle % 200 == 0)
                    path.QPF.status = 0;
                else
                    path.QPF.status = -1;
                return;
            }
            LogDebug($"LocustSwarm.cs: Dist to target: {Vector2.Distance(self.center, path.Nodes.First().Vec2())}/{Vector2.Distance(self.owner.room.GetTilePosition(self.center).ToVector2(), path.Nodes.First().IntVec2().ToVector2())}");
            LogDebug($"LocustSwarm.cs: Position: {self.center} Target: {path.Nodes.First().Vec2()}");
            LogDebug($"LocustSwarm.cs: IntPosition: {self.owner.room.GetTilePosition(self.center)} IntTarget: {path.Nodes.First().IntVec2()}");
            self.targetPos = path.Nodes.First().Vec2();
            self.disbandCounter = 0;
            if (Vector2.Distance(self.center, self.targetPos) < 500)
            {
                LogDebug("LocustSwarm.cs: Clearing node");
                path.Nodes.RemoveAt(0);
            }
        }*/

        internal static void Setup()
        {
			On.LocustSystem.ExposedToSky += Hook_ExposedToSky;
            On.LocustSystem.Swarm.Update += Hook_SwarmUpdate;
            On.LocustSystem.CanKill += Hook_CanKill;
            On.LocustSystem.Swarm.IsTargetValid += Hook_IsTargetValid;
            On.LocustSystem.SwarmScore_Creature += Hook_SwarmScore;
            On.LocustSystem.GetAvoidanceRadius += Hook_GetAvoidance;
            On.LocustSystem.Update += Hook_LSUpdate;
			On.Player.Update += Update;
            
            LocustSwarmILHooks.Setup();
        }
	}

    internal static class LocustSwarmILHooks
    {
        internal static void Setup()
        {
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
                    ILLabel cont = il.DefineLabel();
                    ILLabel skip = il.DefineLabel();
                    c.Index--;
                    c.Emit(OpCodes.Brtrue_S, cont);
                    c.Emit(OpCodes.Ldarg_0);
                    c.Emit(OpCodes.Callvirt, typeof(LocustSwarm).GetMethod("CanPathfind"));
                    //original brfalse.s IL_02b5
                    c.Index++;
                    c.Emit(OpCodes.Ldarg_0);
                    c.Emit(OpCodes.Callvirt, typeof(LocustSwarm).GetMethod("DoPathfind"));
                    c.Emit(OpCodes.Br, skip);
                    c.MarkLabel(cont);

                    if (c.TryGotoNext(MoveType.Before,
                            x => x.MatchLdarg(0),
                            x => x.MatchLdarg(0),
                            x => x.MatchLdfld(typeof(LocustSystem.Swarm).GetField("locusts"))))
                    {
                        c.MarkLabel(skip);
                        if (c.TryGotoNext(MoveType.After,
                                x => x.MatchConvR4(),
                                x => x.MatchDiv(),
                                x => x.MatchStloc(out _)))
                        {
                            ILLabel cont2 = il.DefineLabel();
                            ILLabel skip2 = il.DefineLabel();
                            c.Index -= 4;
                            c.Emit(OpCodes.Callvirt, typeof(LocustSwarm).GetMethod("ShouldKillFaster"));
                            c.Emit(OpCodes.Brfalse_S, cont2);
                            c.Emit(OpCodes.Ldc_R4, 30f);
                            c.Emit(OpCodes.Br_S, skip2);
                            c.MarkLabel(cont2);
                            c.Emit(OpCodes.Ldarg_0);
                            c.Index++;
                            c.MarkLabel(skip2);
                            LogInfo("LocustSwarm.cs: All IL hooks successfully injected.");
                        }
                        else
                            LogWarning("LocustSwarm.cs: Unable to locate 2nd IL instruction in Swarm.Update()");
                    }
                    else
                        LogWarning("LocustSwarm.cs: Unable to mark skip label in Swarm.Update() - IL instruction not found");
                }
                else
                    LogWarning("LocustSwarm.cs: Failed to inject IL hook in Swarm.Update() - IL instruction not found");
            }
            catch (Exception e)
            {
                LogError($"LocustSwarm.cs: Failed to inject IL hook in Swarm.Update() - {e}");
            }
        }
    }
}

