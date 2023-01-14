using System;

namespace RegionKit.Modules.Objects;

public class Drawable : CosmeticSprite
{
	public static ManagedField[] Fields = {
		new Vector2ArrayField("quad", 4, true, Vector2ArrayField.Vector2ArrayRepresentationType.Polygon, Vector2.zero, Vector2.right * 20f, (Vector2.right + Vector2.up) * 20f, Vector2.up * 20f),
		new StringField("spriteName", "Futile_White", "Decal Name"),
		new FloatField("depth", 0f, 1f, 1f, displayName: "Depth"),
		new StringField("shader", "Basic", "Shader"),
		new EnumField<FContainer>("container", FContainer.Foreground, displayName: "FContainer"),
		new IntegerField("alpha", 1, 255, 255, ManagedFieldWithPanel.ControlType.slider, "Alpha"),
		new BooleanField("useColour", false, displayName: "Use Colour"),
		new ColorField("colour", Color.white, ManagedFieldWithPanel.ControlType.slider, "Colour")
	};

	public Drawable(PlacedObject pObj, Room room)
	{
		this.room = room;
		LocalPlacedObject = pObj;
	}

	public enum FContainer
	{
		Shadows,
		BackgroundShortcuts,
		Background,
		Midground,
		Items,
		Foreground,
		ForegroundLights,
		Shortcuts,
		Water,
		GrabShaders,
		Bloom,
		HUD,
		HUD2
	}

	public ManagedData Data => (LocalPlacedObject.data as ManagedData)!;

	public Vector2 PlacedObjectTile => LocalPlacedObject.pos;


	public PlacedObject LocalPlacedObject { get; }

	public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
	{
		base.InitiateSprites(sLeaser, rCam);
		TriangleMesh.Triangle[] triangles = new TriangleMesh.Triangle[2];
		triangles[0] = new TriangleMesh.Triangle(0, 1, 2);
		triangles[1] = new TriangleMesh.Triangle(1, 2, 3);
		TriangleMesh mesh = new TriangleMesh("Futile_White", triangles, true)
		{
			UVvertices = {
				[0] = new Vector2(0, 0),
				[1] = new Vector2(1, 0),
				[2] = new Vector2(0, 1),
				[3] = new Vector2(1, 1)
			}
		};
		sLeaser.sprites = new FSprite[] { mesh };
	}

	public Vector2[] Quad
	{
		get
		{
			var vecs = Data.GetValue<Vector2[]>("quad")!;
			return new[]
			{
				vecs[0],
				vecs[1],
				vecs[3],
				vecs[2]
			};
		}
	}

	public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
	{
		base.DrawSprites(sLeaser, rCam, timeStacker, camPos);
		sLeaser.sprites[0].alpha = Data.GetValue<int>("alpha") / 255f;
		rCam.ReturnFContainer(Data.GetValue<FContainer>("container").ToString())
			.AddChildAtIndex(sLeaser.sprites[0],
				Mathf.FloorToInt(
					Data.GetValue<float>("depth") *
					rCam.ReturnFContainer(Data.GetValue<FContainer>("container").ToString())
						.GetChildCount()));
		try
		{
			sLeaser.sprites[0].SetElementByName(Data.GetValue<string>("spriteName"));
		}
		catch (FutileException)
		{
			try
			{
				//TODO: test if changed io works
				WWW www = new WWW(AssetManager.ResolveFilePath($"decals/{Data.GetValue<string>("SpriteName")}"));
				Texture2D tex = new Texture2D(1, 1, TextureFormat.ARGB32, false)
				{
					wrapMode = TextureWrapMode.Clamp,
					anisoLevel = 0,
					filterMode = FilterMode.Point
				};
				www.LoadImageIntoTexture(tex);
				HeavyTexturesCache.LoadAndCacheAtlasFromTexture(Data.GetValue<string>("spriteName"), tex, false);
				sLeaser.sprites[0].SetElementByName(Data.GetValue<string>("spriteName"));
			}
			catch (Exception e) when (e is FutileException)
			{
				//ignored
			}
			catch (Exception e) when (e is IO.IOException)
			{
				//ignored
			}
		}

		for (int i = 0; i < 4; i++)
		{
			((TriangleMesh)sLeaser.sprites[0]).MoveVertice(i, PlacedObjectTile + Quad[i] - camPos);
		}

		if (rCam.game.rainWorld.Shaders.ContainsKey(Data.GetValue<string>("shader")))
		{
			sLeaser.sprites[0].shader = rCam.game.rainWorld.Shaders[Data.GetValue<string>("shader")];
		}

		var col = Data.GetValue<Color>("colour");
		col.a = Data.GetValue<int>("alpha") / 255f;
		sLeaser.sprites[0].color = Data.GetValue<bool>("useColour") ? col : new Color(Color.white.r, Color.white.g, Color.white.b, Data.GetValue<int>("alpha") / 255f);
	}

	public static void Register() => RegisterFullyManagedObjectType(Fields, typeof(Drawable), "FreeformDecalOrSprite");
}
