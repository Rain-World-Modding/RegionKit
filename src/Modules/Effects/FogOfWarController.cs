using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using RWCustom;

namespace RegionKit.Modules.Effects;

/// <summary>
/// Controls sprites and logic for line-of-sight limiting fog. Copies behavior from the Line of Sight mod.
/// </summary>
public class FogOfWarController : CosmeticSprite
{
	internal static bool hackToDelayDrawingUntilAfterTheLevelMoves;

	private const bool showShortcuts = true;

	private int x;
	private int y;
	public float lastScreenblockAlpha = 1f;
	public float screenblockAlpha = 1f;
	public bool hideAllSprites = false;
	private float peekAlpha;
	private float lastPeekAlpha;
	private Vector2 peekPos;
	private float peekAngle;
	private Vector2? overrideEyePos;
	private Vector2 lastOverrideEyePos;

	private Room.Tile[,] tiles;

	private static FShader fovShader;
	private FShader shader;

	private ShortcutDisplay shortcutDisplay;

	public enum FogType
	{
		None,
		Solid,
		Darkened
	}

	public enum MappingState
	{
		FindingEdges,
		DuplicatingPoints,
		Done
	}
	public MappingState state;

	public static RoomSettings.RoomEffect.Type GetEffectType(FogType fogType)
	{
		switch (fogType)
		{
		case FogType.Darkened: return NewEffects.FogOfWarDarkened;
		case FogType.Solid: return NewEffects.FogOfWarSolid;
		default: return RoomSettings.RoomEffect.Type.None;
		}
	}
	public static FogType GetFogType(RoomSettings.RoomEffect.Type effectType)
	{
		if (effectType == NewEffects.FogOfWarDarkened) return FogType.Darkened;
		if (effectType == NewEffects.FogOfWarSolid) return FogType.Solid;
		return FogType.None;
	}

	public FogType Type
	{
		get
		{
			foreach (var effect in room.roomSettings.effects)
			{
				var type = GetFogType(effect.type);
				if (type != FogType.None)
					return type;
			}
			return FogType.None;
		}
	}
	public float Lightness => Type == FogType.Solid ? room.roomSettings.GetEffectAmount(GetEffectType(Type)) : 0f;
	public float Amount => Type == FogType.Solid ? 1f : room.roomSettings.GetEffectAmount(GetEffectType(Type));

	public FogOfWarController(Room room)
	{
		x = 0;
		y = 0;

		if (fovShader == null)
		{
			//TODO: make the shader load properly
			Material mat = new Material(_Assets.FogOfWar);
			fovShader = FShader.CreateShader("FogOfWar", mat.shader);
		}

		shader = fovShader;

		// Create a copy of the room's tiles that the mapper can use
		// Gates and shelter doors can modify this array, so it must be grabbed as early as possible
		Room.Tile[,] fromTiles = room.Tiles;
		tiles = new Room.Tile[fromTiles.GetLength(0), fromTiles.GetLength(1)];
		Array.Copy(fromTiles, tiles, fromTiles.Length);
	}

	public List<Vector2> corners = new List<Vector2>();
	public List<int> edges = new List<int>();

	private enum Direction
	{
		Up,
		Right,
		Down,
		Left
	}

	private bool HasEdge(int x, int y, Direction dir)
	{
		Room.Tile tile = room.GetTile(x, y);
		Room.Tile.TerrainType terrain = tile.Terrain;
		Room.SlopeDirection slope = (terrain == Room.Tile.TerrainType.Slope) ? room.IdentifySlope(x, y) : Room.SlopeDirection.Broken;

		if (terrain == Room.Tile.TerrainType.Solid) return true;
		if (terrain == Room.Tile.TerrainType.Air ||
			terrain == Room.Tile.TerrainType.ShortcutEntrance ||
			terrain == Room.Tile.TerrainType.Floor) return false;
		switch (dir)
		{
		case Direction.Up:
			return slope == Room.SlopeDirection.DownRight || slope == Room.SlopeDirection.DownLeft;
		case Direction.Right:
			return slope == Room.SlopeDirection.UpLeft || slope == Room.SlopeDirection.DownLeft;
		case Direction.Down:
			return slope == Room.SlopeDirection.UpRight || slope == Room.SlopeDirection.UpLeft;
		case Direction.Left:
			return slope == Room.SlopeDirection.DownRight || slope == Room.SlopeDirection.UpRight;
		}
		return false;
	}

