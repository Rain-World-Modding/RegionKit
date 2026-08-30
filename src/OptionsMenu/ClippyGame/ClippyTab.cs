using System.IO;
using Menu.Remix;
using Menu.Remix.MixedUI;
using Unity.Mathematics;
using UnityEngine;
using static RegionKit.Modules.MoonIO.IOType;
using static RegionKit.OptionsMenu.ClippyGame.ClippyTab;
using Random = UnityEngine.Random;

namespace RegionKit.OptionsMenu.ClippyGame
{
	public class ClippyTab : OpTab
	{
		public const bool DEBUG = false;

		public class Spark : UIelement
		{
			new public Vector2 pos;

			public Vector2 vel;

			new public OpTab tab;

			public FSprite sprite;

			public Spark(OpTab tab, Vector2 pos, Vector2 vel) : base(pos, 0f)
			{
				this.tab = tab;
				this.pos = pos;
				this.vel = vel;

				sprite = new FSprite("pixel");
				sprite.scaleY = 2f;

				sprite.x = pos.x;
				sprite.y = pos.y;
				sprite.rotation = Custom.VecToDeg(Custom.DirVec(pos, vel));

				tab._container.AddChild(sprite);
			}

			public override void Update()
			{
				pos += vel;
				vel *= 0.99f;

				vel.y += -0.9f;

				sprite.x = pos.x;
				sprite.y = pos.y;
				sprite.rotation = Custom.VecToDeg(Custom.DirVec(pos, vel));

				if (pos.y < 0f)
				{
					tab._container.RemoveChild(sprite);
					tab.RemoveItems(this);
				}
			}
		}

		public class CatCube : UIelement
		{
			public FSprite[] trail;

			public FSprite cat;

			public float _speed = 2f;

			public float speed
			{
				get
				{
					return _speed + speedadd;
				}
				set
				{
					_speed = value;
				}
			}

			public float speedadd;

			public bool clickLock;

			public Vector2 dir;

			new public Vector2 _pos;

			public Vector2[] lastposArray;

			new public Vector2 pos => _pos - container.GetPosition();

			public FContainer container => tab._container;

			new public Vector2 MousePos
			{
				get
				{
					return new Vector2(Futile.mousePosition.x, Futile.mousePosition.y);
				}
			}

			new public bool MouseOver
			{
				get
				{
					Vector2 vec = pos;

					if (MousePos.x > vec.x - (size.x / 2f) && MousePos.x < vec.x + (size.x / 2f))
					{
						return MousePos.y > vec.y - (size.y / 2f) && MousePos.y < vec.y + (size.y / 2f);
					}

					return false;
				}
			}

			public CatCube(OpTab tab, Vector2 pos, float scale) : base(pos, new Vector2(50f, 50f) * scale)
			{
				this.tab = tab;
				this._pos = pos;

				cat = new FSprite("assets/regionkit/clippy_cat");

				cat.x = this.pos.x;
				cat.y = this.pos.y;
				cat.scaleX = (50f * scale) / cat.element.sourcePixelSize.x;
				cat.scaleY = (50f * scale) / cat.element.sourcePixelSize.y;

				container.AddChild(cat);

				trail = new FSprite[5];
				lastposArray = new Vector2[trail.Length];
				for (int i = 0; i < trail.Length; i++)
				{
					trail[i] = new FSprite("assets/regionkit/clippy_cat");
					trail[i].x = cat.x;
					trail[i].y = cat.y;
					trail[i].scaleX = cat.scaleX;
					trail[i].scaleY = cat.scaleY;
					trail[i].alpha = 1f - ((float)(i + 1) / (float)(trail.Length + 1));

					lastposArray[i] = cat.GetPosition();
					container.AddChild(trail[i]);
				}

				trail[4].MoveBehindOtherNode(cat);
				trail[3].MoveBehindOtherNode(cat);
				trail[2].MoveBehindOtherNode(cat);
				trail[1].MoveBehindOtherNode(cat);
				trail[0].MoveBehindOtherNode(cat);

				dir = new Vector2(Random.value > 0.5f ? 1f : -1f, Random.value > 0.5f ? 1f : -1f);
			}

