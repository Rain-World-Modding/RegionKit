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
		RegisterFullyManagedObjectType(fields.ToArray(), typeof(SteamHazard));
	}
}


public class SteamHazard : UpdatableAndDeletable
{
	public PlacedObject placedObject;
	public float durationRate;
	public float frequencyRate;
	public float duration;
	public float frequency;
	public float lifetime;
	public float dangerRange;
	public Vector2 fromPos;
	public Vector2 toPos;
	public Vector2[] steamZone;
	public Smoke.SteamSmoke steam;
	public RectangularDynamicSoundLoop soundLoop;
	public SteamHazard(PlacedObject pObj, Room room)
	{
		placedObject = pObj;
		this.room = room;
		durationRate = (placedObject.data as ManagedData).GetValue<float>("f1");
		frequencyRate = (placedObject.data as ManagedData).GetValue<float>("f2");
		duration = 0f;
		frequency = 0f;
		lifetime = (placedObject.data as ManagedData).GetValue<float>("f3");
		fromPos = placedObject.pos;
		toPos = (placedObject.data as ManagedData).GetValue<Vector2>("v1");
		steam = new Smoke.SteamSmoke(this.room);
		soundLoop = new RectangularDynamicSoundLoop(this, new FloatRect(fromPos.x - 20f, fromPos.y - 20f, fromPos.x + 20f, fromPos.y + 20f), this.room);
		soundLoop.sound = SoundID.Gate_Water_Steam_LOOP;
	}

	public override void Update(bool eu)
	{
		base.Update(eu);
		durationRate = (placedObject.data as ManagedData).GetValue<float>("f1");
		frequencyRate = (placedObject.data as ManagedData).GetValue<float>("f2");
		lifetime = (placedObject.data as ManagedData).GetValue<float>("f3");
		dangerRange = 0.9f;
		fromPos = placedObject.pos;
		toPos = (placedObject.data as ManagedData).GetValue<Vector2>("v1");

		//Steam burst
		if (soundLoop != null)
		{
			if (soundLoop.Volume > 0f)
			{
				soundLoop.Update();
			}
			frequency += frequencyRate * Time.deltaTime;
			if (frequency >= 1f)
			{
				soundLoop.Volume = 0.6f;
				duration += durationRate * Time.deltaTime;
				steam.EmitSmoke(fromPos, toPos * 0.15f, room.RoomRect, lifetime);
				if (duration >= 1f)
				{
					duration = 0f;
					frequency = 0f;
				}
			}
			else
			{
				soundLoop.Volume -= 0.5f * Time.deltaTime;
				if (soundLoop.Volume <= 0f)
				{
					soundLoop.Stop();
				}
			}
		}

		//Creature hit by steam
		for (int i = 0; i < steam.particles.Count; i++)
		{
			if (steam.particles[i].life > dangerRange)
			{
				for (int w = 0; w < room.physicalObjects.Length; w++)
				{
					for (int j = 0; j < room.physicalObjects[w].Count; j++)
					{
						for (int k = 0; k < room.physicalObjects[w][j].bodyChunks.Length; k++)
						{
							Vector2 a = room.physicalObjects[w][j].bodyChunks[k].ContactPoint.ToVector2();
							Vector2 v = room.physicalObjects[w][j].bodyChunks[k].pos + a * (room.physicalObjects[w][j].bodyChunks[k].rad + 30f);

							if (Vector2.Distance(steam.particles[i].pos, v) < 20f)
							{
								if (room.physicalObjects[w][j] is Creature)
								{
									if ((room.physicalObjects[w][j] as Creature).stun == 0)
									{
										(room.physicalObjects[w][j] as Creature).stun = 100;
										room.AddObject(new CreatureSpasmer(room.physicalObjects[w][j] as Creature, false, (room.physicalObjects[w][j] as Creature).stun));
										float silentChance = room.game.cameras[0].virtualMicrophone.soundLoader.soundTriggers[(int)SoundID.Gate_Water_Steam_Puff].silentChance;
										room.game.cameras[0].virtualMicrophone.soundLoader.soundTriggers[(int)SoundID.Gate_Water_Steam_Puff].silentChance = 0f;
										room.PlaySound(SoundID.Gate_Water_Steam_Puff, (room.physicalObjects[w][j] as Creature).mainBodyChunk, false, 0.8f, 1f);
										room.PlaySound(SoundID.Big_Spider_Spit_Warning_Rustle, (room.physicalObjects[w][j] as Creature).mainBodyChunk, false, 1f, 1f);
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
