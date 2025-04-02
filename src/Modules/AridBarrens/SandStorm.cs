using System;
using System.Collections.Generic;
using RWCustom;
using UnityEngine;

namespace RegionKit.Modules.AridBarrens;
/// <summary>
/// SandStorm scene. Add for sandstorm GO
/// </summary>
public class SandStorm : BackgroundScene, INotifyWhenRoomIsReady
{
	///<inheritdoc/>
	public SandStorm(RoomSettings.RoomEffect effect, Room room) : base(room)
	{
		_deathtimer = 0;
		this._effect = effect;
		this.sceneOrigo = new(2514f, 26000);
		this._generalFog = new(this);
		this.AddElement(this._generalFog);
		this._rainReach = new int[room.TileWidth];
		for (int i = 0; i < room.TileWidth; i++)
		{
			bool flag = true;
			for (int j = room.TileHeight - 1; j >= 0; j--)
			{
				if (flag && room.GetTile(i, j).Solid)
				{
					flag = false;
					this._rainReach[i] = j;
				}
			}
		}
		this._particles = new List<SandPart>();
		float num = Mathf.Lerp(0f, 0.6f, effect.amount);
		this._totParticles = Custom.IntClamp((int)((float)(room.TileWidth * room.TileHeight) * num), 1, 300);
	}
	private RainCycle _Cycle
	{
		get
		{
			return this.room.game.world.rainCycle;
		}
	}
	private float _Intensity
	{
		get
		{
			return this._effect.amount * Mathf.Pow(Mathf.InverseLerp((float)(this._Cycle.cycleLength - 400), (float)(this._Cycle.cycleLength + 2400), (float)this._Cycle.timer), 2.2f);
		}
	}
	///<inheritdoc/>
	public override void AddElement(BackgroundScene.BackgroundSceneElement element)
	{
		base.AddElement(element);
	}
	///<inheritdoc/>
	public override void Update(bool eu)
	{
		base.Update(eu);
		if (this._Intensity == 0f)
		{
			return;
		}
		for (int i = this._particles.Count - 1; i >= 0; i--)
		{
			if (this._particles[i].slatedForDeletetion)
			{
				this._particles.RemoveAt(i);
			}
			else
			{
				this._particles[i].vel += this._wind * 0.2f;
			}
		}
		if (this._particles.Count < this._totParticles * Mathf.Pow(this._Intensity, 0.2f))
		{
			this.AddSpark();
		}
		this._wind += Custom.RNV() * 0.1f;
		this._wind *= 0.98f;
		this._wind = Vector2.ClampMagnitude(this._wind, 1f);
		if (this._soundLoop == null)
		{
			this._soundLoop = new DisembodiedDynamicSoundLoop(this);
			this._soundLoop.sound = SoundID.Void_Sea_Worm_Swimby_Woosh_LOOP;
			this._soundLoop.Volume = 0f;
		}
		else
		{
			this._soundLoop.Update();
			this._soundLoop.Volume = Mathf.Pow(this._Intensity, 0.5f);
		}
		if (this._soundLoop2 == null)
		{
			this._soundLoop2 = new DisembodiedDynamicSoundLoop(this);
			this._soundLoop2.sound = SoundID.Gate_Electric_Steam_LOOP;
			this._soundLoop2.Volume = 0f;
		}
		else
		{
			this._soundLoop2.Update();
			this._soundLoop2.Volume = Mathf.Pow(this._Intensity, 0.1f) * Mathf.Lerp(0.5f + 0.5f * Mathf.Sin(this._sin * 3.14159274f * 2f), 0f, Mathf.Pow(this._Intensity, 8f));
		}

		this._sin += 0.002f;
		if (this._closeToWallTiles != null && this.room.BeingViewed && UnityEngine.Random.value < Mathf.InverseLerp(1000f, 9120f, (float)(this.room.TileWidth * this.room.TileHeight)) * 2f * Mathf.Pow(this._Intensity, 0.3f))
		{
			IntVector2 pos = this._closeToWallTiles[UnityEngine.Random.Range(0, this._closeToWallTiles.Count)];
			Vector2 pos2 = this.room.MiddleOfTile(pos) + new Vector2(Mathf.Lerp(-10f, 10f, UnityEngine.Random.value), Mathf.Lerp(-10f, 10f, UnityEngine.Random.value));
			float num = UnityEngine.Random.value * this._Intensity;
			if (this.room.ViewedByAnyCamera(pos2, 50f))
			{
				this.room.AddObject(new SandPuff(pos2, num));
			}
		}
		if (this._Intensity > 0.1)
		{
			ThrowAroundObjects();
		}

		if (this._Intensity > 0.99f && _killedCreatures == false)
		{
			_deathtimer++;
			if (_deathtimer > 500)
			{
				_deathtimer = 450;
				for (int j = 0; j < this.room.physicalObjects.Length; j++)
				{
					for (int k = 0; k < this.room.physicalObjects[j].Count; k++)
					{
						if (this.room.physicalObjects[j][k] is Creature crit)
						{
							if (!crit.dead)
							{
								crit.Violence(null, null, this.room.physicalObjects[j][k].bodyChunks[0], null, Creature.DamageType.Blunt, 1.8f, 40f);
								if ((this.room.physicalObjects[j][k] as Creature) is Player)
								{
									_killedCreatures = true;
								}
							}
							else if ((this.room.physicalObjects[j][k] as Creature) is Player)
							{
								_killedCreatures = true;
							}
						}
					}
				}
			}
		}
	}

