using HUD;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RegionKit.Modules.CustomProjections;
using UnityEngine;
using Random = UnityEngine.Random;

namespace RegionKit.Modules.Misc;

/// <summary>
/// Miscellaneous region configuration options that can be defined in a region's properties or world file.
/// </summary>
public abstract class RegionConfigs
{
    public static void Apply()
    {
        On.Region.ctor_string_int_int_RainWorldGame_Timeline += Hook_RegionConstructor;
        On.RainCycle.GetDesiredCycleLength += Hook_DesiredCycleLength;
        On.HUD.RainMeter.Draw += Hook_RainMeterDraw;
    }

    public static void Undo()
    {
        On.Region.ctor_string_int_int_RainWorldGame_Timeline -= Hook_RegionConstructor;
        On.RainCycle.GetDesiredCycleLength -= Hook_DesiredCycleLength;
        On.HUD.RainMeter.Draw -= Hook_RainMeterDraw;
    }

    private static void Hook_RegionConstructor(On.Region.orig_ctor_string_int_int_RainWorldGame_Timeline orig,
        Region self, string name, int firstRoomIndex, int regionNumber, RainWorldGame game, SlugcatStats.Timeline timelineIndex)
    {
        orig(self, name, firstRoomIndex, regionNumber, game, timelineIndex);
        foreach (KeyValuePair<string,string> pair in self.regionParams.unrecognizedParams)
        foreach (CustomProperty prop in Properties)
            prop.Match(pair, self);
    }

    public class CustomProperty(string name, Action<KeyValuePair<string,string>,Region> method, bool startsWith = false)
    {
        public bool Match(KeyValuePair<string,string> pair, Region region)
        {
            if (pair.Key != name && (!pair.Key.StartsWith(name) || !startsWith))
                return false;
            method(pair, region);
            return true;
        }
    }
    public static List<CustomProperty> Properties = new()
    {
        new CustomProperty("guideDestinationRoom", (x,r) =>
            OverseerProperties.GetOverseerProperties(r).CustomDestinationRoom = x.Value),
        new CustomProperty("guideProgressionSymbol", (x,r) =>
            OverseerProperties.GetOverseerProperties(r).ProgressionSymbol = x.Value),
        new CustomProperty("guideShelterWeight", (x,r) =>
        {
            if (float.TryParse(x.Value, out float y))
                OverseerProperties.GetOverseerProperties(r).ShelterShowWeight = y;
        }),
        new CustomProperty("guideBatWeight", (x,r) =>
        {
            if (float.TryParse(x.Value, out float y))
                OverseerProperties.GetOverseerProperties(r).BatShowWeight = y;
        }),
        new CustomProperty("guideProgressionWeight", (x,r) =>
        {
            if (float.TryParse(x.Value, out float y))
                OverseerProperties.GetOverseerProperties(r).ProgressionShowWeight = y;
        }),
        new CustomProperty("guideDangerousCreatureWeight", (x,r) =>
        {
            if (float.TryParse(x.Value, out float y))
                OverseerProperties.GetOverseerProperties(r).DangerousCreatureWeight = y;
        }),
        new CustomProperty("guideDeliciousFoodWeight", (x,r) =>
        {
            if (float.TryParse(x.Value, out float y))
                OverseerProperties.GetOverseerProperties(r).DeliciousFoodWeight = y;
        }),
        new CustomProperty("guideColor", (x,r) =>
        {
            if (OverseerProperties.TryParseOverseerColor(x.Value, out Color y))
                OverseerProperties.GetOverseerProperties(r).GuideColor = y;
        }),
        new CustomProperty("inspectorColor", (x,r) =>
        {
            if (OverseerProperties.TryParseOverseerColor(x.Value, out Color y))
                OverseerProperties.GetOverseerProperties(r).InspectorColor = y;
        }),
        new CustomProperty("overseersColorOverride", (x,r) =>
        {
            int a = x.Key.IndexOf('(') + 1, b = x.Key.IndexOf(')');
            if (a != 1
                && a < b 
                && OverseerProperties.TryParseOverseerColor(x.Key.Substring(a,b - a), out Color y)
                && float.TryParse(x.Value, out float z))
                OverseerProperties.GetOverseerProperties(r).overseerColorChances[y] = z;
        }, true),
        new CustomProperty("minCycleLength", (x, r) =>
        {
            if (int.TryParse(x.Value, out int y))
                GetCycleLength(r).Min = y;
        }),
        new CustomProperty("maxCycleLength", (x, r) =>
        {
            if (int.TryParse(x.Value, out int y))
                GetCycleLength(r).Max = y;
        }),
        new CustomProperty("trueHideTimer", (x, r) =>
        {
            if (x.Value != "true")
                return;
            if (TrueHideTimer!.TryGetValue(r, out _))
                TrueHideTimer[r] = true;
            else
                TrueHideTimer.Add(r, true);
        })
    };

    public static Dictionary<Region, CycleLength> CycleLengths = new();
    public static CycleLength GetCycleLength(Region r)
    {
        if (CycleLengths.TryGetValue(r, out CycleLength c))
            return c;
        CycleLength x = new(-1, -1);
        CycleLengths.Add(r, x);
        return x;
    }
    public class CycleLength(int min, int max)
    {
        public int Min = min;
        public int Max = max;
    }

    public static Dictionary<Region, bool> TrueHideTimer = new();
    public static bool ShouldHideTimer(Region r)
    {
        if (TrueHideTimer.TryGetValue(r, out bool x))
            return x;
        TrueHideTimer.Add(r, false);
        return false;
    }

    internal static int Hook_DesiredCycleLength(On.RainCycle.orig_GetDesiredCycleLength orig, RainCycle self)
    {
        CycleLength c = GetCycleLength(self.world.region);
        if (c.Min > 0 && c.Max > 0 && c.Min <= c.Max)
            return Random.Range(c.Min, c.Max) * 40;
        return orig(self);
    }

    internal static void Hook_RainMeterDraw(On.HUD.RainMeter.orig_Draw orig, RainMeter self, float timeStacker)
    {
        if (ShouldHideTimer((self.hud.owner as Player)!.abstractCreature.world.region))
            return;
        orig(self, timeStacker);
    }
}
