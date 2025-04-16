using System;
using System.Collections.Generic;
using RWCustom;
using UnityEngine;

namespace RegionKit.Modules.AridBarrens;

/// <summary>
/// Passively spawns sand puffs in room at all times
/// </summary>
public partial class SandPuffsScene : BackgroundScene, INotifyWhenRoomIsReady
{
	///<inheritdoc/>
	public SandPuffsScene(RoomSettings.RoomEffect effect, Room room) : base(room)
	{
		this._effect = effect;
	}
	///<inheritdoc/>
	public override void Update(bool eu)
	{
		base.Update(eu);
		if (UnityEngine.Random.value < Custom.LerpMap(this._effect.amount, 0f, 1f, 0f, 0.7f))
		{
			for (int j = 0; j < this.room.physicalObjects.Length; j++)
			{
				for (int k = 0; k < this.room.physicalObjects[j].Count; k++)
				{
					for (int l = 0; l < this.room.physicalObjects[j][k].bodyChunks.Length; l++)
					{
						if ((this.room.physicalObjects[j][k].bodyChunks[l].ContactPoint.x != 0 || this.room.physicalObjects[j][k].bodyChunks[l].ContactPoint.y != 0) && Mathf.Abs(this.room.physicalObjects[j][k].bodyChunks[l].lastPos.y - this.room.physicalObjects[j][k].bodyChunks[l].pos.y) > 5f)
						{
							this.room.AddObject(new SandPuff(this.room.physicalObjects[j][k].bodyChunks[l].pos + new Vector2(0f, -this.room.physicalObjects[j][k].bodyChunks[l].rad), Custom.LerpMap(this.room.physicalObjects[j][k].bodyChunks[l].lastPos.y - this.room.physicalObjects[j][k].bodyChunks[l].pos.y, 5f, 10f, 0.5f, 1f)));
						}
						else if ((this.room.physicalObjects[j][k].bodyChunks[l].ContactPoint.x != 0 || this.room.physicalObjects[j][k].bodyChunks[l].ContactPoint.y != 0) && Mathf.Abs(this.room.physicalObjects[j][k].bodyChunks[l].lastPos.x - this.room.physicalObjects[j][k].bodyChunks[l].pos.x) > 5f)
						{
							this.room.AddObject(new SandPuff(this.room.physicalObjects[j][k].bodyChunks[l].pos + new Vector2(0f, -this.room.physicalObjects[j][k].bodyChunks[l].rad), Custom.LerpMap(this.room.physicalObjects[j][k].bodyChunks[l].lastPos.y - this.room.physicalObjects[j][k].bodyChunks[l].pos.y, 5f, 10f, 0.5f, 1f)));
						}
					}
				}
			}
		}
	}
	///<inheritdoc/>
	public void ShortcutsReady()
	{
	}
	/// <summary>
	/// Populates <see cref="_closeToWallTiles"/>
	/// </summary>
	public void AIMapReady()
	{
		this._closeToWallTiles = new List<IntVector2>();
		for (int i = 0; i < this.room.TileWidth; i++)
		{
			for (int j = 0; j < this.room.TileHeight; j++)
			{
				if (this.room.aimap.getTerrainProximity(i, j) == 1)
				{
					this._closeToWallTiles.Add(new IntVector2(i, j));
				}
			}
		}
	}

	private RoomSettings.RoomEffect _effect;

	private List<IntVector2>? _closeToWallTiles;
	
}
