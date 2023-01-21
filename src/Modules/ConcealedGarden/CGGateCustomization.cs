using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RegionKit.Modules.ConcealedGarden;

internal class CGGateCustomization : UpdatableAndDeletable, IDrawable
{
	private readonly PlacedObject _pObj;
	private RegionGateGraphics.DoorGraphic? _leftDoor;
	private RegionGateGraphics.DoorGraphic? _rightDoor;
	private bool _swappedDrawOrder;

	private ManagedData _Data => (_pObj.data as ManagedData)!;

	public CGGateCustomization(Room room, PlacedObject pObj)
	{
		this.room = room;
		this._pObj = pObj;

		if (_Data.GetValue<bool>("nowater"))
		{
			IDrawable? water = null;
			foreach (var item in room.drawableObjects)
			{
				if (item is Water)
				{
					water = item;
					break;
				}
			}
			if (water != null) room.drawableObjects.Remove(water);
		}

		if (_Data.GetValue<bool>("noleft"))
		{
			room.regionGate.doors[0].closeSpeed = 0f;
			this._leftDoor = room.regionGate.graphics.doorGraphs[0];
		}

		if (_Data.GetValue<bool>("noright"))
		{
			room.regionGate.doors[2].closeSpeed = 0f;
			this._rightDoor = room.regionGate.graphics.doorGraphs[2];
		}


	}

	public override void Update(bool eu)
	{
		base.Update(eu);

		if (room.regionGate.washingCounter == 0) room.regionGate.washingCounter = 200;
		if (_leftDoor != null)
		{
			_leftDoor.lastClosedFac = room.regionGate.doors[0].closedFac;
			room.regionGate.goalDoorPositions[0] = room.regionGate.doors[0].closedFac;
		}
		if (_rightDoor != null)
		{
			_rightDoor.lastClosedFac = room.regionGate.doors[2].closedFac;
			room.regionGate.goalDoorPositions[2] = room.regionGate.doors[2].closedFac;
		}
		if (_Data.GetValue<bool>("zdontstop"))
		{
			if (room.regionGate.startCounter == 60)
			{
				if (this.room.game.manager.musicPlayer != null && this.room.game.manager.musicPlayer.song is Music.GhostSong ghostSong)
				{
					ghostSong.stopAtGate = false;
				}
			}
		}
	}

	// We need to do stuff in the draw loop >:3c
	public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
	{
		if (!this._swappedDrawOrder)
		{
			RoomCamera.SpriteLeaser? found = null;
			foreach (var item in rCam.spriteLeasers)
			{
				if (item.drawableObject == room.regionGate)
				{
					found = item;
				}
			}
			if (found != null)
			{
				rCam.spriteLeasers.Remove(found);
				rCam.spriteLeasers.Add(found);
				_swappedDrawOrder = true;
			}
		}
		// I'm too lazy to properly handle custom doors that nobody has made yet
		if (_leftDoor != null)
		{
			foreach (var item in rCam.spriteLeasers)
			{
				if (item.drawableObject == room.regionGate)
				{
					for (int i = 0; i < _leftDoor.TotalSprites; i++)
					{
						item.sprites[i].isVisible = false;
					}
				}
			}
		}
		if (_rightDoor != null)
		{
			foreach (var item in rCam.spriteLeasers)
			{
				if (item.drawableObject == room.regionGate)
				{
					for (int i = _rightDoor.TotalSprites * 2; i < _rightDoor.TotalSprites * 3; i++)
					{
						item.sprites[i].isVisible = false;
					}
				}
			}
		}
	}

	public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam) { sLeaser.sprites = new FSprite[0]; }
	public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette) { }
	public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner) { }
}