	private int AddCorner(Vector2 pos)
	{
		int ind = corners.IndexOf(pos);
		if (ind == -1)
		{
			corners.Add(pos);
			ind = corners.Count - 1;
		}
		return ind;
	}

	private void AddEdge(int x, int y, Direction dir)
	{
		Vector2 mid = room.MiddleOfTile(x, y);
		int ind1 = -1;
		int ind2 = -1;
		switch (dir)
		{
		case Direction.Up:
			ind1 = AddCorner(new Vector2(mid.x - 10f, mid.y + 10f));
			ind2 = AddCorner(new Vector2(mid.x + 10f, mid.y + 10f));
			break;
		case Direction.Right:
			ind1 = AddCorner(new Vector2(mid.x + 10f, mid.y + 10f));
			ind2 = AddCorner(new Vector2(mid.x + 10f, mid.y - 10f));
			break;
		case Direction.Down:
			ind1 = AddCorner(new Vector2(mid.x + 10f, mid.y - 10f));
			ind2 = AddCorner(new Vector2(mid.x - 10f, mid.y - 10f));
			break;
		case Direction.Left:
			ind1 = AddCorner(new Vector2(mid.x - 10f, mid.y - 10f));
			ind2 = AddCorner(new Vector2(mid.x - 10f, mid.y + 10f));
			break;
		}
		edges.Add(ind1);
		edges.Add(ind2);
	}

	private void AddSlopeEdge(int x, int y, Room.SlopeDirection dir)
	{
		Vector2 mid = room.MiddleOfTile(x, y);
		int ind1 = -1;
		int ind2 = -1;
		switch (dir.ToString())
		{
		case nameof(Room.SlopeDirection.DownLeft):
		case nameof(Room.SlopeDirection.UpRight):
			ind2 = AddCorner(new Vector2(mid.x - 10f, mid.y + 10f));
			ind1 = AddCorner(new Vector2(mid.x + 10f, mid.y - 10f));
			break;
		case nameof(Room.SlopeDirection.DownRight):
		case nameof(Room.SlopeDirection.UpLeft):
			ind1 = AddCorner(new Vector2(mid.x - 10f, mid.y - 10f));
			ind2 = AddCorner(new Vector2(mid.x + 10f, mid.y + 10f));
			break;
		}
		edges.Add(ind1);
		edges.Add(ind2);
	}


	private static IntVector2[] _peekSearchOffsets = new IntVector2[]
	{
			new IntVector2( 0,  0), // Middle
            new IntVector2( 1,  0), // 1 tile
            new IntVector2( 0,  1),
			new IntVector2(-1,  0),
			new IntVector2( 0, -1),
			new IntVector2( 2,  0), // 2 tiles
            new IntVector2( 1,  1),
			new IntVector2( 0,  2),
			new IntVector2(-1,  1),
			new IntVector2(-2,  0),
			new IntVector2(-1, -1),
			new IntVector2( 0, -2)
	};

