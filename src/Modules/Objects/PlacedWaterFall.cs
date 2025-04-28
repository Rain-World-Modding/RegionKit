using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RegionKit.Modules.Objects;

/// <summary>
/// a slightly better waterfall
/// </summary>
[Obsolete("Use in-game FluxWaterfall set to static.")]
public class PlacedWaterFall : WaterFall
{
	///<inheritdoc/>
	public PlacedWaterFall(PlacedObject owner, Room room) : base(room, (owner.pos / 20).ToIntVector2(), (owner.data as PlacedWaterfallData)?.flow ?? 1f, (owner.data as PlacedWaterfallData)?.width ?? 1)
	{
		_po = owner;
		LogDebug($"({room.abstractRoom.name}): created PlacedWaterfall.");
		Array.Resize(ref room.waterFalls, room.waterFalls.Length + 1);
		room.waterFalls[^1] = this;
		if (room.waterObject != null)
		{
			ConnectToWaterObject(room.waterObject);
		}
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
