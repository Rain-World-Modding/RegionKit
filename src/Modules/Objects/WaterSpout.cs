namespace RegionKit.Modules.Objects;

public static class WaterSpoutObjRep
{
    //by LeeMoriya
    internal static void Register()
    {
        List<ManagedField> fields = new List<ManagedField>
        {
            new FloatField("f1", 0f, 50f, 15f, 1f, ManagedFieldWithPanel.ControlType.slider, "Intensity"),
            new Vector2Field("v1", new Vector2(0f,45f), Vector2Field.VectorReprType.line)
        };
        RegisterFullyManagedObjectType(fields.ToArray(), typeof(WaterSpout), "WaterSpout", _Module.DECORATIONS_POM_CATEGORY);
    }
}

public class WaterSpout : UpdatableAndDeletable
{
    public PlacedObject placedObject;
    public float force;
    public SplashWater.WaterJet jet;
    public Vector2 fromPos;
    public Vector2 toPos;
    public RectangularDynamicSoundLoop? soundLoop;

    public WaterSpout(PlacedObject pObj, Room room)
    {
        this.placedObject = pObj;
        this.room = room;
        this.force = ((ManagedData)this.placedObject.data).GetValue<float>("f1");
        this.fromPos = this.placedObject.pos;
        this.toPos = ((ManagedData)this.placedObject.data).GetValue<Vector2>("v1");
        this.jet = new SplashWater.WaterJet(this.room);
    }

    public override void Update(bool eu)
    {
        base.Update(eu);
        this.force = ((ManagedData)this.placedObject.data).GetValue<float>("f1");
        this.fromPos = this.placedObject.pos;
        this.toPos = ((ManagedData)this.placedObject.data).GetValue<Vector2>("v1");
        if(this.jet != null)
        {
            this.jet.Update();
            this.jet.NewParticle(this.fromPos, this.toPos * 0.2f, this.force, 0.5f);
        }
    }
}