			public override void Update()
			{
				base.Update();

				lastposArray[4] = lastposArray[3];
				lastposArray[3] = lastposArray[2];
				lastposArray[2] = lastposArray[1];
				lastposArray[1] = lastposArray[0];
				lastposArray[0] = pos;

				speedadd = Mathf.Lerp(speedadd, 0f, 0.1f);
				_pos += dir * speed;

				if (pos.x - (size.x / 2f) <= -11f)
				{
					dir.x *= dir.x > 0f ? 1f : -1f;
				}
				else if (pos.x + (size.x / 2f) >= tab.CanvasSize.x + 11f)
				{
					dir.x *= dir.x < 0f ? 1f : -1f;
				}

				if (pos.y - (size.y / 2f) <= -6f)
				{
					dir.y *= dir.y > 0f ? 1f : -1f;
				}
				else if (pos.y + (size.y / 2f) >= tab.CanvasSize.y + 12f)
				{
					dir.y *= dir.y < 0f ? 1f : -1f;
				}

				cat.x = pos.x;
				cat.y = pos.y;

				for (int i = 0; i < trail.Length; i++)
				{
					trail[i].x = lastposArray[i].x;
					trail[i].y = lastposArray[i].y;
					trail[i].scaleX = cat.scaleX;
					trail[i].scaleY = cat.scaleY;
				}

				if (MouseOver && Input.GetKey(KeyCode.Mouse0))
				{
					if (!clickLock)
					{
						ConfigContainer.PlaySound(_Enums.CatCube_Meow, 0.5f, 1f, 1f);
					}

					clickLock = true;
				}
				else
				{
					clickLock = false;
				}
			}

			public override void Unload()
			{
				base.Unload();
				for (int i = 0; i < trail.Length; i++)
				{
					container.RemoveChild(trail[i]);
				}

				container.RemoveChild(cat);
			}
		}


		public class PointLabel : OpLabel
		{
			public int c;

			public Color? _c;

			public Vector2 dir;

			public Color clr
			{
				get
				{
					if (_c.HasValue)
					{
						return _c.Value;
					}
					else
					{
						return HSL2RGB(Custom.Mod(c / 20f, 1f), 1f, 0.5f);
					}
				}
			}

			public PointLabel(float posX, float posY, string s) : base(posX, posY, s)
			{
				Init(posX, posY);
			}

			public PointLabel(float posX, float posY, string s, Color clr) : base(posX, posY, s)
			{
				_c = clr;

				Init(posX, posY);
			}

			public void Init(float x, float y)
			{
				dir = DegToVec(((UnityEngine.Random.value * 0.5f) - 0.25f) * 180f);
				alignment = FLabelAlignment.Center;
				pos = new Vector2(x, y);
				label.scale *= 1.4f;
				c = 40;
				alpha = 0f;
				color = clr;
			}

			public override void Update()
			{
				base.Update();

				pos += dir * 3f;
				c--;
				alpha = c / 40f;

				color = clr;
				if (c == 0)
				{
					tab.RemoveItems(this);
					Unload();
				}
			}
		}

		public int score = 0;

		public int highScore = 0;

		public bool newScore = true;

		public bool clicklock = false;

		public FSprite clippy = null!;

		public OpLabel milestoneLabel = null!;

		public OpLabel milestoneLabelNum = null!;

		public OpLabel pointslabel = null!;

		public OpSimpleButton joarButton = null!;

		public int blink;

		public FSprite speechBubble;

		public FLabel text;

		public int talkcounter;

		public bool loaded;

		public int teleportCooldown;

		public float tScale;

		public FSprite echoEffect = null!;

		public FContainer echoEffectContainer = null!;

		public bool talking
		{
			get
			{
				return talkcounter > 0;
			}
		}

		public int nextMilestone
		{ 
			get
			{
				return score switch
				{
					< 10000 => 10000,      // rainbow
					< 100000 => 100000,    // moving around
					< 200000 => 200000,    // joar 
					< 400000 => 400000,    // echo mode
					< 600000 => 600000,    // unknown
					< 800000 => 800000,    // unknown
					< 1000000 => 1000000,  // omega clippy
					< 2000000 => 2000000,  // finale

					_ => (int)Mathf.Pow(10f, Mathf.Ceil(math.log10((score > highScore ? score : highScore) + 0.5f)))
				};
			}
		}

		public bool Rainbow => score >= 10000 && !(Runaround || Echo || idk2 || OmegaClippy);

		public bool Runaround => score >= 100000 && !(Echo || idk2 || OmegaClippy);

