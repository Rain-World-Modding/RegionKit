using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RegionKit.Modules.ConcealedGarden;

internal class CGGateCustomization : UpdatableAndDeletable, IDrawable
{
	private readonly PlacedObject pObj;
	private RegionGateGraphics.DoorGraphic? leftDoor;
	private RegionGateGraphics.DoorGraphic? rightDoor;
	private bool swappedDrawOrder;

	ManagedData data => (pObj.data as ManagedData)!;

	public CGGateCustomization(Room room, PlacedObject pObj)
	{
		this.room = room;
		this.pObj = pObj;

		if (data.GetValue<bool>("nowater"))
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

		if (data.GetValue<bool>("noleft"))
		{
			room.regionGate.doors[0].closeSpeed = 0f;
			this.leftDoor = room.regionGate.graphics.doorGraphs[0];
		}

		if (data.GetValue<bool>("noright"))
		{
			room.regionGate.doors[2].closeSpeed = 0f;
			this.rightDoor = room.regionGate.graphics.doorGraphs[2];
		}


	}

	internal static void Register()
	{
		RegisterFullyManagedObjectType(new ManagedField[]
		{
				new BooleanField("noleft", false, displayName:"No Left Door"),
				new BooleanField("noright", false, displayName:"No Right Door"),
				new BooleanField("nowater", false, displayName:"No Water"),
				new BooleanField("zdontstop", false, displayName:"Dont cut song"),
		}, typeof(CGGateCustomization), "CGGateCustomization");
	}

	public override void Update(bool eu)
	{
		base.Update(eu);

		if (room.regionGate.washingCounter == 0) room.regionGate.washingCounter = 200;
		if (leftDoor != null)
		{
			leftDoor.lastClosedFac = room.regionGate.doors[0].closedFac;
			room.regionGate.goalDoorPositions[0] = room.regionGate.doors[0].closedFac;
		}
		if (rightDoor != null)
		{
			rightDoor.lastClosedFac = room.regionGate.doors[2].closedFac;
			room.regionGate.goalDoorPositions[2] = room.regionGate.doors[2].closedFac;
		}
		if (data.GetValue<bool>("zdontstop"))
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
		if (!this.swappedDrawOrder)
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
				swappedDrawOrder = true;
			}
		}
		// I'm too lazy to properly handle custom doors that nobody has made yet
		if (leftDoor != null)
		{
			foreach (var item in rCam.spriteLeasers)
			{
				if (item.drawableObject == room.regionGate)
				{
					for (int i = 0; i < leftDoor.TotalSprites; i++)
					{
						item.sprites[i].isVisible = false;
					}
				}
			}
		}
		if (rightDoor != null)
		{
			foreach (var item in rCam.spriteLeasers)
			{
				if (item.drawableObject == room.regionGate)
				{
					for (int i = rightDoor.TotalSprites * 2; i < rightDoor.TotalSprites * 3; i++)
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
