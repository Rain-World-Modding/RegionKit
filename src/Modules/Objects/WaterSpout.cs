using System.Runtime.CompilerServices;
using Mono.Cecil.Cil;
using MonoMod.Cil;

namespace RegionKit.Modules.Objects;

public class WaterSpout : UpdatableAndDeletable
{
	//by LeeMoriya
	internal static void Register()
	{
		List<ManagedField> fields = new List<ManagedField>
		{
			new FloatField("f1", 0f, 50f, 15f, 1f, ManagedFieldWithPanel.ControlType.slider, "Intensity"),
			new FloatField("f2", 0f, 1f, 1f, 0.01f, ManagedFieldWithPanel.ControlType.slider, "Volume"),
			new Vector2Field("v1", new Vector2(0f,45f), Vector2Field.VectorReprType.line)
		};
		RegisterFullyManagedObjectType(fields.ToArray(), typeof(WaterSpout), "WaterSpout", _Module.DECORATIONS_POM_CATEGORY);
	}

	internal static void Apply()
	{
		try { IL.SplashWater.WaterParticle.Update += WaterParticle_Update; }
		catch (Exception e) { LogError("WaterSpout failed to il hook WaterParticle.Update\n" + e); }

		try { IL.SplashWater.JetWater.Update += WaterParticle_Update; }
		catch (Exception e) { LogError("WaterSpout failed to il hook JetWater.Update\n" + e); }

		try { IL.SplashWater.Splash.Update += WaterParticle_Update; }
		catch (Exception e) { LogError("WaterSpout failed to il hook Splash.Update\n" + e); }
	}

	private static ConditionalWeakTable<SplashWater.WaterParticle, StrongBox<float>> _particleVolume = new();
	public static StrongBox<float> ParticleVolume(SplashWater.WaterParticle p) => _particleVolume.GetValue(p, _ => new(1f));

	internal static void Undo()
	{
		IL.SplashWater.WaterParticle.Update -= WaterParticle_Update;
		IL.SplashWater.JetWater.Update -= WaterParticle_Update;
		IL.SplashWater.Splash.Update -= WaterParticle_Update;
	}

	private static void WaterParticle_Update(ILContext il)
	{
		var c = new ILCursor(il);
		c.GotoNext(MoveType.Before,
			x => x.MatchLdcR4(out _),
			x => x.MatchCallvirt<Room>(nameof(Room.PlaySound)));
		c.Emit(OpCodes.Ldarg_0);
		c.EmitDelegate((float orig, SplashWater.WaterParticle self) => { return orig * WaterSpout.ParticleVolume(self).Value; });
	}
	public PlacedObject placedObject;
	public ManagedData data;
	public SplashWater.WaterJet jet;
	public float volume => data.GetValue<float>("f2");
	public float force => data.GetValue<float>("f1");
	public Vector2 fromPos => placedObject.pos;
    public Vector2 toPos => data.GetValue<Vector2>("v1");

    public WaterSpout(PlacedObject pObj, Room room)
    {
		this.placedObject = pObj;
		this.data = (ManagedData)pObj.data;
		this.room = room;
        this.jet = new SplashWater.WaterJet(this.room);
    }

    public override void Update(bool eu)
    {
        base.Update(eu);
        if(this.jet != null)
        {
            this.jet.Update();
            this.jet.NewParticle(this.fromPos, this.toPos * 0.2f, this.force, 0.5f);
			ParticleVolume(jet.particles[jet.particles.Count - 1]).Value = volume;
		}
    }
}

