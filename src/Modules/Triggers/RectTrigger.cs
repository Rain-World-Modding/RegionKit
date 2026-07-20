using System.Globalization;
using System.Text.RegularExpressions;
using DevInterface;

namespace RegionKit.Modules.Triggers
{
	public class RectTrigger : EventTrigger, ICustomTrigger
	{
		private Vector2 rectHandle1;
		private Vector2 rectHandle2;

		private Rect TriggerRect
		{
			get
			{
				Vector2 min = Vector2.Min(rectHandle1, rectHandle2);
				Vector2 max = Vector2.Max(rectHandle1, rectHandle2);
				return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
			}
		}

		public RectTrigger() : base(_Enums.RectTrigger)
		{
		}

		public override string ToString()
		{
			string text = base.BaseSaveString() + string.Format(CultureInfo.InvariantCulture, "<tA>handle1<tB>{0}<tB>{1}<tA>handle2<tB>{2}<tB>{3}",
				rectHandle1.x, rectHandle1.y, 
				rectHandle2.x, rectHandle2.y
				);
			foreach (KeyValuePair<string, string> keyValuePair in unrecognizedSaveStrings)
			{
				text = string.Concat([text, "<tA>", keyValuePair.Key, "<tB>", keyValuePair.Value]);
			}
			return text;
		}

		public override void FromString(string[] s)
		{
			base.FromString(s);
			for (int i = 0; i < s.Length; i++)
			{
				string[] array = Regex.Split(s[i], "<tB>");
				string text = array[0];
				switch(text)
				{
					case "handle1":
						rectHandle1 = new Vector2(float.Parse(array[1], CultureInfo.InvariantCulture), float.Parse(array[2], CultureInfo.InvariantCulture));
						break;
					case "handle2":
						rectHandle2 = new Vector2(float.Parse(array[1], CultureInfo.InvariantCulture), float.Parse(array[2], CultureInfo.InvariantCulture));
						break;
				}
			}

			unrecognizedSaveStrings.Remove("handle1");
			unrecognizedSaveStrings.Remove("handle2");
		}

		public bool PerformWait => true;

		public bool CheckCondition(Player player, Room room)
		{
			return TriggerRect.Contains(player.mainBodyChunk.pos);
		}

		public void InitAtPosition(Vector2 pos)
		{
			rectHandle1 = pos + new Vector2(-150f, -150f);
			rectHandle2 = rectHandle1 + new Vector2(100f, 100f);
		}

		public void InitDevUI(TriggerPanel triggerPanel)
		{
			RectTriggerHandle left, right;
			triggerPanel.subNodes.Add(left = new RectTriggerHandle(triggerPanel.owner, "RectTrigger_Handle1", triggerPanel, this, true));
			triggerPanel.subNodes.Add(right = new RectTriggerHandle(triggerPanel.owner, "RectTrigger_Handle2", triggerPanel, this, false));
			left.other = right;
			right.other = left;
		}

		private class RectTriggerHandle : Handle
		{
			private readonly RectTrigger trigger;
			private readonly bool handle1;
			private readonly FSprite connectorH;
			private readonly FSprite connectorV;
			private readonly FSprite? panelConnector;
			public RectTriggerHandle? other;

			public RectTriggerHandle(DevUI owner, string IDstring, DevUINode parentNode, RectTrigger rectTrigger, bool handle1) 
				: base(owner, IDstring, parentNode, (handle1 ? rectTrigger.rectHandle1 : rectTrigger.rectHandle2) - owner.game.cameras[0].pos - rectTrigger.panelPosition)
			{
				trigger = rectTrigger;
				this.handle1 = handle1;
				if (handle1)
				{
					panelConnector = new FSprite("pixel")
					{
						anchorY = 0f
					};
					fSprites.Add(panelConnector);
					owner.placedObjectsContainer.AddChild(panelConnector);
				}

				connectorH = new FSprite("pixel")
				{
					anchorX = handle1 ? 0f : 1f
				};
				connectorV = new FSprite("pixel")
				{
					anchorY = handle1 ? 0f : 1f
				};
				fSprites.Add(connectorH);
				fSprites.Add(connectorV);
				owner.placedObjectsContainer.AddChild(connectorH);
				owner.placedObjectsContainer.AddChild(connectorV);
			}

			public override void Update()
			{
				base.Update();
				Vector2 camPos = owner.game.cameras[0].pos;
				if (dragged)
				{
					if (handle1)
					{
						trigger.rectHandle1 = camPos + absPos;
					}
					else
					{
						trigger.rectHandle2 = camPos + absPos;
					}
				}
				else
				{
					AbsMove((handle1 ? trigger.rectHandle1 : trigger.rectHandle2) - camPos);
				}
			}

			public override void Refresh()
			{
				base.Refresh();
				UpdateSprites();
				other?.UpdateSprites();
			}

			private void UpdateSprites()
			{
				if (panelConnector != null)
				{
					panelConnector.SetPosition(absPos + new Vector2(0.01f, 0.01f));
					panelConnector.rotation = Custom.AimFromOneVectorToAnother(absPos, trigger.panelPosition);
					panelConnector.scaleY = Vector2.Distance(absPos, trigger.panelPosition);
				}
				Vector2 camPos = owner.game.cameras[0].pos;
				Rect rect = trigger.TriggerRect;
				connectorH.SetPosition((handle1 ? rect.min : rect.max) - camPos);
				connectorV.SetPosition((handle1 ? rect.min : rect.max) - camPos);
				connectorH.scaleX = rect.width;
				connectorV.scaleY = rect.height;
			}
		}
	}
}
