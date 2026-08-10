using EffExt;
using Random = UnityEngine.Random;

namespace RegionKit.Modules.Effects
{
	/// <summary>
	/// Rumbles effect by Damoonlord
	/// </summary>
	public class Rumbles : UpdatableAndDeletable
	{
		internal static void __RegisterBuilder()
		{
			new EffectDefinitionBuilder(_Enums.Rumbles.value)
				.SetUADFactory((room, data, firstTimeRealized) => new Rumbles(data))
				.SetCategory(_Enums.RegionKit_Gameplay.value)
				.Register();
		}

		public EffectExtraData Data { get; }

		public int Duration;

		public int Delay;

		public float Amount;

		public DisembodiedDynamicSoundLoop shakesound;

		public Rumbles(EffectExtraData effectData) : base()
		{
			Data = effectData;
			Delay = (int)Random.Range(122f, 500f);

			shakesound = new DisembodiedDynamicSoundLoop(this);
			shakesound.sound = SoundID.Screen_Shake_LOOP;
		}

		public override void Update(bool eu)
		{
			base.Update(eu);

			if (Duration > 0)
			{
				Duration--;
				room.game.cameras[0].screenShake = Amount;
				shakesound.Volume = Amount * 1.4444f;

				if (0.25f + Data.Amount * 3f > 1f && room.ceilingTiles.Length > 0)
				{
					for (int num = Mathf.Min(40, Mathf.RoundToInt(Mathf.Lerp(room.ceilingTiles.Length, 150f, 0.25f) / 100f * 0.25f * Mathf.Clamp(0.25f + Amount * 2f, 2f, 8f))); num > 0; num--)
					{
						Vector2 pos2 = room.MiddleOfTile(room.ceilingTiles[UnityEngine.Random.Range(0, room.ceilingTiles.Length)]) + new Vector2(Mathf.Lerp(-10f, 10f, UnityEngine.Random.value), 9f);
						if (room.ViewedByAnyCamera(pos2, 300f))
						{
							room.AddObject(new WaterDrip(pos2, new Vector2(Mathf.Lerp(-1.5f, 1.5f, UnityEngine.Random.value), 0f), waterColor: false));
						}
					}

					foreach (AbstractCreature Acrit in room.abstractRoom.creatures)
					{
						if (Acrit.abstractAI != null)
						{
							Acrit.abstractAI.GoToDen();
						}
					}
				}
			}
			else
			{
				shakesound.Volume = 0f;
			}

			shakesound.Update();

			if (Duration > 0 && Delay == 0)
			{
				Delay = (int)Random.Range(122f, 500f);
			}
			else if (Delay > 0 && Duration == 0)
			{
				Delay--;
			}

			if (Delay == 0 && Duration == 0 && Random.value < 0.01f)
			{
				Duration = (int)Random.Range(75f, 222f);
				Amount = Random.Range(Data.Amount * 0.0675f, Data.Amount * 0.15f);
			}
		}
	}
}
