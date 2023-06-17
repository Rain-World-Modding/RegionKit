using System;
using System.Collections.Generic;
using UnityEngine;

namespace RegionKit.Modules.ConcealedGarden;


public class CGLifeSimProjection : UpdatableAndDeletable, INotifyWhenRoomIsReady, IDrawable
{
	internal List<PlacedObject> places;

	LoadingState loadingState;
	private int ticksPerUpdate;
	private int currentX;
	private int currentY;
	private int gridWidth;
	private int gridHeight;
	private Tile[,] grid;
	private int tilecount;
	private int rootX;
	private int rootY;
	private List<Rect> rectangles;
	private int generation;
	private List<Tile> activeTiles;

	enum LoadingState
	{
		Start,
		Create,
		Map,
		Done
	}


	public CGLifeSimProjection(Room owner)
	{
		this.grid = new Tile[0, 0];
		this.rectangles = new();
		this.room = owner;
		this.places = new List<PlacedObject>();

		foreach (var pobj in room.roomSettings.placedObjects)
		{
			if (pobj.active && pobj.type.ToString() == "CGLifeSimProjectionSegment") places.Add(pobj);
		}

		loadingState = LoadingState.Start;
		this.ticksPerUpdate = 50;

		activeTiles = new List<Tile>();
	}

	public void AIMapReady()
	{
		// room done loading, speed up
		if (loadingState != LoadingState.Done)
		{
			Debug.Log("LifeSimProjection not yet loaded by AIMapReady, increasing loading rate");
			ticksPerUpdate = 100;
		}
	}

	public void ShortcutsReady()
	{
		// ignored, room started loading
	}

	public override void Update(bool eu)
	{
		base.Update(eu);
		if (loadingState != LoadingState.Done)
		{
			LoadSomeTiles();
			return;
		}

		TickSomeTiles();

		for (int j = 0; j < this.room.physicalObjects.Length; j++)
		{
			if (j != 1 && UnityEngine.Random.value < 0.5f) continue;
			for (int k = 0; k < this.room.physicalObjects[j].Count; k++)
			{
				if (j == 0 && this.room.physicalObjects[j][k] is CoralBrain.StemSegment && UnityEngine.Random.value < 0.97f) continue;
				for (int num = 0; num < this.room.physicalObjects[j][k].bodyChunks.Length; num++)
				{
					Tile? candidate = GetTileAtPos(this.room.physicalObjects[j][k].bodyChunks[num].pos);
					if (candidate != null)
					{
						candidate.hovered++;
						// Debug.Log("LifeSimProjection: HOVERED");
					}
				}
			}
		}

		for (int i = activeTiles.Count - 1; i >= 0; i--)
		{
			Tile tile = activeTiles[i];
			if (!tile.alive && !tile.needGraphicalChange)
				activeTiles.RemoveAt(i);
			else
				tile.Update();
		}
	}

	private Tile? GetTileAtPos(Vector2 pos)
	{
		int tilex = Mathf.FloorToInt((pos.x / 20f - rootX) / 2f);
		int tiley = Mathf.FloorToInt((pos.y / 20f - rootY) / 2f); //(Mathf.FloorToInt(pos.y / 20f) - rootY - 1) / 2;
		if (tilex < 0 || tiley < 0 || tilex >= gridWidth || tiley >= gridHeight) return null;
		return grid[tilex, tiley];
	}

	private void TickSomeTiles()
	{
		int toUpdate = ticksPerUpdate;

		while (currentX < gridWidth && toUpdate > 0)
		{
			while (currentY < gridHeight && toUpdate > 0)
			{
				if (grid[currentX, currentY] != null)
				{
					grid[currentX, currentY].Tick(this, generation);
				}
				toUpdate--;
				currentY++;
			}
			if (currentY == gridHeight)
			{
				currentY = 0;
				currentX++;
			}
		}
		if (currentX == gridWidth)
		{
			//Debug.Log("LifeSimProjection: Generation " + generation + " done");
			this.generation++;
			currentX = 0;
			currentY = 0;
		}
	}

