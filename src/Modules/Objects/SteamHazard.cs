namespace RegionKit.Modules.Objects;

public static class SteamObjRep
{
	internal static void SteamRep()
	{
		List<ManagedField> fields = new List<ManagedField>
		{
			new FloatField("f1", 0f, 1f, 0.5f,0.01f, ManagedFieldWithPanel.ControlType.slider, "Duration"),
			new FloatField("f2", 0f,1f,0.5f,0.01f, ManagedFieldWithPanel.ControlType.slider, "Frequency"),
			new FloatField("f3", 0f,1f,0.5f,0.01f, ManagedFieldWithPanel.ControlType.slider, "Lifetime"),
			new Vector2Field("v1", new Vector2(0f,45f), Vector2Field.VectorReprType.line)
		};
		RegisterFullyManagedObjectType(fields.ToArray(), typeof(SteamHazard), nameof(SteamHazard), RK_POM_CATEGORY);
	}
}


public class SteamHazard : UpdatableAndDeletable
{
	private readonly PlacedObject _placedObject;
	private float _durationRate;
	private float _frequencyRate;
	private float _duration;
	private float _frequency;
	private float _lifetime;
	private float _dangerRange;
	private Vector2 _fromPos;
	private Vector2 _toPos;
	private Vector2[]? _steamZone;
	private Smoke.SteamSmoke _steam;
	private RectangularDynamicSoundLoop _soundLoop;
	public SteamHazard(PlacedObject pObj, Room room)
	{
		_placedObject = pObj;
		this.room = room;
		ManagedData managedData = (_placedObject.data as ManagedData)!;
		_durationRate = managedData.GetValue<float>("f1");
		_frequencyRate = managedData.GetValue<float>("f2");
		_duration = 0f;
		_frequency = 0f;
		_lifetime = managedData.GetValue<float>("f3");
		_fromPos = _placedObject.pos;
		_toPos = managedData.GetValue<Vector2>("v1");
		_steam = new Smoke.SteamSmoke(this.room);
		_soundLoop = new RectangularDynamicSoundLoop(this, new FloatRect(_fromPos.x - 20f, _fromPos.y - 20f, _fromPos.x + 20f, _fromPos.y + 20f), this.room);
		_soundLoop.sound = SoundID.Gate_Water_Steam_LOOP;
	}

	public override void Update(bool eu)
	{
		base.Update(eu);
		ManagedData managedData = (_placedObject.data as ManagedData)!;
		_durationRate = managedData.GetValue<float>("f1");
		_frequencyRate = managedData.GetValue<float>("f2");
		_lifetime = managedData.GetValue<float>("f3");
		_dangerRange = 0.9f;
		_fromPos = _placedObject.pos;
		_toPos = managedData.GetValue<Vector2>("v1");

		//Steam burst
		if (_soundLoop != null)
		{
			if (_soundLoop.Volume > 0f)
			{
				_soundLoop.Update();
			}
			_frequency += _frequencyRate * Time.deltaTime;
			if (_frequency >= 1f)
			{
				_soundLoop.Volume = 0.6f;
				_duration += _durationRate * Time.deltaTime;
				_steam.EmitSmoke(_fromPos, _toPos * 0.15f, room.RoomRect, _lifetime);
				if (_duration >= 1f)
				{
					_duration = 0f;
					_frequency = 0f;
				}
			}
			else
			{
				_soundLoop.Volume -= 0.5f * Time.deltaTime;
				if (_soundLoop.Volume <= 0f)
				{
					_soundLoop.Stop();
				}
			}
		}

		//Creature hit by steam
		for (int i = 0; i < _steam.particles.Count; i++)
		{
			if (_steam.particles[i].life > _dangerRange)
			{
				for (int w = 0; w < room.physicalObjects.Length; w++)
				{
					for (int j = 0; j < room.physicalObjects[w].Count; j++)
					{
						for (int k = 0; k < room.physicalObjects[w][j].bodyChunks.Length; k++)
						{
							Vector2 a = room.physicalObjects[w][j].bodyChunks[k].ContactPoint.ToVector2();
							Vector2 v = room.physicalObjects[w][j].bodyChunks[k].pos + a * (room.physicalObjects[w][j].bodyChunks[k].rad + 30f);

							if (Vector2.Distance(_steam.particles[i].pos, v) < 20f)
							{
								if (room.physicalObjects[w][j] is Creature crit)
								{
									if (crit.stun == 0)
									{
										crit.stun = 100;
										room.AddObject(new CreatureSpasmer(room.physicalObjects[w][j] as Creature, false, crit.stun));
										float silentChance = room.game.cameras[0].virtualMicrophone.soundLoader.soundTriggers[(int)SoundID.Gate_Water_Steam_Puff].silentChance;
										room.game.cameras[0].virtualMicrophone.soundLoader.soundTriggers[(int)SoundID.Gate_Water_Steam_Puff].silentChance = 0f;
										room.PlaySound(SoundID.Gate_Water_Steam_Puff, crit.mainBodyChunk, false, 0.8f, 1f);
										room.PlaySound(SoundID.Big_Spider_Spit_Warning_Rustle, crit.mainBodyChunk, false, 1f, 1f);
										room.game.cameras[0].virtualMicrophone.soundLoader.soundTriggers[(int)SoundID.Gate_Water_Steam_Puff].silentChance = silentChance;
										return;
									}
								}
							}
						}
					}
				}
			}
		}
	}
}
