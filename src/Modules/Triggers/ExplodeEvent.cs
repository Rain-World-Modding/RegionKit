using DevInterface;
using Noise;
using Random = UnityEngine.Random;

namespace RegionKit.Modules.Triggers
{
	/// <summary>
	/// Makes the player explode when triggered. Meant for debug usage.
	/// </summary>
	public sealed class ExplodeEvent : TriggeredEvent, ICustomEvent
	{
		public bool DefaultMultiUse => false;

		public ExplodeEvent() : base(_Enums.ExplodeEvent)
		{
		}

		public void Fire(EventTrigger trigger, Room room)
		{
			LogInfo("POW");
			foreach (Player player in room.physicalObjects.SelectMany(x => x).OfType<Player>())
			{
				if (!player.dead && !player.isNPC && player.room == room)
				{
					ExplodePlayer(player, room);
				}
			}
		}

		private void ExplodePlayer(Player player, Room room)
		{
			Color explodeColor = player.ShortCutColor();
			Vector2 pos = Vector2.Lerp(player.firstChunk.pos, player.firstChunk.lastPos, 0.35f);
			room.AddObject(new SootMark(room, pos, 80f, true));
			room.AddObject(new Explosion(room, null, pos, 7, 250f, 6.2f, 2f, 280f, 0.25f, null, 0.7f, 160f, 1f));
			room.AddObject(new Explosion.ExplosionLight(pos, 280f, 1f, 7, explodeColor));
			room.AddObject(new Explosion.ExplosionLight(pos, 230f, 1f, 3, new Color(1f, 1f, 1f)));
			room.AddObject(new ExplosionSpikes(room, pos, 14, 30f, 9f, 7f, 170f, explodeColor));
			room.AddObject(new ShockWave(pos, 330f, 0.045f, 5, false));
			for (int i = 0; i < 25; i++)
			{
				Vector2 dir = RNV();
				if (room.GetTile(pos + dir * 20f).Solid)
				{
					if (!room.GetTile(pos - dir * 20f).Solid)
					{
						dir *= -1f;
					}
					else
					{
						dir = RNV();
					}
				}
				for (int j = 0; j < 3; j++)
				{
					room.AddObject(new Spark(pos + dir * Mathf.Lerp(30f, 60f, Random.value), dir * Mathf.Lerp(7f, 38f, Random.value) + RNV() * (20f * Random.value), Color.Lerp(explodeColor, new Color(1f, 1f, 1f), Random.value), null, 11, 28));
				}
				room.AddObject(new Explosion.FlashingSmoke(pos + dir * (40f * Random.value), dir * Mathf.Lerp(4f, 20f, Mathf.Pow(Random.value, 2f)), 1f + 0.05f * Random.value, new Color(1f, 1f, 1f), explodeColor, Random.Range(3, 11)));
			}
			room.ScreenMovement(pos, default, 1.3f);
			for (int m = 0; m < player.abstractPhysicalObject.stuckObjects.Count; m++)
			{
				player.abstractPhysicalObject.stuckObjects[m].Deactivate();
			}
			room.PlaySound(SoundID.Bomb_Explode, pos, player.abstractPhysicalObject);
			room.InGameNoise(new InGameNoise(pos, 9000f, player, 1f));
		}

		public StandardEventPanel? InitDevUIPanel(TriggerPanel triggerPanel)
		{
			return null;
		}
	}
}
