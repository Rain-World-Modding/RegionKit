using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RegionKit.Modules.Objects
{
    public class PlacedWaterFall : WaterFall
    {
        public PlacedWaterFall(PlacedObject owner, Room room) : base (room, (owner.pos / 20).ToIntVector2(), (owner.data as PlacedWaterfallData)?.flow ?? 1f, (owner.data as PlacedWaterfallData)?.width ?? 1)
        {
            po = owner;
            plog.LogDebug($"({room.abstractRoom.name}): created PlacedWaterfall.");
        }
        private PlacedObject po;
        private PlacedWaterfallData pwd => (po?.data as PlacedWaterfallData);

        public override void Update(bool eu)
        {
            base.Update(eu);
            this.pos = po.pos;
            if (pwd != null)
            {
                this.setFlow = pwd.flow;
                this.width = pwd.width;
            }
        }
    }
}
