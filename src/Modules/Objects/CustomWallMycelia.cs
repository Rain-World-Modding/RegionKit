using CoralBrain;

namespace RegionKit.Modules.Objects;

public class CustomWallMyceliaData : ManagedData
{
    [Vector2Field("size", 100, 100, Vector2Field.VectorReprType.circle)]
    public Vector2 size;

    [ColorField(nameof(stalkGradStart), 0, 0, 0, controlType: ManagedFieldWithPanel.ControlType.slider, DisplayName: "Stalk Gradient Start")]
    public Color stalkGradStart;

    [ColorField(nameof(stalkGradEnd), 0.1f, 0.3f, 0.287f, controlType: ManagedFieldWithPanel.ControlType.slider, DisplayName: "Stalk Gradient End")]
    public Color stalkGradEnd;

    [ColorField(nameof(tipColor), 0, 0, 1, controlType: ManagedFieldWithPanel.ControlType.slider, DisplayName: "Tip Color")]
    public Color tipColor;

    public CustomWallMyceliaData(PlacedObject owner) : base(owner, null)
    {
    }
}

public class CustomWallMycelia : WallMycelia
{
	public static void Undo()
	{
		On.Room.Loaded -= Room_Loaded;
		On.CoralBrain.CoralNeuronSystem.AIMapReady -= CoralNeuronSystem_AIMapReady;
		On.CoralBrain.WallMycelia.RezData -= WallMyceliaOnRezData;
		On.CoralBrain.Mycelium.UpdateColor -= MyceliumOnUpdateColor;
	}

	public static void Apply()
	{
		On.Room.Loaded += Room_Loaded;
		On.CoralBrain.CoralNeuronSystem.AIMapReady += CoralNeuronSystem_AIMapReady;
		On.CoralBrain.WallMycelia.RezData += WallMyceliaOnRezData;
		On.CoralBrain.Mycelium.UpdateColor += MyceliumOnUpdateColor;
	}

	private static void MyceliumOnUpdateColor(On.CoralBrain.Mycelium.orig_UpdateColor orig, CoralBrain.Mycelium self, Color newColor, float gradientStart, int spr, RoomCamera.SpriteLeaser sLeaser)
	{
		orig(self, newColor, gradientStart, spr, sLeaser);

		if (self.owner is not CustomWallMycelia mycelia || mycelia.places.FirstOrDefault()?.data is not CustomWallMyceliaData data || sLeaser.sprites[spr] is not TriangleMesh mesh) return;

		var stalkGradStart = data.stalkGradStart;
		var stalkGradEnd = data.stalkGradEnd;
		var tipColor = data.tipColor;

		for (int i = 0; i < mesh.verticeColors.Length; i++)
		{
			float value = (float)i / (mesh.verticeColors.Length - 1);
			mesh.verticeColors[i] = Color.Lerp(stalkGradStart, stalkGradEnd, Mathf.InverseLerp(gradientStart, 1f, value));
		}
		for (int j = 1; j < 3; j++)
		{
			mesh.verticeColors[mesh.verticeColors.Length - j] = tipColor;
		}
	}

	private static PlacedObject.ResizableObjectData WallMyceliaOnRezData(On.CoralBrain.WallMycelia.orig_RezData orig, WallMycelia self, int i)
	{
		var result = orig(self, i);

		if (self is CustomWallMycelia)
		{
			var data = (self.places[i].data as CustomWallMyceliaData);
			return new PlacedObject.ResizableObjectData(self.places[i])
			{
				handlePos = data!.size
			};
		}

		return result;
	}

	private static void CoralNeuronSystem_AIMapReady(On.CoralBrain.CoralNeuronSystem.orig_AIMapReady orig, CoralNeuronSystem self)
	{
		orig(self);

		foreach (var customMycelia in self.room.roomSettings.placedObjects.FindAll(x => x.type.value == "CustomWallMycelia" && x.active))
		{
			self.room.AddObject(new CustomWallMycelia(self, new List<PlacedObject> { customMycelia }));
		}
	}

	private static void Room_Loaded(On.Room.orig_Loaded orig, Room self)
	{
		orig(self);
		
		if (self.roomSettings.placedObjects.Any(x => x.type.value == "CustomWallMycelia") && !self.updateList.Any(x => x is CoralNeuronSystem))
		{
			self.AddObject(new CoralNeuronSystem());
			self.waitToEnterAfterFullyLoaded = Math.Max(self.waitToEnterAfterFullyLoaded, 80);
		}
	}

	public CustomWallMycelia(CoralNeuronSystem system, List<PlacedObject> places) : base(system, places)
	{
	}
}