	private void LoadSomeTiles()
	{
		int toLoad = ticksPerUpdate;
		switch (loadingState)
		{
		case LoadingState.Start:
			int minX = int.MaxValue, minY = int.MaxValue, maxX = int.MinValue, maxY = int.MinValue;
			this.rectangles = new List<Rect>();
			foreach (PlacedObject po in places)
			{
				RWCustom.IntRect rect = ((PlacedObject.GridRectObjectData)po.data).Rect;
				minX = Mathf.Min(minX, rect.left);
				minY = Mathf.Min(minY, rect.bottom);
				maxX = Mathf.Max(maxX, rect.right + 1);
				maxY = Mathf.Max(maxY, rect.top + 1);
				this.rectangles.Add(new Rect(rect.left * 20f, rect.bottom * 20f, (rect.Width + 1) * 20f, (rect.Height + 1) * 20f));
				Debug.Log("LifeSimProjection: rect.left is " + rect.left);
				Debug.Log("LifeSimProjection: rect.bottom is " + rect.bottom);
				Debug.Log("LifeSimProjection: rect.Width is " + rect.Width);
				Debug.Log("LifeSimProjection: rect.Height is " + rect.Height);

			}

			this.rootX = minX;
			this.rootY = minY;
			Debug.Log("LifeSimProjection: rootX is " + rootX);
			Debug.Log("LifeSimProjection: rootY is " + rootY);

			gridWidth = (maxX - minX) / 2;
			gridHeight = (maxY - minY) / 2;
			Debug.Log("LifeSimProjection: gridWidth is " + gridWidth);
			Debug.Log("LifeSimProjection: gridHeight is " + gridHeight);


			grid = new Tile[gridWidth, gridHeight];

			currentX = 0;
			currentY = 0;
			loadingState++;
			Debug.Log("LifeSimProjection: Load setup done");
			break;
		case LoadingState.Create:
			while (currentX < gridWidth && toLoad > 0)
			{
				while (currentY < gridHeight && toLoad > 0)
				{
					int tilex = rootX + 1 + currentX * 2; // center of 2x2
					int tiley = rootY + 1 + currentY * 2;
					foreach (var rect in rectangles)
					{
						if (rect.Contains(new Vector2(tilex * 20, tiley * 20)))
						{
							grid[currentX, currentY] = new Tile(this, tilecount, currentX, currentY);
							// Debug.Log("LifeSimProjection: Tile added in " + currentX + " - " + currentY);
							tilecount++;
							break;
						}
					}
					toLoad--;
					currentY++;
				}
				if (currentY == gridHeight)
				{
					currentY = 0;
					currentX++;
				}
			}
			if (currentX == gridWidth)
			{
				Debug.Log("LifeSimProjection: Load create done");
				loadingState++;
				currentX = 0;
				currentY = 0;
			}
			break;
		case LoadingState.Map:
			while (currentX < gridWidth && toLoad > 0)
			{
				while (currentY < gridHeight && toLoad > 0)
				{
					if (grid[currentX, currentY] != null)
					{
						grid[currentX, currentY].MapNeighbours(this);
					}
					toLoad--;
					currentY++;
				}
				if (currentY == gridHeight)
				{
					currentY = 0;
					currentX++;
				}
			}
			if (currentX == gridWidth)
			{
				Debug.Log("LifeSimProjection: Load map done");
				loadingState++;
				currentX = 0;
				currentY = 0;
				ticksPerUpdate = Mathf.Max(1, Mathf.CeilToInt((gridHeight * gridWidth) / 60f));
			}
			break;
		case LoadingState.Done:
			break;
		}
	}

	public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
	{
		if (loadingState != LoadingState.Done)
		{
			Debug.Log("LifeSimProjection not yet loaded by InitiateSprites, force-loading");
			ticksPerUpdate = int.MaxValue;
			LoadSomeTiles();
		}
		Debug.Log("LifeSimProjection: Initializing " + tilecount + " sprites");
		FSprite[] sprites = new FSprite[tilecount];
		for (int i = 0; i < tilecount; i++)
		{
			FSprite sprite = new FSprite("pixel");
			sprite.scale = 34;
			sprite.isVisible = false;
			sprite.shader = rCam.game.rainWorld.Shaders["Projection"];

			sprites[i] = sprite;
		}
		sLeaser.sprites = sprites;
		AddToContainer(sLeaser, rCam, null);
	}

