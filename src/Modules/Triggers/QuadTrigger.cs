using System.Globalization;
using System.Text.RegularExpressions;
using DevInterface;

namespace RegionKit.Modules.Triggers
{
	public class QuadTrigger : EventTrigger, ICustomTrigger
	{
		private Vector2 handle1;
		private Vector2 handle2;
		private Vector2 handle3;
		private Vector2 handle4;

		public QuadTrigger() : base(_Enums.QuadTrigger)
		{
		}

		public override string ToString()
		{
			string text = base.BaseSaveString() + string.Format(CultureInfo.InvariantCulture, 
				"<tA>handle1<tB>{0}<tB>{1}<tA>handle2<tB>{2}<tB>{3}<tA>handle3<tB>{4}<tB>{5}<tA>handle4<tB>{6}<tB>{7}", 
				handle1.x, handle1.y,
				handle2.x, handle2.y,
				handle3.x, handle3.y,
				handle4.x, handle4.y
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
				switch (text)
				{
					case "handle1":
						handle1 = new Vector2(float.Parse(array[1], CultureInfo.InvariantCulture), float.Parse(array[2], CultureInfo.InvariantCulture));
						break;
					case "handle2":
						handle2 = new Vector2(float.Parse(array[1], CultureInfo.InvariantCulture), float.Parse(array[2], CultureInfo.InvariantCulture));
						break;
					case "handle3":
						handle3 = new Vector2(float.Parse(array[1], CultureInfo.InvariantCulture), float.Parse(array[2], CultureInfo.InvariantCulture));
						break;
					case "handle4":
						handle4 = new Vector2(float.Parse(array[1], CultureInfo.InvariantCulture), float.Parse(array[2], CultureInfo.InvariantCulture));
						break;
				}
			}

			unrecognizedSaveStrings.Remove("handle1");
			unrecognizedSaveStrings.Remove("handle2");
			unrecognizedSaveStrings.Remove("handle3");
			unrecognizedSaveStrings.Remove("handle4");
		}

		public bool PerformWait => true;

		public bool CheckCondition(Player player, Room room)
		{
			return Custom.TriContainsPoint(player.mainBodyChunk.pos, handle1, handle2, handle3) || Custom.TriContainsPoint(player.mainBodyChunk.pos, handle1, handle3, handle4);
		}

		public void InitAtPosition(Vector2 pos)
		{
			handle1 = pos + new Vector2(-150f, -150f);
			handle2 = handle1 + new Vector2(100f, 0f);
			handle3 = handle1 + new Vector2(100f, 100f);
			handle4 = handle1 + new Vector2(0f, 100f);
		}

		public void InitDevUI(TriggerPanel triggerPanel)
		{
			QuadTriggerHandle h1, h2, h3, h4;
			triggerPanel.subNodes.Add(h1 = new QuadTriggerHandle(triggerPanel.owner, "QuadTrigger_Handle1", triggerPanel, this, 1));
			triggerPanel.subNodes.Add(h2 = new QuadTriggerHandle(triggerPanel.owner, "QuadTrigger_Handle2", triggerPanel, this, 2));
			triggerPanel.subNodes.Add(h3 = new QuadTriggerHandle(triggerPanel.owner, "QuadTrigger_Handle3", triggerPanel, this, 3));
			triggerPanel.subNodes.Add(h4 = new QuadTriggerHandle(triggerPanel.owner, "QuadTrigger_Handle4", triggerPanel, this, 4));
			h1.other = h4;
			h2.other = h1;
			h3.other = h2;
			h4.other = h3;
		}

		private class QuadTriggerHandle : Handle
		{
			private readonly QuadTrigger trigger;
			private readonly int handle;
			private readonly FSprite connector;
			private readonly FSprite? panelConnector;
			public QuadTriggerHandle? other;

			private Vector2 handlePos
			{
				get => handle switch
				{
					1 => trigger.handle1,
					2 => trigger.handle2,
					3 => trigger.handle3,
					4 => trigger.handle4,
					_ => throw new InvalidOperationException()
				};
				set
				{
					switch (handle)
					{
						case 1: trigger.handle1 = value; break;
						case 2: trigger.handle2 = value; break;
						case 3: trigger.handle3 = value; break;
						case 4: trigger.handle4 = value; break;
						default: throw new InvalidOperationException();
					}
				}
			}

			private Vector2 handleNext
			{
				get => handle switch
				{
					1 => trigger.handle2,
					2 => trigger.handle3,
					3 => trigger.handle4,
					4 => trigger.handle1,
					_ => throw new InvalidOperationException()
				};
			}

			public QuadTriggerHandle(DevUI owner, string IDstring, DevUINode parentNode, QuadTrigger quadTrigger, int handle)
				: base(owner, IDstring, parentNode, (handle switch
				{
					// this is cursed and I'm dreadfully sorry
					1 => quadTrigger.handle1,
					2 => quadTrigger.handle2,
					3 => quadTrigger.handle3,
					4 => quadTrigger.handle4,
					_ => throw new ArgumentOutOfRangeException(nameof(handle)),
				}) - owner.game.cameras[0].pos - quadTrigger.panelPosition)
			{
				trigger = quadTrigger;
				this.handle = handle;
				if (handle == 1)
				{
					panelConnector = new FSprite("pixel")
					{
						anchorY = 0f
					};
					fSprites.Add(panelConnector);
					owner.placedObjectsContainer.AddChild(panelConnector);
				}

				connector = new FSprite("pixel")
				{
					anchorY = 0f
				};
				fSprites.Add(connector);
				owner.placedObjectsContainer.AddChild(connector);
			}

			public override void Update()
			{
				base.Update();
				Vector2 camPos = owner.game.cameras[0].pos;
				if (dragged)
				{
					handlePos = camPos + absPos;
				}
				else
				{
					AbsMove(handlePos - camPos);
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
				connector.SetPosition(handlePos - camPos);
				connector.rotation = Custom.AimFromOneVectorToAnother(handlePos, handleNext);
				connector.scaleY = Vector2.Distance(handlePos, handleNext);
			}
		}
	}
}