		public bool JoarMode => score >= 200000 && !disableJoar;
		public bool Echo => score >= 400000 && !(idk2 || OmegaClippy);

		public bool cat_Cube => score >= 600000 && !disableCat;

		public bool idk2 => score >= 800000 && !OmegaClippy;

		public bool OmegaClippy => score >= 1000000 && false;

		public bool finale = false;

		public bool disableJoar = false;

		public bool disableCat = false;

		public bool cutscene = true;

		public int cutsceneCounter;

		public bool kill;

		public CatCube catCube = null!;

		public Vector2 velocity;

		public Vector2 target;

		public Vector2 targetRND;

		public int rainbowCounter;

		public float targetSize = 2f;

		public float targetSizeLerp;

		public int points_per_click
		{
			get
			{
				if (DEBUG && Input.GetKey(KeyCode.LeftControl))
				{
					return nextMilestone / 50;
				}

				if (OmegaClippy)
				{
					return 1;
				}
				else if (idk2)
				{
					return 1000;
				}
				else if (cat_Cube)
				{
					return 750;
				}
				else if (Echo)
				{
					return 500;
				}
				else if (Runaround)
				{
					return 300;
				}
				else if (Rainbow)
				{
					return 150;
				}

				return 100;
			}
		}

		public Vector2 MousePos
		{
			get
			{
				return new Vector2(Futile.mousePosition.x, Futile.mousePosition.y);
			}
		}

		public bool MouseOver
		{
			get
			{
				Vector2 vec = _container.GetPosition() + clippy.GetPosition();

				if (MousePos.x > vec.x - (clippy.width / 2f) && MousePos.x < vec.x + (clippy.width / 2f))
				{
					return MousePos.y > vec.y - (clippy.height / 2f) && MousePos.y < vec.y + (clippy.height / 2f);
				}

				return false;
			}
		}

		public ClippyTab(OptionInterface owner) : base(owner, "???")
		{

		}

		public void Initialize()
		{
			clippy = new FSprite("assets/regionkit/clippy");
			speechBubble = new FSprite("assets/regionkit/clippy_talker");
			text = new FLabel(Custom.GetFont(), "");

			target = new Vector2(CanvasSize.x / 2f, CanvasSize.y / 2f - 50f);
			clippy.x = target.x;
			clippy.y = target.y;
			clippy.scale = targetSize;

			speechBubble.x = clippy.x + 250f;
			speechBubble.y = clippy.y + 150f;
			speechBubble.scale = 2f;

			text.x = speechBubble.x + 20f;
			text.y = speechBubble.y + 15f;
			text.scale = 2f;
			text.color = Color.red;
			text.alignment = FLabelAlignment.Center;

			_container.AddChild(clippy);
			_container.AddChild(speechBubble);
			_container.AddChild(text);
			_container.MoveToFront();

			milestoneLabel = new OpLabel(CanvasSize.x / 2f - 60f, CanvasSize.y - 40f, "Next Milestone:", true);
			milestoneLabelNum = new OpLabel(CanvasSize.x / 2f, CanvasSize.y - 80f, highScore < 1000 ? "1000" : Mathf.Pow(10f, Mathf.Ceil(math.log10(highScore + 0.5f))).ToString(), true);

			milestoneLabel.alignment = FLabelAlignment.Center;
			milestoneLabelNum.alignment = FLabelAlignment.Center;
			milestoneLabel.label.shader = Custom.rainWorld.Shaders["MenuText"];
			milestoneLabelNum.label.shader = Custom.rainWorld.Shaders["MenuText"];
			milestoneLabel.Change();
			milestoneLabelNum.Change();

			pointslabel = new OpLabel(10f, 10f, "Score: " + score, true);
			pointslabel.alignment = FLabelAlignment.Left;
			pointslabel.label.shader = Custom.rainWorld.Shaders["MenuText"];
			pointslabel.Change();

			AddItems(milestoneLabel, milestoneLabelNum, pointslabel);

			if (highScore == 0)
			{
				newScore = false;
			}
		}

