using System.Runtime.CompilerServices;
using Random = UnityEngine.Random;

namespace RegionKit.Modules.Objects;

public class SlugcatEyeSelectorData : ManagedData
{
    [Vector2Field("size", 100, 100, Vector2Field.VectorReprType.rect)]
    public Vector2 size;

    [ExtEnumField<EyeMode>("eyeMode", nameof(EyeMode.Closed), new[] { nameof(EyeMode.Closed), nameof(EyeMode.Stunned), nameof(EyeMode.Dead), nameof(EyeMode.ReverseBlink), nameof(EyeMode.CustomBlink) }, displayName: "Eye Mode")]
    public EyeMode mode;

    [FloatField("customFrequency", 0, 1, 0.5f, 0.01f, displayName: "Custom Frequency")]
    public float customFrequency;

    [IntegerField("customDurationMin", 0, 400, 3, control: ManagedFieldWithPanel.ControlType.text, displayName: "Custom Duration Min")]
    public int customDurationMin;

    [IntegerField("customDurationMax", 0, 400, 10, control: ManagedFieldWithPanel.ControlType.text, displayName: "Custom Duration Max")]
    public int customDurationMax;

    [FloatField("beforeCycle", 0, 1, 0, 0.01f, displayName: "Before Cycle")]
    public float beforeCycle;

    [FloatField("afterCycle", 0, 1, 0, 0.01f, displayName: "After Cycle")]
    public float afterCycle;

    public SlugcatEyeSelectorData(PlacedObject owner) : base(owner, null)
    {
    }
}

public class EyeMode : ExtEnum<EyeMode>
{
    public EyeMode(string value, bool register = false) : base(value, register)
    {
    }
    
    public static readonly EyeMode Closed = new(nameof(Closed), true);
    public static readonly EyeMode Stunned = new(nameof(Stunned), true);
    public static readonly EyeMode Dead = new(nameof(Dead), true);
    public static readonly EyeMode ReverseBlink = new(nameof(ReverseBlink), true);
    public static readonly EyeMode CustomBlink = new(nameof(CustomBlink), true);
}

public class ForcedEyePlayerData
{
	public bool eu;
	public EyeMode? mode;
    public int customBlink;
    public float frequency;
    public int durationMin;
    public int durationMax;

    private static ConditionalWeakTable<PlayerGraphics, ForcedEyePlayerData> _cwt = new();
    
    public static ForcedEyePlayerData Get(PlayerGraphics pg) => _cwt.GetValue(pg, _ => new());
}

public class SlugcatEyeSelector : UpdatableAndDeletable
{
    private PlacedObject placedObject;
    
    public static void Apply()
    {
        On.PlayerGraphics.Update += PlayerGraphics_Update;
        On.PlayerGraphics.DrawSprites += PlayerGraphics_DrawSprites;
    }

    public static void Undo()
    {
	    On.PlayerGraphics.Update -= PlayerGraphics_Update;
	    On.PlayerGraphics.DrawSprites -= PlayerGraphics_DrawSprites;
    }

    private static void PlayerGraphics_Update(On.PlayerGraphics.orig_Update orig, PlayerGraphics self)
    {
        orig(self);

        var data = ForcedEyePlayerData.Get(self);
        
        if (data.mode == EyeMode.CustomBlink)
        {
            data.customBlink--;
            var maxRange = (int)((1 - data.frequency) * 3600);
            if (data.customBlink < -Random.Range(2, maxRange))
            {
                data.customBlink = Random.Range(data.durationMin, data.durationMax);
            }
        }
    }

    private static void PlayerGraphics_DrawSprites(On.PlayerGraphics.orig_DrawSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
        var data = ForcedEyePlayerData.Get(self);
        var mode = data.mode;

        int? oldBlink = null;
        int? oldStun = null;
        bool? oldDead = null;

        if (mode == EyeMode.Closed)
        {
            oldBlink = self.blink;
            self.blink = 1;
        }
        else if (mode == EyeMode.Stunned)
        {
            oldStun = self.player.stun;
            self.player.stun = 10;
        }
        else if (mode == EyeMode.Dead)
        {
            oldDead = self.player.dead;
            self.player.dead = true;
        }
        else if (mode == EyeMode.ReverseBlink)
        {
            oldBlink = self.blink;
            self.blink = self.blink > 0 ? 0 : 1;
        }
        else if (mode == EyeMode.CustomBlink)
        {
            self.blink = data.customBlink;
        }

        orig(self, sLeaser, rCam, timeStacker, camPos);

        if (oldBlink.HasValue)
        {
            self.blink = oldBlink.Value;
        }
        if (oldStun.HasValue)
        {
            self.player.stun = oldStun.Value;
        }
        if (oldDead.HasValue)
        {
            self.player.dead = oldDead.Value;
        }
    }

    public SlugcatEyeSelector(PlacedObject placedObject, Room room)
    {
        this.placedObject = placedObject;
    }

    public override void Update(bool eu)
    {
        base.Update(eu);

        var data = (placedObject.data as SlugcatEyeSelectorData)!;

        var pos = placedObject.pos;

        if (data.size.x < 0)
        {
            data.size.x = -data.size.x;
            pos.x -= data.size.x;
        }
        if (data.size.y < 0)
        {
            data.size.y = -data.size.y;
            pos.y -= data.size.y;
        }
        
        var affectedRect = new Rect(pos, data.size);

        var cycleProgression = 1 - room.game.world.rainCycle.AmountLeft;
        foreach (var player in room.PlayersInRoom)
        {
            if (player.graphicsModule is not PlayerGraphics pg) continue;

            var pgData = ForcedEyePlayerData.Get(pg);

            //-- Don't apply more than once in the same update, in case there is overlap between multiple objects
            if (pgData.eu == eu) continue;

            if ((data.afterCycle == 0 || cycleProgression > data.afterCycle) && (data.beforeCycle == 0 || cycleProgression < data.beforeCycle) && affectedRect.Contains(player.mainBodyChunk.pos))
            {
	            pgData.mode = data.mode;
	            pgData.frequency = data.customFrequency;
	            pgData.durationMin = data.customDurationMin;
	            pgData.durationMin = data.customDurationMax;
	            pgData.eu = eu;
            }
            else
            {
	            pgData.mode = null;
            }
        }
    }
}
