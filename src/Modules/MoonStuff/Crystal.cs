using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using static RegionKit.Modules.MoonStuff.CrystalType;

#nullable disable

namespace RegionKit.Modules.MoonStuff
{
	public class Crystal : UpdatableAndDeletable, IDrawable
	{

		public PlacedObject placedObject;

		public FAtlas atlas;

		public float Layer => (placedObject.data as CrystalData).lay;

		public float Middle => (placedObject.data as CrystalData).mid;

		public float MiddleWidth => (placedObject.data as CrystalData).midw;

		public float BaseWidth => (placedObject.data as CrystalData).basew;

		public Vector2 Pos => (placedObject.data as CrystalData).pos;

		public HSLColor CrystalColor
		{
			get
			{
				HSLColor col = room.world.region?.MoonRegionData().CrystalColor ?? new HSLColor(0.87f, 0.9f, 0.6f);
				float h = (placedObject.data as CrystalData).CrystalHue == -1f ? col.hue : (placedObject.data as CrystalData).CrystalHue;
				float s = (placedObject.data as CrystalData).CrystalSat == -1f ? col.saturation : (placedObject.data as CrystalData).CrystalSat;
				float l = (placedObject.data as CrystalData).CrystalLit == -1f ? col.lightness : (placedObject.data as CrystalData).CrystalLit;

				return new HSLColor(h, s, l);
			}
		}

		public Crystal(PlacedObject pObj, Room room)
		{
			this.room = room;
			this.placedObject = pObj;
		}

		public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
		{
			if (atlas == null)
			{
				Texture2D texture = new Texture2D(1, 1);
				texture.SetPixel(0, 0, new Color(1f, 1f, 1f, 0.6f));
				atlas = Futile.atlasManager.LoadAtlasFromTexture("CrystalTransparency", texture, false);
			}

			sLeaser.sprites = new FSprite[4];

			sLeaser.sprites[0] = TriangleMesh.MakeLongMesh(1, false, true, "CrystalTransparency");
			sLeaser.sprites[1] = TriangleMesh.MakeLongMesh(1, true, true, "CrystalTransparency");
			sLeaser.sprites[2] = TriangleMesh.MakeLongMesh(1, false, true, "CrystalTransparency");
			sLeaser.sprites[3] = TriangleMesh.MakeLongMesh(1, true, true, "CrystalTransparency");


			sLeaser.sprites[0].shader = rCam.game.rainWorld.Shaders["CustomDepth"];
			sLeaser.sprites[1].shader = rCam.game.rainWorld.Shaders["CustomDepth"];
			sLeaser.sprites[2].shader = rCam.game.rainWorld.Shaders["CustomDepth"];
			sLeaser.sprites[3].shader = rCam.game.rainWorld.Shaders["CustomDepth"];

			AddToContainer(sLeaser, rCam, null);
		}

		public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
		{
			Vector2 startpoint = placedObject.pos - camPos;
			Vector2 endpoint = startpoint + Pos;
			Vector2 midpoint = Vector2.Lerp(startpoint, endpoint, Middle);

			float _basew = BaseWidth * 32;
			float _midw = MiddleWidth * 32;

			float rotation = Custom.AimFromOneVectorToAnother(startpoint, endpoint);

			Vector2 startpointout = Custom.RotateAroundVector(new Vector2(startpoint.x + _basew, startpoint.y), startpoint, rotation);
			Vector2 midpointout = Custom.RotateAroundVector(new Vector2(midpoint.x + _midw, midpoint.y), midpoint, rotation);

			Vector2 startpointoutflip = Custom.RotateAroundVector(new Vector2(startpoint.x - _basew, startpoint.y), startpoint, rotation);
			Vector2 midpointoutflip = Custom.RotateAroundVector(new Vector2(midpoint.x - _midw, midpoint.y), midpoint, rotation);

			((TriangleMesh)sLeaser.sprites[0]).MoveVertice(0, startpoint);
			((TriangleMesh)sLeaser.sprites[0]).MoveVertice(1, startpointout);
			((TriangleMesh)sLeaser.sprites[0]).MoveVertice(2, midpoint);
			((TriangleMesh)sLeaser.sprites[0]).MoveVertice(3, midpointout);

			((TriangleMesh)sLeaser.sprites[1]).MoveVertice(0, midpoint);
			((TriangleMesh)sLeaser.sprites[1]).MoveVertice(1, midpointout);
			((TriangleMesh)sLeaser.sprites[1]).MoveVertice(2, endpoint);

			((TriangleMesh)sLeaser.sprites[2]).MoveVertice(0, startpoint);
			((TriangleMesh)sLeaser.sprites[2]).MoveVertice(1, startpointoutflip);
			((TriangleMesh)sLeaser.sprites[2]).MoveVertice(2, midpoint);
			((TriangleMesh)sLeaser.sprites[2]).MoveVertice(3, midpointoutflip);

			((TriangleMesh)sLeaser.sprites[3]).MoveVertice(0, midpoint);
			((TriangleMesh)sLeaser.sprites[3]).MoveVertice(1, midpointoutflip);
			((TriangleMesh)sLeaser.sprites[3]).MoveVertice(2, endpoint);

			float _depth = 1f - (Layer - 1) / 30f;

			Color fog = new Color(rCam.currentPalette.fogColor.r, rCam.currentPalette.fogColor.g, rCam.currentPalette.fogColor.b, _depth);
			Color crystalcolor = Custom.HSL2RGB(CrystalColor.hue, CrystalColor.saturation, CrystalColor.lightness);

			((TriangleMesh)sLeaser.sprites[0]).color = Color.Lerp(fog, crystalcolor, _depth);
			((TriangleMesh)sLeaser.sprites[1]).color = Color.Lerp(fog, Color.Lerp(crystalcolor, new Color(1f, 1f, 1f, _depth), 0.05f), _depth);
			((TriangleMesh)sLeaser.sprites[2]).color = Color.Lerp(fog, Color.Lerp(crystalcolor, new Color(1f, 1f, 1f, _depth), 0.2f), _depth);
			((TriangleMesh)sLeaser.sprites[3]).color = Color.Lerp(fog, Color.Lerp(crystalcolor, new Color(1f, 1f, 1f, _depth), 0.3f), _depth);

			if (base.slatedForDeletetion || room != rCam.room)
			{
				sLeaser.CleanSpritesAndRemove();
			}
		}

		public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette) { }

		public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
		{
			if (newContatiner == null)
			{
				newContatiner = rCam.ReturnFContainer("Water");
			}

			FSprite[] sprites = sLeaser.sprites;
			foreach (FSprite fSprite in sprites)
			{
				fSprite.RemoveFromContainer();
				newContatiner.AddChild(fSprite);
			}
		}
	}
}