	public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
	{
		for (int i = activeTiles.Count - 1; i >= 0; i--)
		{
			Tile tile = activeTiles[i];
			FSprite sprite = sLeaser.sprites[tile.index];
			if (tile.alive)
			{
				if (tile.needGraphicalChange)
				{
					sprite.isVisible = true;
					//sprite.alpha = 1;
					tile.needGraphicalChange = false;
				}
				sprite.x = tile.pixelX - camPos.x - (tile.pixelX - camPos.x - 700) * 0.022f;
				sprite.y = tile.pixelY - camPos.y - (tile.pixelY - camPos.y - 400) * 0.018f;
				//sprite.x = tile.pixelX - camPos.x;
				//sprite.y = tile.pixelY - camPos.y;

				sprite.alpha = 0.3f + 0.5f * (1f - tile.noTickCount * 0.012f) * Mathf.InverseLerp(0, 5, tile.noChangeCount);
			}
			else if (!tile.alive)
			{
				if (tile.needGraphicalChange)
				{
					sprite.x = tile.pixelX - camPos.x - (tile.pixelX - camPos.x - 700) * 0.022f;
					sprite.y = tile.pixelY - camPos.y - (tile.pixelY - camPos.y - 400) * 0.018f;
					float newAlpha = (0.2f - tile.noTickCount * 0.012f) * Mathf.InverseLerp(12, 0, tile.noChangeCount);
					if (newAlpha > 0.05f)
					{
						sprite.alpha = 0.3f + 0.5f * newAlpha;
					}
					else
					{
						sprite.isVisible = false;
						tile.needGraphicalChange = false;
					}
				}
			}
		}
	}

	public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
	{
		// ???
	}

	public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer? newContatiner)
	{
		if (newContatiner == null)
		{
			//newContatiner = rCam.ReturnFContainer("Background");
			newContatiner = rCam.ReturnFContainer("Background");
		}
		for (int i = 0; i < sLeaser.sprites.Length; i++)
		{
			newContatiner.AddChild(sLeaser.sprites[i]);
		}
	}

	private class Tile
	{
		private List<Tile> neighbours = new();
		internal int hovered;
		internal int gridX;
		internal int gridY;
		internal int index;
		internal int generation;
		internal bool lastAlive;
		internal int noTickCount;
		internal bool alive;
		internal bool needGraphicalChange;
		internal int noChangeCount;
		internal float pixelX;
		internal float pixelY;

		// lets make GC easier
		//private LifeSimProjection owner;

		public Tile(CGLifeSimProjection owner, int index, int currentX, int currentY)
		{
			this.gridX = currentX;
			this.gridY = currentY;
			this.index = index;
			this.pixelX = owner.rootX * 20f + gridX * 40f + 20f;
			this.pixelY = owner.rootY * 20f + gridY * 40f + 20f;
		}

		internal void MapNeighbours(CGLifeSimProjection lifeSimProjection)
		{
			int tmpx, tmpy;
			int maxx = lifeSimProjection.gridWidth - 1;
			int maxy = lifeSimProjection.gridHeight - 1;

			this.neighbours = new List<Tile>();

			for (int i = -1; i <= 1; i++)
			{
				tmpx = i + gridX;
				if (tmpx < 0 || tmpx > maxx) continue;
				for (int j = -1; j <= 1; j++)
				{
					if (i == 0 && j == 0) continue;
					tmpy = j + gridY;
					if (tmpy < 0 || tmpy > maxy) continue;

					Tile candidate = lifeSimProjection.grid[tmpx, tmpy];
					if (candidate != null) this.neighbours.Add(candidate);
				}
			}
		}

		internal void Tick(CGLifeSimProjection lifeSimProjection, int generation)
		{
			// Debug.Log("LifeSimProjection: tile " + index + " ticked");
			this.generation = generation;
			this.lastAlive = this.alive;
			noTickCount = 0;

			int lifecount = 0;
			foreach (var neighbor in neighbours)
			{
				if (neighbor.generation == generation ? neighbor.lastAlive : neighbor.alive) lifecount++;
			}

			// External excitement
			if (lifecount < 3 && hovered > 0)
			{
				lifecount = Mathf.Clamp(lifecount + hovered / (8 + 8 * lifecount), 0, 3);
				if (alive && lifecount > 1) hovered = Mathf.Max(0, hovered - 16);
			}

			if (!alive && lifecount == 3)
			{
				Birth(lifeSimProjection);
			}
			else if (alive && !(lifecount == 2 || lifecount == 3))
			{
				Death(lifeSimProjection);
			}
		}

		private void Death(CGLifeSimProjection lifeSimProjection)
		{
			//Debug.Log("LifeSimProjection: Death");
			alive = false;
			needGraphicalChange = true;
			noChangeCount = 0;
			// removed from active once sprite is hid
		}

		private void Birth(CGLifeSimProjection lifeSimProjection)
		{
			//Debug.Log("LifeSimProjection: Birth!");
			alive = true;
			hovered = 0;
			needGraphicalChange = true;
			noChangeCount = 0;
			if (lifeSimProjection.activeTiles.Contains(this)) return;
			lifeSimProjection.activeTiles.Add(this);
		}

		internal void Update()
		{
			noChangeCount++;
			noTickCount++;
		}
	}
}