	public override void Update(bool eu)
	{
		base.Update(eu);

		if (shortcutDisplay == null)
		{
			room.AddObject(shortcutDisplay = new ShortcutDisplay(this));
		}

		lastScreenblockAlpha = screenblockAlpha;

		hideAllSprites = false;
		if (room.game.IsArenaSession)
		{
			if (!room.game.GetArenaGameSession.playersSpawned)
				hideAllSprites = true;
		}

		Player ply = null;
		if (room.game.Players.Count > 0)
			ply = room.game.Players[0].realizedCreature as Player;

		// Map edges to display quads
		if (state != MappingState.Done)
			UpdateMapper(300);

		// Do not try to access shortcuts when the room is not ready for AI
		if (!room.readyForAI)
		{
			screenblockAlpha = 1f;
			return;
		}

		// Find the player's shortcut vessel
		ShortcutHandler.ShortCutVessel plyVessel = null;
		foreach (ShortcutHandler.ShortCutVessel vessel in room.game.shortcuts.transportVessels)
		{
			if (vessel.creature == ply)
			{
				plyVessel = vessel;
				break;
			}
		}

		if (ply == null || ply.room != room || (plyVessel != null && plyVessel.entranceNode != -1))
			screenblockAlpha = Mathf.Clamp01(screenblockAlpha + 0.1f);
		else
			screenblockAlpha = Mathf.Clamp01(screenblockAlpha - 0.1f);

		if (ply != null)
		{
			// Search for the closest shortcut entrance and display a sprite at the end location
			// Disabled in classic mode
			if (showShortcuts)
			{
				IntVector2 scPos = new IntVector2();
				bool found = false;
				if (ply.room != null && !ply.inShortcut)
				{
					for (int chunk = 0; chunk < ply.bodyChunks.Length; chunk++)
					{
						IntVector2 chunkPos = room.GetTilePosition(ply.bodyChunks[chunk].pos);
						for (int i = 0; i < _peekSearchOffsets.Length; i++)
						{
							IntVector2 testPos = chunkPos + _peekSearchOffsets[i];
							if (testPos.x < 0 || testPos.y < 0 || testPos.x >= room.TileWidth || testPos.y >= room.TileHeight) continue;
							if (room.GetTile(testPos).Terrain == Room.Tile.TerrainType.ShortcutEntrance)
							{
								int ind = Array.IndexOf(room.shortcutsIndex, testPos);
								if (ind > -1 && ind < (room.shortcuts?.Length ?? 0))
								{
									if (room.shortcuts[ind].shortCutType == ShortcutData.Type.Normal)
									{
										found = true;
										scPos = testPos;
										break;
									}
								}
							}
						}
					}
				}

				ShortcutData sc = default(ShortcutData);
				int scInd = Array.IndexOf(room.shortcutsIndex, scPos);
				if (scInd > -1 && scInd < (room.shortcuts?.Length ?? 0))
					sc = room.shortcuts[scInd];
				else
					found = false;
				if (found)
				{
					IntVector2 dest = sc.DestTile;
					Vector2 newPeekPos = ply.room.MiddleOfTile(dest);
					if (peekPos != newPeekPos)
					{
						peekAlpha = 0f;
						peekPos = newPeekPos;
						peekAngle = 0f;
						for (int i = 0; i < 4; i++)
						{
							if (!ply.room.GetTile(dest + Custom.fourDirections[i]).Solid)
							{
								peekAngle = 180f - 90f * i;
								break;
							}
						}
					}
				}

				lastPeekAlpha = peekAlpha;
				peekAlpha = Custom.LerpAndTick(peekAlpha, found ? Mathf.Sin(room.game.clock / 40f * Mathf.PI * 4f) * 0.25f + 0.75f : 0f, 0.1f, 0.075f);
			}

			// Allow vision when going through shortcuts
			if (plyVessel != null)
			{
				int updateShortCut = room.game.updateShortCut;
				bool first = !overrideEyePos.HasValue;
				if (!first) lastOverrideEyePos = overrideEyePos.Value;
				overrideEyePos = Vector2.Lerp(plyVessel.lastPos.ToVector2(), plyVessel.pos.ToVector2(), (updateShortCut + 1) / 3f) * 20f + new Vector2(10f, 10f);
				if (first) lastOverrideEyePos = overrideEyePos.Value;
				if (plyVessel.room.realizedRoom != null)
					screenblockAlpha = plyVessel.room.realizedRoom.GetTile(overrideEyePos.Value).Solid ? 1f : 0f;
			}
			else
				overrideEyePos = null;
		}
		else
		{
			peekAlpha = 0f;
		}

		// Don't display in arena while multiple players are present
		// This doesn't happen in story so that Monkland still works
		if (room.game.IsArenaSession && room.game.Players.Count > 1)
			hideAllSprites = true;
	}

