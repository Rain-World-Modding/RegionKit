using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RWCustom;
using UnityEngine;
using static System.Math;
using static RWCustom.Custom;

namespace RegionKit.Modules.Objects;
/// <summary>
/// Spawns wormgrass from WormgrassRectData
/// </summary>
public class WormgrassManager : UpdatableAndDeletable
{
	
	internal WormgrassManager(Room rm)
	{

	}

	private bool _regAlreadyAttempted = false;
	///<inheritdoc/>
	public override void Update(bool eu)
	{
		base.Update(eu);
		if (room?.game == null || _regAlreadyAttempted) return;
		_regAlreadyAttempted = true;
		foreach (var po in room.roomSettings.placedObjects)
		{
			TryRegisterArea(po);
		}
		room.AddObject(new WormGrass(room, _tarTiles));
	}
	private bool TryRegisterArea(PlacedObject po)
	{
		if (po.data is WormgrassRectData wgrect)
		{
			var st = (po.pos / 20).ToIntVector2();
			var ht = st + wgrect.p2;
			for (int i = Min(st.x, ht.x); i < Max(st.x, ht.x); i++)
			{
				for (int j = Min(st.y, ht.y); j < Max(st.y, ht.y); j++)
				{
					TryRegisterTile(new IntVector2(i, j));
				}
			}
			return true;
		}
		return false;
	}
	private void TryRegisterTile(IntVector2 tile)
	{
		_tarTiles.Add(tile);
	}
	private readonly List<IntVector2> _tarTiles = new List<IntVector2>();
}
