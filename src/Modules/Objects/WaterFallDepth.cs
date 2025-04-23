using System.Globalization;
using System.Text.RegularExpressions;

namespace RegionKit.Modules.Objects
{
    public class WaterFallDepth : WaterFall
    {
        private PlacedObject po;
        private float lastDepth;
        private IntVector2 lastTilePos;
        internal float HeightOffset => (lastDepth * 30f - 6f) * 0.5f;
        internal WaterFallDepthData Data => (po.data as WaterFallDepthData)!;

		private StaticSoundLoop topLoop = null!;
		private StaticSoundLoop bottomLoop = null!;

		public WaterFallDepth(Room room, PlacedObject owner) : base(room, room.GetTilePosition(owner.pos), (owner.data as WaterFallDepthData)!.flow, (owner.data as WaterFallDepthData)!.width)
        {
            this.room = room;
            po = owner;
            lastTilePos = tilePos;
            lastDepth = -1f; // to force it to update

			// Add to room
			Array.Resize(ref room.waterFalls, room.waterFalls.Length + 1);
			room.waterFalls[^1] = this;
			if (room.waterObject != null)
			{
				ConnectToWaterObject(room.waterObject);
			}
		}
		private float GetFlow()
		{
			if (!Data.dynamic)
			{
				return Data.flow;
			}
			WaterLevelCycle waterCycle = room.world.rainCycle.waterCycle;
			WaterLevelCycle.Stage stage = waterCycle.stage;
			float? num = null;
			float? num2 = null;
			if (stage == WaterLevelCycle.Stage.Fall)
			{
				return 0f;
			}
			if (stage == WaterLevelCycle.Stage.Min)
			{
				num = new float?(waterCycle.TimeLeftInStage);
			}
			else if (stage == WaterLevelCycle.Stage.Rise)
			{
				num = new float?(-waterCycle.timeInStage);
			}
			if (stage == WaterLevelCycle.Stage.Rise)
			{
				num2 = new float?(waterCycle.TimeLeftInStage);
			}
			else if (stage == WaterLevelCycle.Stage.Max)
			{
				num2 = new float?(-waterCycle.timeInStage);
			}
			float num3 = 1f;
			if (num != null)
			{
				num3 *= Mathf.Clamp01((num.Value + Data.preDelay) / -1f);
			}
			if (num2 != null)
			{
				num3 *= Mathf.Pow(Mathf.Clamp01((num2.Value + Data.postDelay) / 2f), 3f);
			}
			num3 = Mathf.Min(num3 + room.game.globalRain.floodSpeed, 1f);
			return Mathf.SmoothStep(0f, Data.flow, num3);
		}

		public override void Update(bool eu)
        {
            if (Data != null)
            {
                originalFlow = Data.flow;
                // width = Data.width; // actually this causes crashes :face_holding_back_tears:
                tilePos = room.GetTilePosition(po.pos);

                bool updateDepth = false;
                if (tilePos != lastTilePos)
                {
                    lastTilePos = tilePos;
                    updateDepth = true;
                    for (int i = 0; i < bubbles.GetLength(0); i++)
                    {
                        for (int j = 0; j < bubbles.GetLength(1); j++)
                        {
                            ResetBubble(i, j);
                        }
                    }
                }
                if (Data.depth != lastDepth || updateDepth)
                {
                    lastDepth = Data.depth;
                    pos = room.MiddleOfTile(tilePos) + new Vector2(-10f, 15f);
                    bottomPos[0] = strikeLevel;
                    bottomPos[1] = strikeLevel;
                    bottomPos[2] = 0f;
                }
			}
			setFlow = GetFlow();
			if (topLoop == null)
			{
				topLoop = new StaticSoundLoop(SoundID.Flux_Waterfall_Top_LOOP, pos, room, 0f, 0f)
				{
					randomStartPosition = true,
					pitch = UnityEngine.Random.Range(0.9f, 1.1f)
				};
				bottomLoop = new StaticSoundLoop(SoundID.Flux_Waterfall_Bottom_LOOP, pos, room, 0f, 0f)
				{
					randomStartPosition = true,
					pitch = UnityEngine.Random.Range(0.9f, 1.1f)
				};
			}
			topLoop.Update();
			topLoop.pos = pos + width * 10f * Vector2.right;
			topLoop.volume = setFlow;
			bottomLoop.Update();
			bottomLoop.pos.x = topLoop.pos.x;
			bottomLoop.pos.y = strikeLevel;
			bottomLoop.volume = setFlow;
            base.Update(eu);
		}

        // See drawing code in WaterFallDepthHooks because I made it this far before realizing the original methods weren't virtual

        public class WaterFallDepthData(PlacedObject owner) : PlacedObject.Data(owner)
        {
            public Vector2 panelPos = new(0f, 100f);
            public int width = 1; // 1-10
            public float flow = 0.5f;
            public float depth = 6f / 30f;
			public bool dynamic = false;
			public float preDelay = 0f;
			public float postDelay = 0f;

            protected string BaseSaveString()
            {
                return string.Format(CultureInfo.InvariantCulture, "{0}~{1}~{2}~{3}~{4}~{5}~{6}~{7}",
                [
                    panelPos.x,
                    panelPos.y,
                    width,
                    flow,
					depth,
					dynamic ? "D" : "S",
					preDelay,
					postDelay
				]);
            }

            public override string ToString()
            {
                string text = BaseSaveString();
                text = SaveState.SetCustomData(this, text);
                return SaveUtils.AppendUnrecognizedStringAttrs(text, "~", unrecognizedAttributes);
            }

            public override void FromString(string s)
            {
                string[] array = Regex.Split(s, "~");
                panelPos.x = ((array.Length > 0) ? float.Parse(array[0], NumberStyles.Any, CultureInfo.InvariantCulture) : panelPos.x);
                panelPos.y = ((array.Length > 1) ? float.Parse(array[1], NumberStyles.Any, CultureInfo.InvariantCulture) : panelPos.y);
                width = ((array.Length > 2) ? int.Parse(array[2], NumberStyles.Any, CultureInfo.InvariantCulture) : width);
                flow = ((array.Length > 3) ? float.Parse(array[3], NumberStyles.Any, CultureInfo.InvariantCulture) : flow);
                depth = ((array.Length > 4) ? float.Parse(array[4], NumberStyles.Any, CultureInfo.InvariantCulture) : depth);
				dynamic = ((array.Length > 5) ? (array[5] == "D") : dynamic);
				preDelay = ((array.Length > 6) ? float.Parse(array[6], NumberStyles.Any, CultureInfo.InvariantCulture) : preDelay);
				postDelay = ((array.Length > 7) ? float.Parse(array[7], NumberStyles.Any, CultureInfo.InvariantCulture) : postDelay);
				unrecognizedAttributes = SaveUtils.PopulateUnrecognizedStringAttrs(array, 5);
            }
        }
    }
}
