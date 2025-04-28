using System;
using System.IO;
using RegionKit.Modules.AnimatedDecals;

namespace RegionKit.Modules.Objects;
/// <summary>
/// A customizeable freeform sprite
/// </summary>
public class Drawable : CosmeticSprite
{
	internal static ManagedField[] __fields = {
		new Vector2ArrayField("quad", 4, true, Vector2ArrayField.Vector2ArrayRepresentationType.Polygon, Vector2.zero, Vector2.right * 20f, (Vector2.right + Vector2.up) * 20f, Vector2.up * 20f),
		new StringField("spriteName", "Futile_White", "Decal Name"),
		new FloatField("depth", 0f, 1f, 1f, displayName: "Depth"),
		new StringField("shader", "Basic", "Shader"),
		new EnumField<FContainer>("container", FContainer.Foreground, displayName: "FContainer"),
		new IntegerField("alpha", 0, 255, 255, ManagedFieldWithPanel.ControlType.slider, "Alpha"),
		new BooleanField("useColour", false, displayName: "Use Colour"),
		new ColorField("colour", Color.white, ManagedFieldWithPanel.ControlType.button, "Colour")
	};
	/// <summary>
	/// POM ctor
	/// </summary>
	public Drawable(PlacedObject pObj, Room room)
	{
		this.room = room;
		_LocalPlacedObject = pObj;
	}
	/// <summary>
	/// Enum for container codes
	/// </summary>
	public enum FContainer
	{
		#pragma warning disable 1591
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
		#pragma warning restore 1591
	}

	private ManagedData _Data => (ManagedData)_LocalPlacedObject.data;
	private Vector2 _PlacedObjectTile => _LocalPlacedObject.pos;
	private PlacedObject _LocalPlacedObject { get; }
	///<inheritdoc/>
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

	private Vector2[] _Quad
	{
		get
		{
			var vecs = _Data.GetValue<Vector2[]>("quad")!;
			return new[]
			{
				vecs[0],
				vecs[1],
				vecs[3],
				vecs[2]
			};
		}
	}
	///<inheritdoc/>
	public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
	{
		base.DrawSprites(sLeaser, rCam, timeStacker, camPos);
		sLeaser.sprites[0].alpha = _Data.GetValue<int>("alpha") / 255f;
		rCam.ReturnFContainer(_Data.GetValue<FContainer>("container").ToString())
			.AddChildAtIndex(sLeaser.sprites[0],
				Mathf.FloorToInt(
					_Data.GetValue<float>("depth") *
					rCam.ReturnFContainer(_Data.GetValue<FContainer>("container").ToString())
						.GetChildCount()));
		try
		{
			sLeaser.sprites[0].SetElementByName(_Data.GetValue<string>("spriteName"));
			UpdateUV(sLeaser);
		}
		catch (FutileException)
		{
			try
			{
				LoadFile(_Data.GetValue<string>("spriteName") ?? "INVALID_SPRITE_NAME");
				sLeaser.sprites[0].SetElementByName(_Data.GetValue<string>("spriteName"));
				UpdateUV(sLeaser);
			}
			catch (Exception e) when (e is FutileException or IOException)
			{
				//ignored
			}
		}

		for (int i = 0; i < 4; i++)
		{
			((TriangleMesh)sLeaser.sprites[0]).MoveVertice(i, _PlacedObjectTile + _Quad[i] - camPos);
		}

		if (rCam.game.rainWorld.Shaders.ContainsKey(_Data.GetValue<string>("shader")))
		{
			sLeaser.sprites[0].shader = rCam.game.rainWorld.Shaders[_Data.GetValue<string>("shader")];
		}

		var col = _Data.GetValue<Color>("colour");
		col.a = _Data.GetValue<int>("alpha") / 255f;
		sLeaser.sprites[0].color = _Data.GetValue<bool>("useColour") ? col : new Color(Color.white.r, Color.white.g, Color.white.b, _Data.GetValue<int>("alpha") / 255f);
	}

	public void UpdateUV(RoomCamera.SpriteLeaser sLeaser)
	{
		sLeaser.sprites[0].SetElementByName(_Data.GetValue<string>("spriteName"));
		var mesh = (TriangleMesh)sLeaser.sprites[0];
		mesh.UVvertices[0] = mesh.element.uvBottomLeft;
		mesh.UVvertices[1] = mesh.element.uvBottomRight;
		mesh.UVvertices[2] = mesh.element.uvTopLeft;
		mesh.UVvertices[3] = mesh.element.uvTopRight;
	}

	public void LoadFile(string fileName)
	{
		if (VideoManager.IsVideoFile(fileName))
		{
			string path = AssetManager.ResolveFilePath("Decals" + Path.DirectorySeparatorChar + fileName);
			VideoManager.LoadAndCacheVideo(fileName, path);
		}
		else
		{
			if (Futile.atlasManager.GetAtlasWithName(fileName) != null)
			{
				return;
			}
			string str = AssetManager.ResolveFilePath("Decals" + Path.DirectorySeparatorChar.ToString() + fileName + ".png");
			Texture2D texture = new Texture2D(1, 1, TextureFormat.ARGB32, false);
			AssetManager.SafeWWWLoadTexture(ref texture, "file:///" + str, true, true);
			HeavyTexturesCache.LoadAndCacheAtlasFromTexture(fileName, texture, false);
		}
	}
}
