namespace RegionKit.Modules.Particles.V2;

public class ParticleSystem : UpdatableAndDeletable, IDrawable
{
	private bool _initDone;
	private int _capacity;
	private (ParticleState, ParticleVisualState)[] _particlePool;
	private readonly List<int> _activeParticles = new();
	private IEnumerable<int> _incomingIndex;
	private IEnumerable<IntVector2>? _tilesShuffledLoop;
	private readonly List<IntVector2> _suitableTiles = new();
	private readonly List<IParticleZone> _zones = new();
	private readonly PlacedObject _owner;
	private ParticleSystemData _Data => (_owner.data as ParticleSystemData)!;
	public ParticleSystem(PlacedObject owner, Room rm)
	{
		_particlePool = new (ParticleState, ParticleVisualState)[0];
		_incomingIndex = new int[0];
		_owner = owner;
		this.room = rm;
		CollectZones();
		CalculateCapacity();
	}
	public override void Update(bool eu)
	{
		base.Update(eu);
		if (!_initDone)
		{

		}
		_initDone = true;
	}
	private void CollectZones()
	{
		foreach (PlacedObject po in room.roomSettings.placedObjects)
			if (po.data is IParticleZone zone)
			{
				_zones.Add(zone);
				_suitableTiles.AddRange(zone.SelectedTiles);
			}
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
		_particlePool = new (ParticleState, ParticleVisualState)[(int)sum * 2 + 32]; //extra just in case
	}

	public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer? newContatiner)
	{
		FSprite[] sprites = sLeaser.sprites;
		FContainer 
			fc_water = rCam.ReturnFContainer(ContainerCodes.Water.ToString()),
			fc_fglum = rCam.ReturnFContainer(ContainerCodes.ForegroundLights.ToString());
		for (int i = 1; i < sprites.Length; i += 2)
		{
			
		}
	}

	public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
	{
		

	}

	public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
	{
		for (int i = 0; i < _activeParticles.Count; i++)
		{
			int index = _activeParticles[i];
			(var move, var vis) = _particlePool[index];
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
				continue;
			}
			partsprite.element = Futile.atlasManager.GetElementWithName(vis.atlasElement);
			partsprite.shader = rCam.game.rainWorld.Shaders[vis.shader];
			lightsprite.shader = rCam.game.rainWorld.Shaders[vis.flat ? "FlatLight" : "LightSource"];
		}
	}

	public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
	{
		FSprite[] sprites = new FSprite[_particlePool.Length];
		for (int i = 1; i < sprites.Length; i += 2)
		{
			sprites[i - 1] = new("SkyDandelion", false);
			sprites[i] = new("Futile_White")
			{
				shader = rCam.game.rainWorld.Shaders["LightSource"]
			};
		}
		sLeaser.sprites = sprites;
		AddToContainer(sLeaser, rCam, null);
	}
}
