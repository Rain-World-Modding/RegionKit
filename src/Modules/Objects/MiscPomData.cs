using System;
using System.Collections.Generic;
using UnityEngine;
using RWCustom;

namespace RegionKit.Modules.Objects
{

    internal class PlacedWaterfallData : ManagedData
    {
        [FloatField("flow", 0f, 20f, 4f)]
        internal float flow;
        [IntegerField("width", 1, 10, 1)]
        internal int width;

        public PlacedWaterfallData(PlacedObject po) : base (po, new ManagedField[] { })
        {

        }
    }

    internal class PlacedHaloData : ManagedData
    {
        [Vector2Field("rad", 15f, 15f, Vector2Field.VectorReprType.circle, label:"radius")]
        internal Vector2 rad;
        [Vector2Field("headpos", 0f, 30f, Vector2Field.VectorReprType.line, label:"head pos")]
        internal Vector2 headpos;
        [Vector2Field("headdir", 20f, 0f, Vector2Field.VectorReprType.line, label:"head dir")]
        internal Vector2 headdir;

        public PlacedHaloData(PlacedObject owner) : base(owner, new ManagedField[] { })
        {

        }
    }

    internal class WormgrassRectData : ManagedData
    {
        internal IntVector2 p2 => GetValue<IntVector2>("p2");

        public WormgrassRectData(PlacedObject po) : base(po, new ManagedField[] 
        {
            new IntVector2Field("p2", new IntVector2(3, 3), IntVector2Field.IntVectorReprType.rect)
        })
        {

        }
    }

    internal class BorderTpData : ManagedData
    {
        [IntegerField("buffer", 1, 30, 0, ManagedFieldWithPanel.ControlType.arrows, "buffer tiles")]
        public int buff;
        [FloatField("tpfrac", 1f, 1.9f, 1.3f, 0.05f, displayName:"tp buffer frac")]
        public float tpFrac;
        [BooleanField("vOn", true, displayName: "vertical warp")]
        public bool vOn;
        [BooleanField("hOn", true, displayName:"horizontal warp")]
        public bool hOn;

        public BorderTpData(PlacedObject owner) : base(owner, null)
        {
        }
    }
}