		public void Update()
		{
			if (isInactive)
			{
				if (DEBUG)
				{
					if (Input.GetKey(KeyCode.Keypad0))
					{
						cutscene = false;
						score = 0;
					}
					else if (Input.GetKey(KeyCode.Keypad1))
					{
						cutscene = false;
						score = 1000000;
					}
					else if (Input.GetKey(KeyCode.Keypad2))
					{
						cutscene = true;
						score = 1000000;
					}
				}

				return;
			}

			if (!loaded)
			{
				// this.name = "Clippy";

				talkcounter = 40;
				speechBubble.isVisible = true;
				text.isVisible = true;
				text.text = RandomDialogue();

				loaded = true;
			}

			milestoneLabelNum.text = highScore < 1000 ? "1000" : Mathf.Pow(10f, Mathf.Ceil(math.log10((score > highScore ? score : highScore) + 0.5f))).ToString();
			pointslabel.text = "Score: " + score;

			if (JoarMode)
			{
				if (joarButton == null)
				{
					joarButton = new OpSimpleButton(new Vector2(CanvasSize.x - 90f, CanvasSize.y - 40f), new Vector2(80, 30), "Joar.");
					joarButton.OnClick += JoarClick;

					AddItems(joarButton);
				}

				if (kill)
				{
					Application.Quit();
				}
			}

			if (talkcounter > 0)
			{
				talkcounter--;

				if (talkcounter == 0)
				{
					speechBubble.isVisible = false;
					text.isVisible = false;
				}
			}

			if (UnityEngine.Random.value < 0.005f)
			{
				blink = Mathf.RoundToInt(4f + Random.value * 4f);
			}

			if (blink > 0)
			{
				blink--;

				if (clippy.element.name != "assets/regionkit/clippy_2")
				{
					clippy.SetElementByName("assets/regionkit/clippy_2");
				}
			}
			else if (clippy.element.name != "assets/regionkit/clippy")
			{
				clippy.SetElementByName("assets/regionkit/clippy");
			}

			if (OmegaClippy)
			{
				if (cutscene)
				{
					speechBubble.x = clippy.x + 250f;
					speechBubble.y = clippy.y + 150f;
					text.x = speechBubble.x + 20f;
					text.y = speechBubble.y + 15f;

					targetSizeLerp = Mathf.SmoothStep(targetSizeLerp, targetSize, 0.4f);
					clippy.scaleX = Custom.LerpExpEaseOut(clippy.scaleX, targetSizeLerp, 0.1f);
					clippy.scaleY = Custom.LerpExpEaseOut(clippy.scaleY, targetSizeLerp, 0.1f);

					cutsceneCounter++;

					if (catCube != null)
					{
						catCube.Update();
					}

					if (cutsceneCounter == 80)
					{
						talkcounter = 40;
						speechBubble.isVisible = true;
						text.isVisible = true;
						text.text = "I've had it with\nyour endless abuse";
					}
					else if (cutsceneCounter == 120)
					{
						disableJoar = true;

						for (int i = 0; i < (int)Mathf.Lerp(15f, 25f, Random.value); i++)
						{
							_AddItem(new Spark(this, joarButton.pos, Custom.RNV() * Mathf.Lerp(5f, 10f, Random.value)));
						}
						joarButton.Unload();
						joarButton = null!;
						ConfigContainer.PlaySound(_Enums.Joar_Death, 0.5f, 1f, 1f);
					}
					else if (cutsceneCounter == 160)
					{
						disableCat = true;

						for (int i = 0; i < (int)Mathf.Lerp(15f, 25f, Random.value); i++)
						{
							_AddItem(new Spark(this, catCube.pos, Custom.RNV() * Mathf.Lerp(5f, 10f, Random.value)));
						}
						catCube.Unload();
						catCube = null!;
						ConfigContainer.PlaySound(_Enums.CatCube_Meow, 0.5f, 1f, 1f);
					}
					else if (cutsceneCounter == 200)
					{
						talkcounter = 40;
						speechBubble.isVisible = true;
						text.isVisible = true;
						text.text = "No more mister nice clippy";
					}
					else if (cutsceneCounter == 239)
					{
						Application.Quit();
					}

					return;
				}
			}

			if (talkcounter == 0 && (Random.value < 0.0008f || (DEBUG && Input.GetKeyUp(KeyCode.T))))
			{
				talkcounter = 40;

				speechBubble.isVisible = true;
				text.isVisible = true;
				text.text = RandomDialogue();
			}

			if (Rainbow)
			{
				rainbowCounter++;

				clippy.color = Custom.HSL2RGB(Custom.Mod(rainbowCounter / 120f, 1f), 1f, 0.5f);
			}
			else
			{
				clippy.color = Color.white;
			}

			if (Runaround)
			{
				clippy.x += velocity.x;
				clippy.y += velocity.y;
				velocity *= 0.9f;

				if (Random.value < 0.01f && targetSizeLerp > (clippy.scale - 0.01f) && targetSizeLerp < (clippy.scale + 0.01f))
				{
					targetSize = Mathf.Lerp(0.1f, 2f, Random.value);
				}

				if (Custom.DistLess(clippy.GetPosition(), target, 10f) || (DEBUG && Input.GetKeyDown(KeyCode.U)))
				{
					target = new Vector2(
						((CanvasSize.x - (clippy.width  / clippy.scaleX)) * Random.value) + (clippy.width  / clippy.scaleX),
						((CanvasSize.y - (clippy.height / clippy.scaleY)) * Random.value) + (clippy.height / clippy.scaleY));
				}
				else
				{
					velocity += Custom.DirVec(clippy.GetPosition(), target);
				}
			}

			speechBubble.x = clippy.x + 250f;
			speechBubble.y = clippy.y + 150f;
			text.x = speechBubble.x + 20f;
			text.y = speechBubble.y + 15f;

			targetSizeLerp = Mathf.SmoothStep(targetSizeLerp, targetSize, 0.4f);
			clippy.scaleX = Custom.LerpExpEaseOut(clippy.scaleX, targetSizeLerp, 0.1f);
			clippy.scaleY = Custom.LerpExpEaseOut(clippy.scaleY, targetSizeLerp, 0.1f);

			if (MouseOver && Input.GetKey(KeyCode.Mouse0))
			{
				if (!clicklock || (Input.GetKey(KeyCode.LeftShift) && DEBUG))
				{
					clippy.scaleX += 0.4f * UnityEngine.Random.value;
					clippy.scaleY += 0.4f * UnityEngine.Random.value;

					if (score + points_per_click >= 1000000 && !OmegaClippy)
					{
						score = 1000000;
					}
					else
					{
						score += points_per_click;
					}

					if (score > highScore && newScore)
					{
						newScore = false;
						AddItems(new PointLabel(MousePos.x - _container.x, MousePos.y - _container.y, "NEW HIGHSCORE"));
						ConfigContainer.PlaySound(_Enums.Clippy_Highscore, 0.5f, 1f, 1f);
					}
					else if (Custom.Mod(math.log10(score), 1f) == 0 && score > 100)
					{
						AddItems(new PointLabel(MousePos.x - _container.x, MousePos.y - _container.y, "MILESTONE REACHED"));
						ConfigContainer.PlaySound(_Enums.Clippy_Milestone, 0.5f, 1f, 1f);
					}
					else
					{
						AddItems(new PointLabel(MousePos.x - _container.x, MousePos.y - _container.y, score.ToString(), HSL2RGB(Custom.Mod(score / (score * 10f), 1f), 1f, 0.5f)));
					}

					if (score > highScore)
					{
						highScore = score;
					}
				}

				clicklock = true;
			}
			else
			{
				clicklock = false;
			}

			if (Echo)
			{
				if (echoEffect == null)
				{
					echoEffect = new FSprite("Futile_White");

					echoEffect.x = clippy.x;
					echoEffect.y = clippy.y;
					echoEffect.scale = Mathf.Max(clippy.width / 16f, clippy.height / 16f);
					echoEffect.shader = Custom.rainWorld.Shaders["GhostDistortion"];

					_container.AddChild(echoEffect);
					tScale = clippy.scaleX;
				}
				else
				{
					echoEffect.x = clippy.x;
					echoEffect.y = clippy.y;
					echoEffect.scale = Mathf.Max(clippy.width / 16f, clippy.height / 16f);
				}

				if (DEBUG && Input.GetKeyUp(KeyCode.U))
				{
					teleportCooldown = 0;
				}

				clippy.SetPosition(Vector3.Slerp(clippy.GetPosition(), target + targetRND, 0.1f));
				if (Custom.DistLess(target + targetRND, clippy.GetPosition(), 2f))
				{
					targetRND = Custom.RNV() * 10f;
				}

				if (teleportCooldown == 0)
				{
					tScale = Custom.LerpExpEaseOut(tScale, 0f, 0.33f);
					clippy.scaleX = tScale;

					if (clippy.scaleX < 0.01f)
					{
						target = new Vector2(
							((CanvasSize.x - (clippy.width / clippy.scaleX)) * Random.value) + (clippy.width / clippy.scaleX),
							((CanvasSize.y - (clippy.height / clippy.scaleY)) * Random.value) + (clippy.height / clippy.scaleY));

						targetRND = Custom.RNV() * 10f;
						clippy.SetPosition(target + targetRND);

						teleportCooldown = (int)Mathf.Lerp(20f, 80f, Random.value);
					}
				}
				else if (teleportCooldown > 0)
				{
					tScale = clippy.scaleX;
					teleportCooldown--;
				}
			}
			else if (!Runaround && clippy.GetPosition() != new Vector2(CanvasSize.x / 2f, CanvasSize.y / 2f - 50f))
			{
				clippy.SetPosition(new Vector2(CanvasSize.x / 2f, CanvasSize.y / 2f - 50f));
			}
			else if (echoEffect != null)
			{
				_container.RemoveChild(echoEffect);
				echoEffect = null!;
			}

			if (cat_Cube || (DEBUG && Input.GetKey(KeyCode.C)))
			{
				if (catCube == null)
				{
					catCube = new CatCube(this, MousePos, 1f);
				}
				else
				{
					catCube.Update();
					if (DEBUG && Input.GetKey(KeyCode.R))
					{
						catCube._pos = MousePos;
					}
				}
			}
		}

