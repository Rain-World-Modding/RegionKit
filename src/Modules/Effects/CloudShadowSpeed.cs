using System.Globalization;
using DevInterface;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RegionKit.Modules.DevUIMisc.GenericNodes;

namespace RegionKit.Modules.Effects
{
	public static class CloudShadowSpeed
	{
		private static bool ApplyEffects = false;

		internal static void Apply()
		{
			_CommonHooks.PostRoomLoad += _CommonHooks_PostRoomLoad;
			IL.RoomCamera.DrawUpdate += RoomCamera_DrawUpdate;
			On.DevInterface.EffectPanel.ctor += EffectPanel_ctor;
		}

		internal static void Undo()
		{
			ApplyEffects = false;
			_CommonHooks.PostRoomLoad -= _CommonHooks_PostRoomLoad;
			IL.RoomCamera.DrawUpdate -= RoomCamera_DrawUpdate;
			On.DevInterface.EffectPanel.ctor -= EffectPanel_ctor;
		}

		private static void _CommonHooks_PostRoomLoad(Room room)
		{
			if (room.game == null) return;
			if (room.roomSettings.effects.Any(x => x.type == _Enums.CloudsShadowSpeed))
			{
				ApplyEffects = true;
			}
		}

		private static void RoomCamera_DrawUpdate(ILContext il)
		{
			var c = new ILCursor(il);

			c.GotoNext(x => x.MatchLdsfld<RainWorld>(nameof(RainWorld.ShadPropCloudsSpeed)));
			c.GotoNext(MoveType.After, x => x.MatchLdcR4(1f));

			c.Emit(OpCodes.Ldarg_0);
			c.EmitDelegate((float speed, RoomCamera self) =>
			{
				if (ApplyEffects)
				{
					return self.room.roomSettings.GetEffect(_Enums.CloudsShadowSpeed)?.amount ?? speed;
				}
				return speed;
			});
		}

		private static void EffectPanel_ctor(On.DevInterface.EffectPanel.orig_ctor orig, EffectPanel self, DevUI owner, DevUINode parentNode, Vector2 pos, RoomSettings.RoomEffect effect)
		{
			orig(self, owner, parentNode, pos, effect);
			if (effect.type == _Enums.CloudsShadowSpeed)
			{
				ApplyEffects = true;
				self.subNodes[^1].ClearSprites();
				self.size.y += 20f;
				self.subNodes[^1] = new CloudShadowSpeedSlider(owner, "Speed_Slider", self, new Vector2(5f, 5f), "Speed:", 240f, 110f, -3f, 3f, effect.amount);
			}
		}

		private sealed class CloudShadowSpeedSlider : ExtendedSlider
		{
			public RoomSettings.RoomEffect effect => (parentNode as EffectPanel)!.effect;

			public override string HandleText => $"{title} ({Value:P1})";

			private float lastValue;

			public CloudShadowSpeedSlider(DevUI owner, string IDstring, EffectPanel parentNode, Vector2 pos, string title, float width, float titleWidth, float leftValue, float rightValue, float initValue) 
				: base(owner, IDstring, parentNode, pos, title, width, titleWidth, leftValue, rightValue, initValue, false, false, parentNode.effect.inherited)
			{
				lastValue = handle.value;
			}

			public override void Update()
			{
				base.Update();
				if (lastValue != handle.value)
				{
					if (numberInput != null && ManagedStringControl.activeStringControl != numberInput)
					{
						numberInput.Text = numberInput.actualValue = Value.ToString(CultureInfo.InvariantCulture);
					}
					Refresh();
				}
				lastValue = handle.value;
				if (!effect.inherited)
				{
					effect.amount = Value;
				}
			}

			public override void Refresh()
			{
				base.Refresh();
			}
		}
	}
}