	public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
	{
		base.InitiateSprites(sLeaser, rCam);

		peekAlpha = 0f;
		peekPos.Set(-1f, -1f);

		while (state != MappingState.Done)
			UpdateMapper(int.MaxValue);

		sLeaser.sprites = new FSprite[3];

		// Generate tris
		TriangleMesh.Triangle[] tris = new TriangleMesh.Triangle[edges.Count];
		for (int i = 0, len = edges.Count / 2; i < len; i++)
		{
			int o = i * 2;
			tris[o] = new TriangleMesh.Triangle(edges[o], edges[o + 1], edges[o] + corners.Count / 2);
			tris[o + 1] = new TriangleMesh.Triangle(edges[o + 1], edges[o + 1] + corners.Count / 2, edges[o] + corners.Count / 2);
		}

		// Block outside of FoV with level color
		TriangleMesh colorBlocker = new TriangleMesh("Futile_White", tris, false, true);
		colorBlocker.shader = shader;
		sLeaser.sprites[0] = colorBlocker;
		corners.CopyTo(colorBlocker.vertices);
		colorBlocker.Refresh();

		// Full screen overlay
		sLeaser.sprites[1] = new FSprite("pixel")
		{
			anchorX = 0f,
			anchorY = 0f
		};
		sLeaser.sprites[1].shader = shader;

		// Shortcut peek
		tris = new TriangleMesh.Triangle[]
		{
                // Small square
                new TriangleMesh.Triangle(0, 1, 2), new TriangleMesh.Triangle(1, 2, 3),
                // Large trapezoid
                new TriangleMesh.Triangle(2, 3, 4), new TriangleMesh.Triangle(3, 4, 5),
				new TriangleMesh.Triangle(4, 5, 6), new TriangleMesh.Triangle(5, 6, 7)
		};
		TriangleMesh scPeek = new TriangleMesh("Futile_White", tris, true, true);
		scPeek.vertices[0].Set(-10f, -10f);
		scPeek.vertices[1].Set(-10f, 10f);
		scPeek.vertices[2].Set(10f, -10f);
		scPeek.vertices[3].Set(10f, 10f);
		scPeek.vertices[4].Set(30f, -30f);
		scPeek.vertices[5].Set(30f, 30f);
		scPeek.vertices[6].Set(60f, -60f);
		scPeek.vertices[7].Set(60f, 60f);
		sLeaser.sprites[2] = scPeek;

		AddToContainer(sLeaser, rCam, null);
	}

	public override void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
	{
		var col = Color.Lerp(palette.blackColor, palette.fogColor, Lightness);
		col.a = Amount;
		sLeaser.sprites[0].color = col;
		sLeaser.sprites[1].color = col;
		sLeaser.sprites[2].color = col;
	}

