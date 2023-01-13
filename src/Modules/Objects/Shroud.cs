namespace RegionKit.Modules.Objects;

public static class ShroudObjRep
{
	internal static void ShroudRep()
	{
		List<ManagedField> fields = new List<ManagedField>
	{
		new Vector2ArrayField("quad", 4, true, Vector2ArrayField.Vector2ArrayRepresentationType.Polygon, Vector2.zero, Vector2.right * 20f, (Vector2.right + Vector2.up) * 20f, Vector2.up * 20f)
	};
		RegisterFullyManagedObjectType(fields.ToArray(), typeof(Shroud));
	}
}

public class Shroud : CosmeticSprite
{
	public PlacedObject pObj;
	public FloatRect rect;
	public Vector2[] quad;
	public float alpha;
	public bool active;
	public bool playerInside;
	public int ID;

	public Shroud(PlacedObject pObj, Room room)
	{
		this.pObj = pObj;
		this.room = room;
		alpha = 1f;
		quad = (this.pObj.data as ManagedData).GetValue<Vector2[]>("quad");
		//this.rect = new FloatRect(quad[0],quad[1],quad[2],quad[3]);
	}
	public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
	{
		sLeaser.sprites = new FSprite[1];
		var tris = new TriangleMesh.Triangle[]
		{
			new TriangleMesh.Triangle(0, 1, 2),
			new TriangleMesh.Triangle(2, 1, 3)
		};
		var mesh = new TriangleMesh("Futile_White", tris, false);
		//Bottom left
		mesh.MoveVertice(0, new Vector2(0f, 0f));
		//Top left
		mesh.MoveVertice(1, new Vector2(0f, 1f));
		//Bottom right
		mesh.MoveVertice(2, new Vector2(1f, 0f));
		//Top right
		mesh.MoveVertice(3, new Vector2(1f, 1f));

		mesh.UVvertices[0] = new Vector2(0f, 0f);
		mesh.UVvertices[1] = new Vector2(0f, 1f);
		mesh.UVvertices[2] = new Vector2(1f, 0f);
		mesh.UVvertices[3] = new Vector2(1f, 1f);

		sLeaser.sprites[0] = mesh;
		//sLeaser.sprites[0].shader = rCam.game.rainWorld.Shaders["Background"];
		AddToContainer(sLeaser, rCam, rCam.ReturnFContainer("GrabShaders"));
	}

	public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
	{
		(sLeaser.sprites[0] as TriangleMesh).MoveVertice(0, pObj.pos - camPos);
		(sLeaser.sprites[0] as TriangleMesh).MoveVertice(1, pObj.pos + quad[1] - camPos);
		(sLeaser.sprites[0] as TriangleMesh).MoveVertice(2, pObj.pos + quad[3] - camPos);
		(sLeaser.sprites[0] as TriangleMesh).MoveVertice(3, pObj.pos + quad[2] - camPos);
		sLeaser.sprites[0].alpha = alpha;
		sLeaser.sprites[0].color = rCam.PixelColorAtCoordinate(pObj.pos);
		base.DrawSprites(sLeaser, rCam, timeStacker, camPos);
	}

	public override void Update(bool eu)
	{
		quad = (pObj.data as ManagedData).GetValue<Vector2[]>("quad");
		Vector2 camPos = room.game.cameras[0].pos;
		Vector2[] poly = new Vector2[]
		{
		pObj.pos - camPos,
		pObj.pos + quad[1]- camPos,
		pObj.pos + quad[3]- camPos,
		pObj.pos + quad[2]- camPos,
		};

		if (active)
		{
			alpha -= 0.03f;
		}
		else
		{
			alpha += 0.05f;
		}

		alpha = Mathf.Clamp(alpha, 0f, 1f);

		base.Update(eu);
	}

	public override void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
	{
		sLeaser.sprites[0].color = rCam.PixelColorAtCoordinate(pObj.pos);
		base.ApplyPalette(sLeaser, rCam, palette);
	}
}