	private void AddSpark()
	{
		IntVector2 pos = new IntVector2(0, 0);
		if (UnityEngine.Random.value < (float)this.room.TileHeight / (float)this.room.TileWidth)
		{
			pos = new IntVector2(0, UnityEngine.Random.Range(0, this.room.TileHeight));
		}
		else
		{
			pos = new IntVector2(UnityEngine.Random.Range(0, this.room.TileWidth), 0);
		}
		if (!this.room.GetTile(pos).Solid)
		{
			Vector2 vector = this.room.MiddleOfTile(pos);
			int num = 0;
			while (num < 10 && this.room.ViewedByAnyCamera(vector, 200f))
			{
				vector += Custom.DirVec(this.room.RoomRect.Center, vector) * 100f;
				num++;
			}
			SandPart particle = new SandPart(vector);
			this.room.AddObject(particle);
			this._particles.Add(particle);
		}
	}

	private float InsidePushAround
	{
		get
		{
			return this._effect.amount * _Intensity;
		}
	}

	private void ThrowAroundObjects()
	{
		if (this._Intensity == 0f)
		{
			return;
		}
		for (int i = 0; i < this.room.physicalObjects.Length; i++)
		{
			for (int j = 0; j < this.room.physicalObjects[i].Count; j++)
			{
				for (int k = 0; k < this.room.physicalObjects[i][j].bodyChunks.Length; k++)
				{
					BodyChunk bodyChunk = this.room.physicalObjects[i][j].bodyChunks[k];
					IntVector2 tilePosition = this.room.GetTilePosition(bodyChunk.pos + new Vector2(Mathf.Lerp(-bodyChunk.rad, bodyChunk.rad, UnityEngine.Random.value), Mathf.Lerp(-bodyChunk.rad, bodyChunk.rad, UnityEngine.Random.value)));
					float num = this.InsidePushAround;
					//bool flag = false;
					if (this._rainReach[Custom.IntClamp(tilePosition.x, 0, this.room.TileWidth - 1)] < tilePosition.y)
					{
						//flag = true;
						num = Mathf.Max(_Intensity, this.InsidePushAround);
					}
					if (this.room.water)
					{
						num *= Mathf.InverseLerp(this.room.FloatWaterLevel(bodyChunk.pos) - 100f, this.room.FloatWaterLevel(bodyChunk.pos), bodyChunk.pos.y);
					}
					if (num > 0f)
					{
						bodyChunk.vel += Custom.DegToVec(Mathf.Lerp(0f, 360f, UnityEngine.Random.value)) * UnityEngine.Random.value * 6.5f * this.InsidePushAround;
					}
				}
			}
		}
	}
	///<inheritdoc/>
	public void ShortcutsReady()
	{
		_killedCreatures = false;
	}
	/// <summary>
	/// Populates _closeToWallTiles
	/// </summary>
	public void AIMapReady()
	{
		this._closeToWallTiles = new List<IntVector2>();
		for (int i = 0; i < this.room.TileWidth; i++)
		{
			for (int j = 0; j < this.room.TileHeight; j++)
			{
				if (this.room.aimap.getTerrainProximity(i, j) == 1)
				{
					this._closeToWallTiles.Add(new IntVector2(i, j));
				}
			}
		}
	}

	private RoomSettings.RoomEffect _effect;

	private List<IntVector2>? _closeToWallTiles;

	private bool _killedCreatures = true;

	private float _sin;

	private int[] _rainReach;

	private int _deathtimer = 0;

	private List<SandPart> _particles;

	private int _totParticles;

	private Vector2 _wind;

	private DisembodiedDynamicSoundLoop? _soundLoop;

	private DisembodiedDynamicSoundLoop? _soundLoop2;
	private SandStorm.Fog _generalFog;
	private class Fog : BackgroundScene.FullScreenSingleColor
	{
		public Fog(SandStorm sandStormScene) : base(sandStormScene, default(Color), 1f, true, float.MaxValue)
		{
			this.depth = 0f;
		}
		private float _Intensity
		{
			get
			{
				return (this.scene as SandStorm)!._Intensity;
			}
		}
		private SandStorm _SandStormScene
		{
			get
			{
				return (this.scene as SandStorm)!;
			}
		}
		///<inheritdoc/>
		public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
		{
			sLeaser.sprites = new FSprite[1];
			sLeaser.sprites[0] = new FSprite("pixel", true);
			sLeaser.sprites[0].scaleX = (rCam.game.rainWorld.screenSize.x + 20f) / 1f;
			sLeaser.sprites[0].scaleY = (rCam.game.rainWorld.screenSize.y + 20f) / 1f;
			sLeaser.sprites[0].x = rCam.game.rainWorld.screenSize.x / 2f;
			sLeaser.sprites[0].y = rCam.game.rainWorld.screenSize.y / 2f;
			//sLeaser.sprites[0].shader = rCam.game.rainWorld.Shaders["Background"];
			sLeaser.sprites[0].color = this.color;
			sLeaser.sprites[0].alpha = this.alpha;
			this.AddToContainer(sLeaser, rCam, null);
		}
		///<inheritdoc/>
		public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
		{
			this.alpha = _Intensity / 1.1f;
			base.DrawSprites(sLeaser, rCam, timeStacker, camPos);
		}
		///<inheritdoc/>
		public override void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
		{
			this.color = palette.skyColor;
			base.ApplyPalette(sLeaser, rCam, palette);
		}
	}
}