	public override void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContainer)
	{
		if (newContainer == null)
			newContainer = rCam.ReturnFContainer("Bloom");
		for (int i = 0; i < sLeaser.sprites.Length; i++)
			newContainer.AddChild(sLeaser.sprites[i]);
	}

	private Vector2 _lastEyePos;
	private Vector2 _eyePos;
	private Vector2 _lastCamPos;
	public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
	{
		if (!hackToDelayDrawingUntilAfterTheLevelMoves)
		{
			_lastCamPos = camPos;
			return;
		}

		_lastEyePos = _eyePos;

		if (sLeaser == null || rCam == null) return;
		if (room == null || room.game == null || sLeaser.sprites == null) return;

		foreach (FSprite sprite in sLeaser.sprites)
			sprite.isVisible = !hideAllSprites;

		if (room.game.Players.Count > 0)
		{
			BodyChunk headChunk = room.game.Players[0].realizedCreature?.bodyChunks[0];
			// Thanks, screams
			if (headChunk != null)
				_eyePos = Vector2.Lerp(headChunk.lastPos, headChunk.pos, timeStacker);
		}

		if (overrideEyePos.HasValue)
			_eyePos = Vector2.Lerp(lastOverrideEyePos, overrideEyePos.Value, timeStacker);

		// Update FOV blocker mesh
		TriangleMesh fovBlocker = (TriangleMesh)sLeaser.sprites[0];

		if (_eyePos != _lastEyePos)
		{
			Vector2 pos;
			pos.x = 0f;
			pos.y = 0f;
			for (int i = 0, len = corners.Count / 2; i < len; i++)
			{
				pos.Set(corners[i].x - _eyePos.x, corners[i].y - _eyePos.y);
				pos.Normalize();
				fovBlocker.vertices[i].Set(pos.x * 5f + corners[i].x, pos.y * 5f + corners[i].y);
				fovBlocker.vertices[i + len].Set(pos.x * 10000f + _eyePos.x, pos.y * 10000f + _eyePos.y);
			}

			// Calculate FoV blocker UVs
			Rect bounds = rCam.levelGraphic.localRect;
			bounds.position += rCam.levelGraphic.GetPosition();
			for (int i = fovBlocker.UVvertices.Length - 1; i >= 0; i--)
			{
				Vector2 wPos = fovBlocker.vertices[i] - _lastCamPos;
				fovBlocker.UVvertices[i].x = InverseLerpUnclamped(bounds.xMin, bounds.xMax, wPos.x);
				fovBlocker.UVvertices[i].y = InverseLerpUnclamped(bounds.yMin, bounds.yMax, wPos.y);
			}
			fovBlocker.Refresh();
		}

		fovBlocker.x = -_lastCamPos.x;
		fovBlocker.y = -_lastCamPos.y;

		if (fovBlocker.element != rCam.levelGraphic.element)
			fovBlocker.element = rCam.levelGraphic.element;

		// Block the screen when inside a wall
		{
			IntVector2 tPos = room.GetTilePosition(_eyePos);
			if (tPos.x < 0) tPos.x = 0;
			if (tPos.x >= room.TileWidth) tPos.x = room.TileWidth - 1;
			if (tPos.y < 0) tPos.y = 0;
			if (tPos.y >= room.TileHeight) tPos.y = room.TileHeight - 1;
			if (tiles[tPos.x, tPos.y].Solid)
			{
				lastScreenblockAlpha = 1f;
				screenblockAlpha = 1f;
			}
		}

		// Move the screenblock
		float alpha = Mathf.Lerp(lastScreenblockAlpha, screenblockAlpha, timeStacker);
		if (alpha == 0f)
		{
			sLeaser.sprites[1].isVisible = false;
		}
		else
		{
			FSprite screenBlock = sLeaser.sprites[1];
			screenBlock.scaleX = rCam.levelGraphic.scaleX;
			screenBlock.scaleY = rCam.levelGraphic.scaleY;
			screenBlock.x = rCam.levelGraphic.x;
			screenBlock.y = rCam.levelGraphic.y;

			if (screenBlock.element != rCam.levelGraphic.element)
				screenBlock.element = rCam.levelGraphic.element;

			screenBlock.alpha = alpha;
		}

		// Update shortcut peek
		float peekAlpha = Mathf.Lerp(lastPeekAlpha, this.peekAlpha, timeStacker);
		if (peekAlpha > 0f)
		{
			TriangleMesh peek = (TriangleMesh)sLeaser.sprites[2];
			//if (peek.element != rCam.levelGraphic.element)
			//    peek.element = rCam.levelGraphic.element;
			if (lastPeekAlpha != this.peekAlpha)
			{
				Color[] cols = peek.verticeColors;
				for (int i = 0; i < cols.Length; i++)
				{
					float vertAlpha = (i < 6) ? peekAlpha : 0f;
					//cols[i] = new Color(1f, vertAlpha * 0.75f, 0f, vertAlpha);
					cols[i] = new Color(1f, 1f, 1f, vertAlpha * 0.25f);
				}
			}

			//Rect bounds = rCam.levelGraphic.localRect;
			//bounds.position += rCam.levelGraphic.GetPosition();
			//for (int i = peek.UVvertices.Length - 1; i >= 0; i--)
			//{
			//    Vector2 wPos = peek.vertices[i];
			//    float rad = _peekAngle * Mathf.Deg2Rad;
			//    wPos.Set(wPos.x * Mathf.Cos(rad) + wPos.y * Mathf.Sin(rad), wPos.y * Mathf.Cos(rad) - wPos.x * Mathf.Sin(rad));
			//    wPos = wPos + _peekPos - camPos;
			//    peek.UVvertices[i].x = InverseLerpUnclamped(bounds.xMin, bounds.xMax, wPos.x);
			//    peek.UVvertices[i].y = InverseLerpUnclamped(bounds.yMin, bounds.yMax, wPos.y);
			//}

			peek.SetPosition(peekPos - _lastCamPos);
			peek.rotation = peekAngle;
		}
		else
			sLeaser.sprites[2].isVisible = false;

		// Keep on top
		FContainer container = sLeaser.sprites[2].container;
		if (container.GetChildAt(container.GetChildCount() - 1) != sLeaser.sprites[2])
		{
			for (int i = 0; i < sLeaser.sprites.Length; i++)
				sLeaser.sprites[i].MoveToFront();
		}

		ApplyPalette(sLeaser, rCam, rCam.currentPalette);

		base.DrawSprites(sLeaser, rCam, timeStacker, _lastCamPos);
	}

	private float InverseLerpUnclamped(float from, float to, float t)
	{
		return (t - from) / (to - from);
	}

	public void UpdateMapper(int iterations)
	{
		Room.Tile[,] tiles = this.tiles;
		for (int i = 0; i < iterations; i++)
		{
			switch (state)
			{
			case MappingState.FindingEdges:
			{
				Room.Tile tile = tiles[x, y];
				Room.Tile.TerrainType terrain = tile.Terrain;
				Room.SlopeDirection slope = (terrain == Room.Tile.TerrainType.Slope) ? room.IdentifySlope(x, y) : Room.SlopeDirection.Broken;

				if (HasEdge(x, y, Direction.Left) && !HasEdge(x - 1, y, Direction.Right)) AddEdge(x, y, Direction.Left);
				if (HasEdge(x, y, Direction.Down) && !HasEdge(x, y - 1, Direction.Up)) AddEdge(x, y, Direction.Down);
				if (HasEdge(x, y, Direction.Right) && !HasEdge(x + 1, y, Direction.Left)) AddEdge(x, y, Direction.Right);
				if (HasEdge(x, y, Direction.Up) && !HasEdge(x, y + 1, Direction.Down)) AddEdge(x, y, Direction.Up);

				if (slope != Room.SlopeDirection.Broken) AddSlopeEdge(x, y, slope);

				x++;
				if (x >= room.TileWidth)
				{
					x = 0;
					y++;
					if (y >= room.TileHeight)
					{
						y = corners.Count;
						state = MappingState.DuplicatingPoints;
					}
				}
			}
			break;
			case MappingState.DuplicatingPoints:
			{
				corners.Add(corners[x]);
				x++;
				if (x >= y)
				{
					state = MappingState.Done;
					x = 0;
					y = 0;
				}
			}
			break;
			case MappingState.Done:
				return;
			}
		}
	}
}

