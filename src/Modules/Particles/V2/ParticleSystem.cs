namespace RegionKit.Modules.Particles.V2;

public class ParticleSystem : UpdatableAndDeletable, IDrawable
{
	public static event ParticleMover OnParticleUpdate;
	private const int _POOL_PADDING = 32;
	private int _spawnCooldown;
	private bool _initDone;
	private int _capacity;
	private (ParticleState, ParticleVisualState)[] _particlePool;
	private readonly HashSet<int> _indicesBuffer = new();
	private readonly HashSet<int> _activeParticles = new();
	//private IEnumerable<int> _incomingIndex;
	private Queue<int> _upcomingIndices = new();
	private IEnumerable<IntVector2>? _tilesShuffledLoop;
	private readonly List<IntVector2> _suitableTiles = new();
	private readonly List<IParticleZone> _zones = new();
	private readonly List<IParticleVisualProvider> _visProvs = new();
	private readonly PlacedObject _owner;
	private ParticleSystemData _Data => (ParticleSystemData)_owner.data;
	public ParticleSystem(PlacedObject owner, Room rm)
	{
		//_particlePool = new (ParticleState, ParticleVisualState)[0];
		//_incomingIndex = new int[0];
		_owner = owner;
		this.room = rm;
		CollectRelated();
		CalculateCapacity();
	}
	public override void Update(bool eu)
	{
		base.Update(eu);
		_indicesBuffer.Clear();
		if (!_initDone)
		{
		}
		_initDone = true;
		foreach (int index in _activeParticles)
		{
			ref (ParticleState, ParticleVisualState) part = ref _particlePool[index];
			part.Item1.Update();
			if (part.Item1.stateChangeSlated < 0)
			{
				_indicesBuffer.Add(index);
			}
		}
		foreach (int buffered in _indicesBuffer)
		{
			_activeParticles.Remove(buffered);
			_upcomingIndices.Enqueue(buffered);
		}
		_indicesBuffer.Clear();
		_spawnCooldown--;
		if (_spawnCooldown < 0)
		{
			_spawnCooldown = RNG.Range(_Data.minCooldown, _Data.maxCooldown);
			int tospawn = RNG.Range(_Data.minAmount, _Data.maxAmount);
			for (int i = 0; i < tospawn; i++)
			{
				__logger.LogDebug("u" + i);
				if (!_upcomingIndices.TryDequeue(out int index))
				{
					__logger.LogError($"PARTICLE SPAWNER IN ROOM {room.abstractRoom.name}: PARTICLE POOL EXCEEDED CAPACITY");
					break;
				}
				Vector2 pos = (_suitableTiles.RandomOrDefault().ToVector2() + new Vector2(RNG.value, RNG.value)) * 20f;
				_particlePool[index] = (_Data.StateForNew(index, pos), _visProvs.RandomOrDefault()?.StateForNew() ?? IParticleVisualProvider.PlaceholderProv.instance.StateForNew());
				_activeParticles.Add(index);
			}
		}
	}
	private void CollectRelated()
	{
		foreach (PlacedObject po in room.roomSettings.placedObjects)
		{
			if (po.data is IParticleZone zone)
			{
				//todo: add filters
				_zones.Add(zone);
				_suitableTiles.AddRange(zone.SelectedTiles);
			}
			if (po is IParticleVisualProvider vis && (vis.Owner.pos - _Data.owner.pos).sqrMagnitude <= vis.P2.sqrMagnitude)
			{
				_visProvs.Add(vis);
			}
		}
		if (_visProvs.Count is 0)
		{
			_visProvs.Add(IParticleVisualProvider.PlaceholderProv.instance);
		}
		__logger.LogDebug($"particle system in room {room.abstractRoom.name} collected {_zones.Count} zones ({_Data.groupTags}) with {_suitableTiles.Count} total tiles");
	}

	private void CalculateCapacity()
	{
		//uhhb so ok like
		//todo: see if math is correct
		float expectancy = _Data.lifeTime + _Data.lifeTimeFluke + _Data.fadeIn + _Data.fadeInFluke + _Data.fadeOut + _Data.fadeOutFluke;
		float birthsPerFrame = (float)(_Data.maxAmount) / _Data.minCooldown;
		float sum = 0f, remExpectancy = expectancy;
		while (remExpectancy > 0)
		{
			sum += birthsPerFrame * remExpectancy;
			remExpectancy -= 1f;
		}
		_particlePool = new (ParticleState, ParticleVisualState)[(int)sum + _POOL_PADDING]; //extra just in case
		_upcomingIndices.EnqueueSeveral(Indices(_particlePool));
		__logger.LogDebug($"particle system in room {room.abstractRoom.name} created a buffer of length {_particlePool.Length}");
	}

	public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer? newContatiner)
	{
		FSprite[] sprites = sLeaser.sprites;
		FContainer
			fc_water = rCam.ReturnFContainer(ContainerCodes.Water.ToString()),
			fc_fglum = rCam.ReturnFContainer(ContainerCodes.ForegroundLights.ToString());
		for (int i = 1; i < _particlePool.Length; i++)
		{
			//todo: add sprites

		}
	}

	public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
	{


	}

	public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
	{
		bool reAddSpritesSlated = false;

		foreach (int index in Indices(_particlePool))
		{
			ref var current = ref _particlePool[index];
			ref var move = ref current.Item1;
			ref var vis = ref current.Item2;
			if (move.lastPos is null || move.lastRot is null)
			{
				continue;
			}
			FSprite
				partsprite = sLeaser.sprites[index * 2],
				lightsprite = sLeaser.sprites[index * 2 + 1];

			if (move.stateChangeSlated < 0)
			{
				partsprite.isVisible = false;
				lightsprite.isVisible = false;
				continue;
			}
			else if (move.stateChangeSlated > 0)
			{
				partsprite.isVisible = true;
				lightsprite.isVisible = true;
				reAddSpritesSlated = true;
				partsprite.element = Futile.atlasManager.GetElementWithName(vis.atlasElement);
				partsprite.scaleX = partsprite.scaleY = vis.scale;
				partsprite.shader = rCam.game.rainWorld.Shaders[vis.shader];
				lightsprite.shader = rCam.game.rainWorld.Shaders[vis.flat ? "FlatLight" : "LightSource"];
				continue;
			}

			Vector2
				posnew = move.pos + camPos,
				posold = move.lastPos[0] + camPos;
			float
				rotnew = move.rot,
				rotold = move.lastRot[0];
			partsprite.x = Lerp(posold.x, posnew.x, timeStacker);
			partsprite.y = Lerp(posold.y, posnew.y, timeStacker);
			lightsprite.x = Lerp(posold.x, posnew.x, timeStacker);
			lightsprite.y = Lerp(posold.y, posnew.y, timeStacker);
			//todo:
			partsprite.alpha = move.CurrentPower;
		}
		if (reAddSpritesSlated) this.AddToContainer(sLeaser, rCam, null);
	}

	public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
	{
		FSprite[] sprites = new FSprite[_particlePool.Length * 2];
		for (int i = 0; i < _particlePool.Length; i++)
		{
			sprites[i * 2] = new("SkyDandelion", false);
			sprites[i * 2 + 1] = new("Futile_White")
			{
				shader = rCam.game.rainWorld.Shaders["LightSource"]
			};
		}
		sLeaser.sprites = sprites;
		AddToContainer(sLeaser, rCam, null);
	}

	public delegate void ParticleMover(ref ParticleState moveState, ref ParticleVisualState visState);
}
