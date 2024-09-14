using DevInterface;

namespace RegionKit.Modules.Iggy;

public class Iggy : DevInterface.Panel, IGiveAToolTip
{
	public const float FACE_H = 180f;
	public const float PADDING = 5f;
	public const float EXTRA_TOP_PADDING = 7.5f;
	public const float LINE_SPACING = 2f;
	public const string TITLE = "Iggy";
	public const float SIZE_X = 200f;
	public const float SIZE_Y = 200f;
	public const float LINE_MAX_LENGTH = SIZE_X - 2 * PADDING;
	public readonly static Vector2 UNSCRUNGLE_PIXEL_BOUNDARIES = new(0.1f, 0.1f);
	public readonly static Vector2 TOTALSIZE = new(SIZE_X, SIZE_Y);
	public readonly static Color iggyColor = new Color(1f, 0.8f, 0.3f);
	public FSprite face;
	public Message? currentlyShowing;
	public DateTime lastFrameOver;
	public int currentFrame = 0;
	public int maxFrames = 111;
	public readonly static TimeSpan frameDuration = TimeSpan.FromMilliseconds(25.0);
	public const string SPRITE_NAMEBASE = "iggymod"; //Animation was made by MerFaruk
	public const string NUMBER_FORMAT = "D4";
	public readonly List<(FLabel, Vector2)> speech = new();
	bool IGeneralMouseOver.MouseOverMe => MouseOver;
	public ToolTip? ToolTip => new("This is me, dummy.", 1, this);
	public string GetCurrentFaceElement => SPRITE_NAMEBASE + (currentFrame + 1).ToString(NUMBER_FORMAT);
	public Iggy(
		DevUI owner,
		string IDstring,
		DevUINode parentNode) : base(
			owner,
			IDstring,
			parentNode,
			_Module.__IggyPos,
			TOTALSIZE,
			TITLE)
	{
		face = new("assets/regionkit/clippy");
		face.height = FACE_H;
		face.scaleX = face.scaleY * -1f;
		face.anchorX = 1f;
		face.anchorY = 0f;
		this.fSprites.Add(face);
		lastFrameOver = DateTime.Now;
		if (owner != null)
		{
			Futile.stage.AddChild(face);
		}
		if (_Module.__IggyCollapsed) this.ToggleCollapse();
		if (owner is null) return;
		FShader holoShader = owner.game.rainWorld.Shaders["Hologram"];
		//face.shader = holoShader;
		fLabels[0].shader = holoShader;
		fLabels[0].color = iggyColor;
		fSprites[0].shader = holoShader;
		for (int i = 1; i < 5; i++)
		{
			FSprite sprite = fSprites[i];
			sprite.color = iggyColor;
			sprite.shader = holoShader;
		}
		// SetText("""
		// What the fuck did you just fucking say about me, you little bitch? I'll have you know I graduated top of my class in the Navy Seals, and I've been involved in numerous secret raids on Al-Quaeda, and I have over 300 confirmed kills. I am trained in gorilla warfare and I'm the top sniper in the entire US armed forces. You are nothing to me but just another target. I will wipe you the fuck out with precision the likes of which has never been seen before on this Earth, mark my fucking words. You think you can get away with saying that shit to me over the Internet? Think again, fucker. As we speak I am contacting my secret network of spies across the USA and your IP is being traced right now so you better prepare for the storm, maggot. The storm that wipes out the pathetic little thing you call your life. You're fucking dead, kid. I can be anywhere, anytime, and I can kill you in over seven hundred ways, and that's just with my bare hands. Not only am I extensively trained in unarmed combat, but I have access to the entire arsenal of the United States Marine Corps and I will use it to its full extent to wipe your miserable ass off the face of the continent, you little shit. If only you could have known what unholy retribution your little "clever" comment was about to bring down upon you, maybe you would have held your fucking tongue. But you couldn't, you didn't, and now you're paying the price, you goddamn idiot. I will shit fury all over you and you will drown in it. You're fucking dead, kiddo.
		// """);

	}
	public override void Update()
	{
		base.Update();
		if (currentlyShowing != _Module.__messageSystem.currentMessage)
		{
			LogTrace("Iggy needs to change speech labels...");
			SetText(_Module.__messageSystem.currentMessage?.text);
			AlignText();
			currentlyShowing = _Module.__messageSystem.currentMessage;
			LogTrace("Speech labels changed");
		}

		_Module.__IggyCollapsed = collapsed;
		_Module.__IggyPos = pos;
		if (DateTime.Now - lastFrameOver > frameDuration)
		{
			currentFrame++;
			if (currentFrame >= maxFrames)
			{
				currentFrame = 0;
			}
			lastFrameOver = DateTime.Now;
		}
		face.element = Futile.atlasManager.GetElementWithName(GetCurrentFaceElement);
		//fSprites[0].color = new Color(0.3f, 0.3f, 0.3f).Deviation(new Color(0.1f, 0.1f, 0.1f));
	}
	public override void Refresh()
	{
		base.Refresh();
		face.SetPosition(this.absPos + new Vector2(PADDING, PADDING) + UNSCRUNGLE_PIXEL_BOUNDARIES);
		face.isVisible = !collapsed;
		AlignText();
	}


