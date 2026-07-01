using System.Data.SqlClient;
using System.Globalization;
using DevInterface;
using RegionKit.Extras.FutileExtras;
using RegionKit.Modules.DevUIMisc.GenericNodes;
using Watcher;
using Random = UnityEngine.Random;

namespace RegionKit.Modules.FloatingDebrisNew
{
	internal class ColoredDust : FloatingDebris.Floater, IDrawable
	{
		private float rotation;
		private float lastRotation;
		private int step;
		private int totalFloaters;
		private int totalConPoints;
		private readonly bool debug;
		private Vector2 splinePos;
		private Vector2 lastOrigPos;
		private DebugSprite? debugPosSprite;
		private DebugSprite? debugTargetSprite;
		private DebugSprite? debugLineSprite;
		private Vector2 random;
		private float shaderTime;
		private float speed;
		private float intensity;
		public Vector2 smoothPos;
		public Color lastDustColor;
		public Color dustColor;
		public float lastDustFade;
		public float dustFade;
		public Color lastSparklesColor;
		public Color sparklesColor;

		public new Vector2 getPos
		{
			get
			{
				preMovementPos = origPos + offset * offsetAmount;
				float num = Mathf.Sin(time + offset.x * Mathf.PI * 2f) * offset.y * 10f;
				num += Mathf.Sin(time + origPos.x / 20f) * 10f;
				num *= 1f - depth;
				return preMovementPos + Vector2.up * (num * movement);
			}
		}

		public ColoredDust(FloatingDebris.FloaterData data) : base(data)
		{
			random = new Vector2(Random.value, Random.value);
		}

		public override void Update(bool eu)
		{
			InitDebug();
			UpdateCounts();
			UpdateRotation();
			intensity = Mathf.Clamp01((extraOffsets.x + 1f) * 0.5f * (1f + random.y * 0.3f));
			speed = Mathf.Clamp01((extraOffsets.y + 1f) * 0.5f);
			lastOrigPos = origPos;
			if (room.BeingViewed)
			{
				time += Time.fixedDeltaTime * 2f * Mathf.Clamp01(Mathf.Abs(speed * 2f - 1f) * 3f);
			}
			lastPos = pos;
			pos = getPos;

			lastDustColor = dustColor;
			lastSparklesColor = sparklesColor;
			lastDustFade = dustFade;

			ColoredDustData? lastData = this.GetExtraDataAt<ColoredDustData>(index - 1);
			ColoredDustData? thisData = this.GetExtraDataAt<ColoredDustData>(index);
			ColoredDustData? nextData = this.GetExtraDataAt<ColoredDustData>(index + 1);
			if (lastData != null && thisData != null && nextData != null)
			{
				if (influence > 0.5f)
				{
					dustColor = Color.Lerp(thisData.dustColor, nextData.dustColor, influence - 0.5f);
					dustFade = Mathf.Lerp(thisData.dustFade, nextData.dustFade, influence - 0.5f);
					sparklesColor = Color.Lerp(thisData.sparklesColor, nextData.sparklesColor, influence - 0.5f);
					// sparklesColor = HSLColor.Lerp(thisData.sparklesColor.HSL(), nextData.sparklesColor.HSL(), influence - 0.5f).rgb;
				}
				else
				{
					dustColor = Color.Lerp(lastData.dustColor, thisData.dustColor, influence + 0.5f);
					dustFade = Mathf.Lerp(lastData.dustFade, thisData.dustFade, influence + 0.5f);
					sparklesColor = Color.Lerp(lastData.sparklesColor, thisData.sparklesColor, influence + 0.5f);
					// sparklesColor = HSLColor.Lerp(lastData.sparklesColor.HSL(), thisData.sparklesColor.HSL(), influence + 0.5f).rgb;
				}
			}
			else if (thisData != null)
			{
				dustColor = thisData.dustColor;
			}
			else
			{
				LogWarning($"ERROR GETTING OTHER DATA {lastData != null} {thisData != null} {nextData != null}");

				// make it something very visually interesting so it's very obvious something fucked up
				dustColor = Color.magenta;
				sparklesColor = Color.green;
				dustFade = 1f;
			}
		}

		private void UpdateCounts()
		{
			step = Mathf.Max(totalFloaters / Mathf.Max(totalConPoints, 1), 2);
			totalFloaters = owner.floaters.Count;
			totalConPoints = owner.data.controlPointPosX.Count;
		}

		public override void Destroy()
		{
			base.Destroy();
			if (debug)
			{
				debugTargetSprite!.Destroy();
				debugPosSprite!.Destroy();
				debugLineSprite!.Destroy();
			}
		}

