using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RegionKit.Modules.Objects;

/// <summary>
/// a slightly better waterfall
/// </summary>
public class PlacedWaterFall : WaterFall
{
	///<inheritdoc/>
	public PlacedWaterFall(PlacedObject owner, Room room) : base(room, (owner.pos / 20).ToIntVector2(), (owner.data as PlacedWaterfallData)?.flow ?? 1f, (owner.data as PlacedWaterfallData)?.width ?? 1)
	{
		_po = owner;
		__logger.LogDebug($"({room.abstractRoom.name}): created PlacedWaterfall.");
	}
	private PlacedObject _po;
	private PlacedWaterfallData _Data => (_po?.data as PlacedWaterfallData)!;
	///<inheritdoc/>
	public override void Update(bool eu)
	{
		base.Update(eu);
		this.pos = _po.pos;
		if (_Data != null)
		{
			this.setFlow = _Data.flow;
			this.width = _Data.width;
		}
	}
}