	public override void ClearSprites()
	{
		base.ClearSprites();
		ClearText();
		Futile.stage.RemoveChild(face);
	}

	private void AlignText()
	{
		// LogTrace($"Begin align lines");
		foreach ((FLabel label, Vector2 pos) in speech)
		{
			Vector2 newPosition = absPos + pos + UNSCRUNGLE_PIXEL_BOUNDARIES;
			label.SetPosition(newPosition);
			// LogTrace($"<{label.text}>, {newPosition}");
			label.isVisible = !collapsed;
			label.color = iggyColor;
		}
		// LogTrace($"End align lines");
	}
	public void SetText(string? text)
	{
		ClearText();
		if (text is null)
		{
			LogTrace("Text received is null");
			return;
		}
		FFont fFont = Futile.atlasManager.GetFontWithName(Custom.GetFont());
		float lineHeight = fFont.LineHeight();
		float maxCharWidth = 5.5f;//fFont.maxCharWidth;
		int maxLines = Mathf.FloorToInt((SIZE_Y - PADDING * 2f - (FACE_H / 2f) /*thanks futile for making the sprite's visible size half of what it was meant to be*/) / lineHeight);
		int charsPerLine = Mathf.FloorToInt(LINE_MAX_LENGTH / maxCharWidth);
		Vector2 labelPos = new Vector2(PADDING, SIZE_Y - PADDING - EXTRA_TOP_PADDING);
		List<string> lines = text.SplitAndRemoveEmpty("\n").ToList();
		for (int i = 0; i < lines.Count && speech.Count < maxLines; i++)
		{
			string line = lines[i];
			string toAdd;
			string until_limit = line[0..Math.Min(charsPerLine, line.Length)];
			if (until_limit == line)
			{
				toAdd = line;
			}
			else
			{
				toAdd = until_limit;
				lines[i] = line[charsPerLine..];
				i--;
			}
			FLabel newLabel = new(GetFont(), toAdd);
			newLabel.anchorX = newLabel.anchorY = 0f;
			speech.Add((newLabel, labelPos));
			newLabel.SetPosition(labelPos);
			labelPos.y -= lineHeight + LINE_SPACING;
			//newLabel.textRect = new Rect(labelPos, new Vector2(LINE_MAX_LENGTH, lineHeight));
		}
		foreach ((FLabel label, Vector2 pos) in speech)
		{
			// LogTrace($"Adding line {label} at internal pos {pos}");
			fLabels.Add(label);
			Futile.stage.AddChild(label);
		}
	}
	public void ClearText()
	{
		foreach ((FLabel label, _) in speech)
		{
			label.RemoveFromContainer();
			fLabels.Remove(label);
		}
		speech.Clear();
	}
}