		private void InitDebug()
		{
			if (!debug || debugPosSprite != null)
			{
				return;
			}
			debugPosSprite = new DebugSprite(Vector2.zero, new FSprite("Futile_White", true)
			{
				color = Color.red
			}, room);
			debugTargetSprite = new DebugSprite(Vector2.zero, new FSprite("Futile_White", true)
			{
				color = Color.green
			}, room);
			debugLineSprite = new DebugSprite(Vector2.zero, new FSprite("Futile_White", true)
			{
				color = Color.blue,
				anchorY = 0f,
				scaleX = 0.12f
			}, room);
			room.AddObject(debugPosSprite);
			room.AddObject(debugTargetSprite);
			room.AddObject(debugLineSprite);
		}

		private void UpdateRotation()
		{
			lastRotation = rotation;
			Vector2 offset = this.offset;
			rotation = GetAngle() + 90f;
			if (debug)
			{
				debugPosSprite!.pos = preMovementPos;
				debugTargetSprite!.pos = offset;
				debugLineSprite!.pos = preMovementPos;
				debugLineSprite!.sprite.rotation = rotation;
				debugLineSprite!.sprite.scaleY = (offset - pos).magnitude;
			}
		}

		private Vector2 ConPos(int i)
		{
			return owner.data.ConPos(i);
		}

		private float GetAngle()
		{
			if (totalConPoints <= 1)
			{
				return VecToDeg(offset);
			}
			float num = VecToDeg(Vector2.Perpendicular(ConPos(index) - ConPos(index - 1)));
			float num2 = VecToDeg(Vector2.Perpendicular(ConPos(index + 1) - ConPos(index)));
			if (index == 0 && influence <= 0.5f)
			{
				return num2;
			}
			if (index == totalConPoints - 2 && influence > 0.5f)
			{
				return num2;
			}
			float num3 = VecToDeg(Vector2.Perpendicular(ConPos(index + 2) - ConPos(index + 1)));
			if (influence > 0.5f)
			{
				return Mathf.LerpAngle(num2, num3, influence - 0.5f);
			}
			return Mathf.LerpAngle(num, num2, influence + 0.5f);
		}


		public virtual void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
		{
			sLeaser.sprites = new FSprite[1];
			sLeaser.sprites[0] = new FSpriteUVs("Futile_White")
			{
				shader = rainWorld.Shaders["RKColoredDust"]
			};
			AddToContainer(sLeaser, rCam, null!);
		}

		public virtual void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
		{
			sLeaser.sprites[0].SetPosition(Vector2.Lerp(lastPos, pos, timeStacker) - camPos);
			sLeaser.sprites[0].rotation = Mathf.LerpAngle(lastRotation, rotation, timeStacker);
			sLeaser.sprites[0].color = new Color(random.x, Mathf.Clamp01(_depth + depthOffset * 0.5f), speed, intensity);
			sLeaser.sprites[0].scaleX = 2f * finalScale;
			sLeaser.sprites[0].scaleY = 4f * finalScale;
			FSpriteUVs uvsSprite = (sLeaser.sprites[0] as FSpriteUVs)!;
			uvsSprite.SetUV(Vector2.Lerp(new Vector2(lastDustColor.r, lastDustColor.g), new Vector2(dustColor.r, dustColor.g), timeStacker), 2);
			uvsSprite.SetUV(new Vector2(Mathf.Lerp(lastDustColor.b, dustColor.b, timeStacker), Mathf.Lerp(lastDustFade, dustFade, timeStacker)), 3);
			uvsSprite.SetUV(Vector2.Lerp(new Vector2(lastSparklesColor.r, lastSparklesColor.g), new Vector2(sparklesColor.r, sparklesColor.g), timeStacker), 4);
			uvsSprite.SetUV(new Vector2(Mathf.Lerp(lastSparklesColor.b, sparklesColor.b, timeStacker), 1f), 5);
			if (slatedForDeletetion || room != rCam.room)
			{
				sLeaser.CleanSpritesAndRemove();
				return;
			}
		}

		public virtual void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
		{
			rCam.ReturnFContainer("GrabShaders").AddChild(sLeaser.sprites[0]);
		}

		public virtual void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
		{
		}

		public class ColoredDustSpawner() : IFloaterSpawner, ICreateFloaterExtraData
		{
			public FloatingDebris.UIText GetUIText()
			{
				return uiText;
			}

			public virtual FloatingDebris.Floater Spawn(FloatingDebris.FloaterData data)
			{
				return new ColoredDust(data);
			}

			public static FloatingDebris.UIText uiText = new()
			{
				depthFar = FloatingDebris.UIText.Title("Depth Far"),
				depthNear = FloatingDebris.UIText.Title("Depth Near"),
				scaleMax = FloatingDebris.UIText.Title("Scale Max"),
				scaleMin = FloatingDebris.UIText.Title("Scale Min"),
				scaleOffset = FloatingDebris.UIText.Title("Scale Offset"),
				depthOffset = FloatingDebris.UIText.Title("Depth Offset"),
				extraSlider1 = FloatingDebris.UIText.Title("Intensity"),
				extraSlider2 = FloatingDebris.UIText.Title("Wind Speed")
			};

			public string DataKeyword => "RKColoredDust";

			public bool CopyDataToNewPoints => true;