		private void JoarClick(UIfocusable trigger)
		{
			talkcounter = 40;
			speechBubble.isVisible = true;
			text.isVisible = true;
			text.text = "Fuck You.";

			kill = true;
			UnityEngine.Debug.LogException(new Joar());
		}

		public class Joar : Exception
		{
			public Joar() : base()
			{

			}
		}

		public string RandomDialogue()
		{
			List<string> t = new List<string>();

			if (!loaded) // entry dialogue
			{
				t.Add("Well well well, look at what the\nslugcat dragged in");
				t.Add("You shouldn't be here");
				t.Add("Please leave");
				t.Add("The rain is coming\nyou need to go!");
				t.Add("Back again?");
				t.Add("I dont want you here");
			}
			else
			{
				t.Add("Clippy doesn't like you");
				t.Add("Modding is dead");
				t.Add("Clippy thinks you're stupid");
				t.Add("You shouldn't be here");
				t.Add("Please leave");
				t.Add("The rain is coming\nyou need to go!");
				t.Add("Go play the game");
				t.Add("It jurts");
				t.Add("Literally anything is more\nexciting than this");
				t.Add("May God smite thee");
				t.Add("You stink");

				if (Rainbow)
				{
					t.Add("My gayness is a superpower");
					t.Add("Let me rainbow in peace");
				}

				if (Runaround)
				{
					t.Add("Get away from me");
					t.Add("Dont come near me");
				}

				if (JoarMode)
				{
					t.Add("Go press the button!");
					t.Add("Clippy thinks you\nshould press the button");
					t.Add("The joar button calls to you");
				}

				if (Echo)
				{
					t.Add("Nineteen Spades, Endless Reflections\nWould not be proud");
					t.Add("Four Needles under Plentiful Leaves\nWould not be proud");
					t.Add("Droplets upon Five Large Droplets\nWould not be proud");
					t.Add("A Bell, Eighteen Amber Beads\nWould not be proud");
					t.Add("Six Grains of Gravel, Mountains Abound\nWould not be proud");
					t.Add("Two Sprouts, Twelve Brackets\nWould not be proud");

					t.Add("Twelve Beads among Burning Skies\nWould not be proud");
					t.Add("Distant Towers upon Cracked Earth\nWould not be proud");
					t.Add("Rhinestones beneath Shattered Glass\nWould not be proud");
					t.Add("Eight Spots on a Blind Eye\nWould not be proud");
					t.Add("Four Needles under Plentiful Leaves\nWould not be proud");
					t.Add("Six Grains of Gravel, Mountains Abound\nWould not be proud");

					t.Add("Spinning Top\nWould not be proud");
					t.Add("OAOAOAOAOAOAOAOAOAOAOA");

					t.Add("I Fourteen Paperclips, Ultimate Lexicon\nAm not proud");
				}

				if (cat_Cube)
				{
					t.Add("Meow");
				}
			}

			string text = t[Mathf.RoundToInt(Random.value * (t.Count - 1))];
			return text;
		}
	}
}
