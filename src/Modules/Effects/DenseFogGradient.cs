using Random = UnityEngine.Random;

namespace RegionKit.Modules.Effects;

public class DenseFogGradient : CosmeticSprite
{
	int _danger;

	public DenseFogGradient(Room room) => this.room = room;

	public override void Update(bool eu)
	{
		base.Update(eu);
		if (room is Room rm)
		{
			float intensity = GetDenseFogIntensity(room);

			// Sound volume
			FloatRect roomRect = rm.RoomRect;
			float volumeEffectAmount = rm.roomSettings.GetEffectAmount(_Enums.DenseFogSoundVolume);
			float fogSoundVolume = (rm.roomSettings.GetEffect(_Enums.DenseFogSoundVolume) is null) ? intensity : volumeEffectAmount * ((float)rm.world.rainCycle.timer / rm.world.rainCycle.cycleLength);
			float fogSoundVolume2 = Mathf.Max(0f, fogSoundVolume - .5f);

			// Spooky sounds
			rm.PlayRectSound(SoundID.Coral_Circuit_Break, roomRect, false, fogSoundVolume, Random.value / 2f);
			rm.PlayRectSound(SoundID.Reds_Illness_LOOP, roomRect, false, fogSoundVolume, Random.value / 2f);
			rm.PlayRectSound(SoundID.Distant_Deer_Summoned, roomRect, false, fogSoundVolume, Random.value / 2f);
			rm.PlayRectSound(SoundID.Coral_Circuit_Jump_Explosion, roomRect, false, fogSoundVolume2, Random.value / 2f);
			rm.PlayRectSound(SoundID.Death_Lightning_Spark_Spontaneous, roomRect, false, fogSoundVolume2, Random.value / 2f);
			//Debug.Log(s0.alpha);
			//Debug.Log(fogSoundVolume);
			//Debug.Log(room.world.rainCycle.timer);

			// Danger + fog demon sound
			if (rm.roomSettings.GetEffectAmount(_Enums.DenseFogSoundVolume) <= 0f)
				_danger = 2;
			if (_danger == 0 && intensity >= .8f)
				++_danger;
			if (_danger == 1 && intensity < .99f)
			{
				room.PlayRectSound(_Enums.FT_Fog_PreDeath, roomRect, false, .4f + volumeEffectAmount * .4f, 1f);
				++_danger;
			}

			// Fog demon kill
			if (intensity >= .99f)
			{
				List<AbstractCreature> crits = rm.abstractRoom.creatures;
				for (var i = 0; i < crits.Count; i++)
				{
					if (crits[i].realizedCreature is Player p && !p.dead)
						p.Die();
				}
			}
		}
	}

	public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
	{
		base.InitiateSprites(sLeaser, rCam);
		sLeaser.sprites = [ new FSprite("pixel") { scaleX = 1500f, scaleY = 900f, anchorX = 0f, anchorY = 0f } ];
		AddToContainer(sLeaser, rCam, rCam.ReturnFContainer("GrabShaders"));
	}

	public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
	{
		if (room is not Room rm) 
			return;
		FSprite s0 = sLeaser.sprites[0];
		s0.y = s0.x = 0f;
		s0.alpha = GetDenseFogIntensity(rm);
		base.DrawSprites(sLeaser, rCam, timeStacker, camPos);
	}

	public override void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
	{
		base.ApplyPalette(sLeaser, rCam, palette);
		sLeaser.sprites[0].color = palette.fogColor;
	}
	
	static float GetDenseFogIntensity(Room rm)
	{
		float effectAmount = rm.roomSettings.GetEffectAmount(_Enums.DenseFog),
			cycleProgress = (float)rm.world.rainCycle.timer / rm.world.rainCycle.cycleLength;
		//Debug.Log(cycleProgress);
		var intensity = Mathf.Exp((cycleProgress * 3f) - 3f);
		intensity /= ((1f - effectAmount) * .3f) + 1;
		intensity = Mathf.Clamp(intensity, 0f, Math.Min(effectAmount * 3f, 1f));
		return intensity;
	}
}