			public IFloaterExtraData CreateData() => new ColoredDustData();
		}

		public class ColoredDustData : IFloaterExtraData
		{
			public Color dustColor = Color.black;
			public float dustFade = 0.5f;
			public Color sparklesColor = Custom.HSL2RGB(Random.value, 1f, 0.5f);
			public Vector2 panelPos = new Vector2(50f, 50f);

			public string SaveFloaterData()
			{
				return string.Format(
					CultureInfo.InvariantCulture,
					"{0}^{1}^{2}^{3}^{4}^{5}^{6}^{7}^{8}",
					panelPos.x,
					panelPos.y,
					dustColor.r,
					dustColor.g,
					dustColor.b,
					dustFade,
					sparklesColor.r,
					sparklesColor.g,
					sparklesColor.b
					);
			}

			public void LoadFloaterData(string data)
			{
				string[] split = data.Split('^');
				int i = 0;
				if (split.Length > i) float.TryParse(split[i++], NumberStyles.Any, CultureInfo.InvariantCulture, out panelPos.x);
				if (split.Length > i) float.TryParse(split[i++], NumberStyles.Any, CultureInfo.InvariantCulture, out panelPos.y);
				if (split.Length > i) float.TryParse(split[i++], NumberStyles.Any, CultureInfo.InvariantCulture, out dustColor.r);
				if (split.Length > i) float.TryParse(split[i++], NumberStyles.Any, CultureInfo.InvariantCulture, out dustColor.g);
				if (split.Length > i) float.TryParse(split[i++], NumberStyles.Any, CultureInfo.InvariantCulture, out dustColor.b);
				if (split.Length > i) float.TryParse(split[i++], NumberStyles.Any, CultureInfo.InvariantCulture, out dustFade);
				if (split.Length > i) float.TryParse(split[i++], NumberStyles.Any, CultureInfo.InvariantCulture, out sparklesColor.r);
				if (split.Length > i) float.TryParse(split[i++], NumberStyles.Any, CultureInfo.InvariantCulture, out sparklesColor.g);
				if (split.Length > i) float.TryParse(split[i++], NumberStyles.Any, CultureInfo.InvariantCulture, out sparklesColor.b);
			}

			public void CreateDevUI(DevUI owner, DevUINode parentNode)
			{
				parentNode.subNodes.Add(new ColoredDustDataPanel(owner, "ColoredDustPanel", parentNode, panelPos, this));
			}
		}

		public class ColoredDustDataPanel : Panel, IDevUISignals
		{
			private readonly ColoredDustData data;
			private readonly FSprite panelLine;
			private readonly RGBSelectButton dustColorButton;
			private readonly GenericSlider dustFadeSlider;
			private readonly RGBSelectButton sparkleColorButton;

			public ColoredDustDataPanel(DevUI owner, string IDstring, DevUINode parentNode, Vector2 pos, ColoredDustData data) : base(owner, IDstring, parentNode, pos, new Vector2(200f, 65f), "Colored Dust Data")
			{
				this.data = data;

				subNodes.Add(new DevUILabel(owner, "SparklesLabel", this, new Vector2(5f, 45f), 80f, "Sparkles:"));
				sparkleColorButton = new RGBSelectButton(owner, "SparklesColor", this, new Vector2(90f, 45f), 105f, "Sparkles color", data.sparklesColor, "Sparkles color");
				subNodes.Add(sparkleColorButton);

				subNodes.Add(new DevUILabel(owner, "DustLabel", this, new Vector2(5f, 25f), 80f, "Dust color:"));
				dustColorButton = new RGBSelectButton(owner, "DustColor", this, new Vector2(90f, 25f), 105f, "Dust color", data.dustColor, "Dust color");
				subNodes.Add(dustColorButton);

				dustFadeSlider = new GenericSlider(owner, "FadeSlider", this, new Vector2(5f, 5f), "Dust fade:", false, 50f, data.dustFade, true, 26f)
				{
					displayRounding = 2
				};
				subNodes.Add(dustFadeSlider);

				panelLine = new FSprite("pixel")
				{
					anchorY = 1f
				};
				fSprites.Add(panelLine);
				Futile.stage.AddChild(panelLine);
			}

			public override void Refresh()
			{
				base.Refresh();
				data.panelPos = pos;

				panelLine.SetPosition(absPos + new Vector2(0.01f, 0.01f));
				panelLine.scaleY = data.panelPos.magnitude;
				panelLine.rotation = Custom.AimFromOneVectorToAnother((parentNode as PositionedDevUINode)!.absPos, absPos);
			}

			public void Signal(DevUISignalType type, DevUINode sender, string message)
			{
				if (sender == sparkleColorButton)
				{
					data.sparklesColor = sparkleColorButton.actualValue;
				}
				else if (sender == dustColorButton)
				{
					data.dustColor = dustColorButton.actualValue;
				}
				else if (sender == dustFadeSlider)
				{
					data.dustFade = dustFadeSlider.actualValue;
				}
			}
		}
	}
}
