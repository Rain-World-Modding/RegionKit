using DevInterface;

namespace RegionKit.Modules.Objects
{
    internal class WaterFallDepthRepresentation : PlacedObjectRepresentation
    {
        internal WaterFallDepth.WaterFallDepthData Data => (pObj.data as WaterFallDepth.WaterFallDepthData)!;
        private readonly WaterFallDepthPanel panel;
        private readonly FSprite panelLine, widthLine, leftBar, rightBar;

        public WaterFallDepthRepresentation(DevUI owner, string IDstring, DevUINode parentNode, PlacedObject pObj) : base(owner, IDstring, parentNode, pObj, "Waterfall With Depth")
        {
            subNodes.Add(panel = new WaterFallDepthPanel(owner, "WaterFallDepth_Panel", this, Data.panelPos));
            fSprites.Add(panelLine = new FSprite("pixel") { anchorY = 0f });
            fSprites.Add(widthLine = new FSprite("pixel") { anchorX = 0f });
            fSprites.Add(leftBar = new FSprite("pixel") { scaleY = 10f });
            fSprites.Add(rightBar = new FSprite("pixel") { scaleY = 10f });
            owner.placedObjectsContainer.AddChild(panelLine);
            owner.placedObjectsContainer.AddChild(widthLine);
            owner.placedObjectsContainer.AddChild(leftBar);
            owner.placedObjectsContainer.AddChild(rightBar);
        }

        public override void Refresh()
        {
            base.Refresh();

            Data.panelPos = panel.pos;

            panelLine.SetPosition(absPos + new Vector2(0.01f, 0.01f));
            panelLine.scaleY = panel.pos.magnitude;
            panelLine.rotation = AimFromOneVectorToAnother(pos, panel.absPos);

            var middlePos = owner.room.MiddleOfTile(pObj.pos) - owner.room.game.cameras[0].pos + new Vector2(0.01f, 15.01f);
            widthLine.SetPosition(middlePos + new Vector2(-10f, 0f));
            widthLine.scaleX = Data.width * 20f;
            
            leftBar.SetPosition(middlePos + new Vector2(-10f, 0f));
            rightBar.SetPosition(middlePos + new Vector2(Data.width * 20f - 10f, 0f));
        }

        internal class WaterFallDepthPanel : Panel
        {
            private static readonly string WIDTH_SLIDER_ID = "WaterFallDepth_Width";
            private static readonly string FLOW_SLIDER_ID = "WaterFallDepth_Flow";
            private static readonly string DEPTH_SLIDER_ID = "WaterFallDepth_Depth";
            private static readonly string PRE_SLIDER_ID = "WaterFallDepth_Pre";
            private static readonly string POST_SLIDER_ID = "WaterFallDepth_Post";

			private readonly Cycler modeCycler;

			private WaterFallDepth.WaterFallDepthData Data => (parentNode as WaterFallDepthRepresentation)!.Data;


			public WaterFallDepthPanel(DevUI owner, string IDstring, DevUINode parentNode, Vector2 pos) : base(owner, IDstring, parentNode, pos, new Vector2(250f, 125f), "Waterfall With Depth")
            {
                subNodes.Add(new WaterFallDepthSlider(owner, WIDTH_SLIDER_ID, this, new Vector2(5f, 105f), "Width: "));
                subNodes.Add(new WaterFallDepthSlider(owner, FLOW_SLIDER_ID,  this, new Vector2(5f, 85f),  "Flow: "));
                subNodes.Add(new WaterFallDepthSlider(owner, DEPTH_SLIDER_ID, this, new Vector2(5f, 65f),  "Depth: "));
				subNodes.Add(new WaterFallDepthSlider(owner, PRE_SLIDER_ID,   this, new Vector2(5f, 25f),  "Pre delay: "));
                subNodes.Add(new WaterFallDepthSlider(owner, POST_SLIDER_ID,  this, new Vector2(5f, 5f),   "Post delay: "));
				subNodes.Add(modeCycler = new Cycler(owner, "WaterFallDepth_Mode", this, new Vector2(5f, 45f), 240f, "Mode: ", ["Static", "Dynamic"]));
				modeCycler.currentAlternative = Data.dynamic ? 1 : 0;
            }

			public override void Update()
			{
				base.Update();
				Data.dynamic = modeCycler.alternatives[modeCycler.currentAlternative] == "Dynamic";
			}

			internal class WaterFallDepthSlider(DevUI owner, string IDstring, DevUINode parentNode, Vector2 pos, string title) : Slider(owner, IDstring, parentNode, pos, title, false, 110f)
            {
                private WaterFallDepthRepresentation Representation => (parentNode.parentNode as WaterFallDepthRepresentation)!;

                public override void Refresh()
                {
                    base.Refresh();
                    float num = 0f;
                    if (IDstring == WIDTH_SLIDER_ID)
                    {
                        num = (Representation.Data.width - 1) / 9f;
                        NumberText = Representation.Data.width.ToString();
                    }
                    else if (IDstring == FLOW_SLIDER_ID)
                    {
                        num = Representation.Data.flow;
                        NumberText = num.ToString("0.00");
                    }
                    else if (IDstring == DEPTH_SLIDER_ID)
                    {
                        num = Representation.Data.depth;
                        NumberText = Mathf.FloorToInt(num * 30f).ToString();
                    }
					else if (IDstring == PRE_SLIDER_ID)
					{
						num = Mathf.InverseLerp(minDelay, maxDelay, Representation.Data.preDelay);
						NumberText = Representation.Data.preDelay.ToString("0.0");
					}
					else if (IDstring == POST_SLIDER_ID)
					{
						num = Mathf.InverseLerp(minDelay, maxDelay, Representation.Data.postDelay);
						NumberText = Representation.Data.postDelay.ToString("0.0");
					}
					RefreshNubPos(num);
                }

                public override void NubDragged(float nubPos)
                {
                    if (IDstring == WIDTH_SLIDER_ID)
                    {
                        Representation.Data.width = Mathf.RoundToInt(nubPos * 9f + 1);
                    }
                    else if (IDstring == FLOW_SLIDER_ID)
                    {
                        Representation.Data.flow = nubPos;
                    }
                    else if (IDstring == DEPTH_SLIDER_ID)
                    {
                        Representation.Data.depth = nubPos;
                    }
					else if (IDstring == PRE_SLIDER_ID)
					{
						Representation.Data.preDelay = Mathf.Lerp(minDelay, maxDelay, nubPos);
					}
					else if (IDstring == POST_SLIDER_ID)
					{
						Representation.Data.postDelay = Mathf.Lerp(minDelay, maxDelay, nubPos);
					}
					Representation.Refresh();
                    Refresh();
                }

				private float minDelay = -3f;
				private float maxDelay = 3f;
            }
        }
    }
}