internal class ShortcutDisplay : CosmeticSprite
{
	private FogOfWarController owner;
	private Player Ply => (room.game.Players.Count > 0) ? room.game.Players[0].realizedCreature as Player : null;

	private float alpha;
	private float lastAlpha;

	public ShortcutDisplay(FogOfWarController owner)
	{
		this.owner = owner;
	}

	public override void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
	{
	}

	public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
	{
		Vector2 drawPos = Vector2.Lerp(lastPos, pos, timeStacker);
		float drawAlpha = Mathf.Lerp(lastAlpha, alpha, timeStacker);
		for (int i = 0; i < sLeaser.sprites.Length; i++)
		{
			sLeaser.sprites[i].SetPosition(drawPos - camPos);
			sLeaser.sprites[i].alpha = drawAlpha;
			sLeaser.sprites[i].isVisible = !owner.hideAllSprites;
		}

		base.DrawSprites(sLeaser, rCam, timeStacker, camPos);
	}

	public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
	{
		sLeaser.sprites = new FSprite[2];
		sLeaser.sprites[0] = new FSprite("Futile_White")
		{
			shader = rCam.game.rainWorld.Shaders["FlatLight"],
			scaleX = 6f,
			scaleY = 6f,
			color = new Color(1f, 1f, 1f, 0.2f)
		};
		sLeaser.sprites[1] = new FSprite("ShortcutArrow")
		{
			rotation = 180f,
			anchorY = 1f
		};

		AddToContainer(sLeaser, rCam, rCam.ReturnFContainer("HUD"));
	}

	public override void Update(bool eu)
	{
		base.Update(eu);

		Player ply = Ply;

		lastAlpha = alpha;

		// Find the player's shortcut vessel
		ShortcutHandler.ShortCutVessel plyVessel = null;
		foreach (ShortcutHandler.ShortCutVessel vessel in room.game.shortcuts.transportVessels)
		{
			if (vessel.creature == ply && vessel.room == room.abstractRoom)
			{
				plyVessel = vessel;
				break;
			}
		}

		if (plyVessel != null)
		{
			// Find the player's position in a shortcut
			Vector2 scPos = room.MiddleOfTile(plyVessel.pos);
			Vector2 lastScPos = room.MiddleOfTile(plyVessel.lastPos);
			//int update = (int)_RainWorldGame_updateShortCut.GetValue(room.game);
			//pos = Vector2.Lerp(lastScPos, scPos, (update + 1f) / 3f);
			lastPos = scPos;
			pos = scPos;
			alpha = Mathf.Min(alpha + 0.2f, 1f);
		}
		else
		{
			// Fade out when not in use
			alpha = Mathf.Max(alpha - 0.2f, 0f);
		}

		if (owner.slatedForDeletetion)
			Destroy();
	}
}
