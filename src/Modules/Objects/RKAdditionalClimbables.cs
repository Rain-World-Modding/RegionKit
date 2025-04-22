using DevInterface;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace RegionKit.Modules.Objects;

internal static class RKAdditionalClimbables
{
	internal static void Apply()
    {
		On.MoreSlugcats.ClimbableVineRenderer.AIMapReady += ClimbableVineRendererAIMapReady;
		IL.Player.UpdateAnimation += PlayerUpdateAnimation;
		IL.Player.Jump += PlayerJump;
		IL.Player.MovementUpdate += PlayerMovementUpdate;
		IL.MoreSlugcats.Yeek.Update += Yeek_Update;
		On.Player.MovementUpdate += Player_MovementUpdate;
    }

	private static void Player_MovementUpdate(On.Player.orig_MovementUpdate orig, Player self, bool eu)
	{
		//if the player isn't holding a vine anymore, remove their vinepos
		//this is critical for JumpAllowed to work, particularly in Yeek.Update where AnimationIndex has been reset already
		if (self.animation != Player.AnimationIndex.VineGrab)
			self.vinePos = null;
		orig(self, eu);
	}

	private static void Yeek_Update(ILContext il)
	{
		int count = 0;
		var c = new ILCursor(il);
		while (c.TryGotoNext(MoveType.After,
			x => x.MatchLdloc(0),
			x => x.MatchLdfld<Player>("bodyMode"),
			x => x.MatchLdsfld<Player.BodyModeIndex>("ClimbingOnBeam"),
			x => x.MatchCall(out _)))
		{
			c.Emit(OpCodes.Ldarg_0);
			c.EmitDelegate((bool orig, MoreSlugcats.Yeek yeek) => JumpAllowed(orig, (yeek.grabbedBy[0].grabber as Player)!));
			count++;
		}
		if (count == 0)
			LogError("Couldn't ILHook Yeek.Update!"); 
		else if (count != 3)
			LogError("Couldn't ILHook Yeek.Update!");
	}

	private static bool JumpAllowed(bool flag, Player player) => flag || player.room?.climbableVines is ClimbableVinesSystem sys
		&& player.vinePos is ClimbableVinesSystem.VinePosition vPos
		&& sys.vines.Count > 0 && sys.GetVineObject(vPos) is IClimbJumpVine v && v.JumpAllowed();

	internal static void Undo()
	{
		On.MoreSlugcats.ClimbableVineRenderer.AIMapReady -= ClimbableVineRendererAIMapReady;
		IL.Player.UpdateAnimation -= PlayerUpdateAnimation;
		IL.Player.Jump -= PlayerJump;
		IL.Player.MovementUpdate -= PlayerMovementUpdate;
		IL.MoreSlugcats.Yeek.Update -= Yeek_Update;
		On.Player.MovementUpdate -= Player_MovementUpdate;
	}

	private static void PlayerMovementUpdate(ILContext il)
	{
		var c = new ILCursor(il);
		var vars = new List<VariableDefinition>();
		for (var i = 0; i < il.Body.Variables.Count; i++)
		{
			VariableDefinition varI = il.Body.Variables[i];
			if (varI.VariableType.Name.Contains("VinePosition"))
				vars.Add(varI);
		}
		var ctr = 0;
		for (var i = 0; i < il.Instrs.Count; i++)
		{
			if (il.Instrs[i].MatchLdsfld<SoundID>("Leaves"))
			{
				ctr++;
				c.Goto(i, MoveType.After);
				c.Emit(OpCodes.Ldarg_0);
				c.Emit(OpCodes.Ldloc, vars[ctr - 1]);
				c.EmitDelegate((SoundID sound, Player self, ClimbableVinesSystem.VinePosition vPos) =>
				{
					if (self.room?.climbableVines is ClimbableVinesSystem sys && sys.vines.Count > 0 && sys.GetVineObject(vPos) is IClimbJumpVine v)
						sound = v.GrabSound();
					return sound;
				});
				if (ctr == 2)
					break;
			}
		}
	}

	private static void PlayerJump(ILContext il)
	{
		var c = new ILCursor(il);
		if (c.TryGotoNext(MoveType.After,
			x => x.MatchLdarg(0),
			x => x.MatchLdfld<Player>("animation"),
			x => x.MatchLdsfld<Player.AnimationIndex>("ClimbOnBeam"),
			x => x.MatchCall(out _)))
		{
			c.Emit(OpCodes.Ldarg_0);
			c.EmitDelegate(JumpAllowed);
		}
		else
			LogError("Couldn't ILHook Player.Jump!");
	}

	private static void PlayerUpdateAnimation(ILContext il)
	{
		var c = new ILCursor(il);
		if (c.TryGotoNext(MoveType.After,
			x => x.MatchLdarg(0),
			x => x.MatchLdfld<UpdatableAndDeletable>("room"),
			x => x.MatchLdfld<Room>("climbableVines"),
			x => x.MatchLdarg(0),
			x => x.MatchLdfld<Player>("vinePos"),
			x => x.MatchLdarg(0),
			x => x.MatchCallOrCallvirt<ClimbableVinesSystem>("VineBeingClimbedOn")))
		{
			c.Emit(OpCodes.Ldarg_0);
			c.EmitDelegate((Player self) =>
			{
				if (self.room?.climbableVines is ClimbableVinesSystem sys && self.vinePos is ClimbableVinesSystem.VinePosition vPos && sys.vines.Count > 0 && sys.GetVineObject(vPos) is IClimbJumpVine v && v.JumpAllowed())
					self.canJump = 5;
			});
		}
		else
			LogError("Couldn't ILHook Player.UpdateAnimation! (part 1)");
		if (c.TryGotoNext(MoveType.After,
			x => x.MatchLdsfld<SoundID>("Leaves")))
		{
			c.Emit(OpCodes.Ldarg_0);
			c.EmitDelegate((SoundID sound, Player self) =>
			{
				if (self.room?.climbableVines is ClimbableVinesSystem sys && self.vinePos is ClimbableVinesSystem.VinePosition vPos && sys.vines.Count > 0 && sys.GetVineObject(vPos) is IClimbJumpVine v)
					sound = v.ClimbSound();
				return sound;
			});
		}
		else
			LogError("Couldn't ILHook Player.UpdateAnimation! (part 2)");
	}

	private static void ClimbableVineRendererAIMapReady(On.MoreSlugcats.ClimbableVineRenderer.orig_AIMapReady orig, MoreSlugcats.ClimbableVineRenderer self)
	{
		orig(self);
		List<PlacedObject> pObjs = self.room.roomSettings.placedObjects;
		for (var i = 0; i < pObjs.Count; i++)
		{
			PlacedObject pObj = pObjs[i];
			if (pObj.active && pObj.type == _Enums.ClimbableWire)
			{
				var wire = new ClimbableWire(self.room, self.totalSprites, pObj);
				self.room.AddObject(wire);
				self.climbVines.Add(wire);
				self.totalSprites += wire.graphic.sprites;
			}
		}
	}
}

/// <summary>
/// Implement this to be able to jump off your custom vine
/// </summary>
public interface IClimbJumpVine : IClimbableVine
{
	/// <summary>
	/// Sound played when the player grabs your vine
	/// </summary>
	/// <returns></returns>
    SoundID GrabSound();

	/// <summary>
	/// Sound played when the player is climbing your vine
	/// </summary>
	/// <returns></returns>
	SoundID ClimbSound();

	/// <summary>
	/// If the player can jump off your vine
	/// </summary>
	/// <returns></returns>
	bool JumpAllowed();
}

/// <summary>
/// By LB/M4rbleL1ne
/// A climbable wire (effect color and length options)
/// </summary>
public class ClimbableWire : MoreSlugcats.ClimbableVine, IClimbJumpVine
{
    internal readonly PlacedObject _pObj;

	///<inheritdoc/>
	public ClimbableWire(Room room, int firstSprite, PlacedObject placedObject) : base(room, firstSprite, placedObject)
    {
		Vector2 hPos = (placedObject.data as PlacedObject.ResizableObjectData)!.handlePos;
		var lgt = IntClamp((int)((placedObject.data as ClimbWireData)!._lgt + (hPos.magnitude * 1.1f + 50f) / 4f), 2, 400);
        var lgtGr = IntClamp((int)((placedObject.data as ClimbWireData)!._lgt + (hPos.magnitude * 1.1f + 50f) / 11f), 2, 400);
        _pObj = placedObject;
        baseColor = room.game.cameras[0].currentPalette.blackColor;
        conRad = 10f;
        segments = new Vector2[Mathf.Max(2, (int)Mathf.Clamp(lgt / conRad, 2f, 400f)), 3];
		Vector2[,] segs = segments;
        Vector2 spawnPosA = placedObject.pos, spawnPosB = placedObject.pos + hPos;
		var l0 = segs.GetLength(0);
		for (var i = 0; i < l0; i++)
        {
            var t = i / (float)(l0 - 1);
            segs[i, 0] = Vector2.Lerp(spawnPosA, spawnPosB, t) + RNV() * UnityEngine.Random.value;
            segs[i, 1] = segs[i, 0];
            segs[i, 2] = RNV() * UnityEngine.Random.value;
        }
        ropes = new Rope[l0 - 1];
		Rope[] rps = ropes;
		for (var i = 0; i < rps.Length; i++)
			rps[i] = new(room, segs[i, 0], segs[i + 1, 0], 2f);
        conRad *= 3f;
        possList = new();
        for (var j = 0; j < l0; j++)
            possList.Add(segs[j, 0]);
        graphic = new ClimbableWireGraphics(this, lgtGr, firstSprite);
    }

	SoundID IClimbJumpVine.GrabSound() => SoundID.Slugcat_Grab_Beam;

    SoundID IClimbJumpVine.ClimbSound() => SoundID.Slugcat_Climb_Along_Horizontal_Beam;

    bool IClimbJumpVine.JumpAllowed() => true;

	private class ClimbableWireGraphics : ClimbVineGraphic
	{
		private int _effectColor;

		internal ClimbableWireGraphics(ClimbableWire owner, int parts, int firstSprite) : base(owner, parts, firstSprite)
		{
			sprites = 1;
			leaves = Array.Empty<Leaf>();
		}

		public override void Update()
		{
			if ((owner as ClimbableWire)?._pObj?.data is ClimbJumpVineData d)
				_effectColor = (int)(d._colorType - 1);
			base.Update();
		}

		public override float OnVineEffectColorFac(float floatPos) => 0f;

		public override void DrawSprite(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
		{
			base.DrawSprite(sLeaser, rCam, timeStacker, camPos);
			if (_effectColor >= 0)
				sLeaser.sprites[firstSprite].color = rCam.currentPalette.texture.GetPixel(30, 5 - _effectColor * 2);
			else
				sLeaser.sprites[firstSprite].color = rCam.currentPalette.blackColor;
		}

		public override void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
		{
			base.ApplyPalette(sLeaser, rCam, palette);
			if (_effectColor >= 0)
				sLeaser.sprites[firstSprite].color = palette.texture.GetPixel(30, 5 - _effectColor * 2);
			else
				sLeaser.sprites[firstSprite].color = palette.blackColor;
		}
	}
}

/// <summary>
/// By LB/M4rbleL1ne
/// A climbable pole (effect color option)
/// </summary>
public class ClimbablePole : UpdatableAndDeletable, IClimbJumpVine, IDrawable
{
	private readonly ClimbJumpVineData _data;
	private readonly Vector2[] _pos = new Vector2[3];
	private float _length, _rotation;
	private int _effectColor;

	///<inheritdoc/>
	public ClimbablePole(Room room, ClimbJumpVineData data)
    {
        this.room = room;
        _data = data;
        _pos[0] = _data.owner.pos;
        _pos[1] = _data.owner.pos + _data.handlePos;
        _length = _data.Rad;
        _rotation = VecToDeg(_data.handlePos.normalized);
        if (room.climbableVines is null)
        {
            room.climbableVines = new();
            room.AddObject(room.climbableVines);
        }
        room.climbableVines.vines.Add(this);
    }

	///<inheritdoc/>
	public override void Update(bool eu)
    {
        base.Update(eu);
        if (_data is ClimbJumpVineData d)
        {
            _effectColor = (int)(d._colorType - 1);
            if (d.owner is PlacedObject pObj)
            {
                _pos[0] = pObj.pos;
                _pos[1] = pObj.pos + d.handlePos;
                _pos[2] = pObj.pos + d.handlePos / 2f;
            }
            _length = d.Rad;
            _rotation = VecToDeg(d.handlePos.normalized);
        }
    }

    Vector2 IClimbableVine.Pos(int index) => _pos[index];

    int IClimbableVine.TotalPositions() => _pos.Length;

    float IClimbableVine.Rad(int index) => 2f;

    float IClimbableVine.Mass(int index) => float.MaxValue;

    void IClimbableVine.Push(int index, Vector2 movement) { }

    void IClimbableVine.BeingClimbedOn(Creature crit) { }

    bool IClimbableVine.CurrentlyClimbable() => true;

    SoundID IClimbJumpVine.GrabSound() => SoundID.Slugcat_Grab_Beam;

    SoundID IClimbJumpVine.ClimbSound() => SoundID.Slugcat_Climb_Along_Horizontal_Beam;

    bool IClimbJumpVine.JumpAllowed() => true;

    void IDrawable.InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
    {
        sLeaser.sprites = new FSprite[] 
        { 
            new("pixel")
		    {
                anchorY = 0f,
                scaleX = 4f
            } 
        };
        AddToContainer(sLeaser, rCam, null);
    }

	///<inheritdoc/>
	void IDrawable.DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
		FSprite spr0 = sLeaser.sprites[0];
        spr0.x = _pos[0].x - camPos.x;
        spr0.y = _pos[0].y - camPos.y;
        spr0.scaleY = _length;
        spr0.rotation = _rotation;
        if (_effectColor >= 0)
            spr0.color = rCam.currentPalette.texture.GetPixel(30, 5 - _effectColor * 2);
        else
            spr0.color = rCam.currentPalette.blackColor;
        if (slatedForDeletetion || room != rCam.room)
            sLeaser.CleanSpritesAndRemove();
    }

    void IDrawable.ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette) 
    {
        if (_effectColor >= 0)
            sLeaser.sprites[0].color = palette.texture.GetPixel(30, 5 - _effectColor * 2);
        else
            sLeaser.sprites[0].color = palette.blackColor;
    }

	///<inheritdoc/>
	public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer? newContatiner)
    {
        newContatiner ??= rCam.ReturnFContainer("Midground");
        for (var i = 0; i < sLeaser.sprites.Length; i++)
        {
            sLeaser.sprites[i].RemoveFromContainer();
            newContatiner.AddChild(sLeaser.sprites[i]);
        }
    }
}

/// <summary>
/// Climbable vine common data
/// </summary>
public class ClimbJumpVineData : PlacedObject.ResizableObjectData
{
    internal Vector2 _panelPos = DegToVec(120f) * 20f;
    internal Color _colorType;

	///<inheritdoc/>
	public ClimbJumpVineData(PlacedObject owner) : base(owner) { }

	///<inheritdoc/>
	public override void FromString(string s)
    {
        base.FromString(s);
        var ar = Regex.Split(s, "~");
        if (ar.Length >= 5)
        {
            float.TryParse(ar[2], NumberStyles.Any, CultureInfo.InvariantCulture, out _panelPos.x);
            float.TryParse(ar[3], NumberStyles.Any, CultureInfo.InvariantCulture, out _panelPos.y);
            Enum.TryParse(ar[4], out _colorType);
            unrecognizedAttributes = SaveUtils.PopulateUnrecognizedStringAttrs(ar, 5);
        }
    }

	///<inheritdoc/>
	public override string ToString() => SaveUtils.AppendUnrecognizedStringAttrs(SaveState.SetCustomData(this, $"{BaseSaveString()}~{_panelPos.x}~{_panelPos.y}~{_colorType}"), "~", unrecognizedAttributes);

	internal enum Color
	{
		Black,
		EffectColor1,
		EffectColor2
	}
}

/// <summary>
/// Climbable wire data
/// </summary>
public class ClimbWireData : ClimbJumpVineData
{
    internal int _lgt = 2;

	///<inheritdoc/>
	public ClimbWireData(PlacedObject owner) : base(owner) { }

	///<inheritdoc/>
	public override void FromString(string s)
    {
        base.FromString(s);
        var ar = Regex.Split(s, "~");
        if (ar.Length >= 6)
        {
            int.TryParse(ar[5], NumberStyles.Any, CultureInfo.InvariantCulture, out _lgt);
            unrecognizedAttributes = SaveUtils.PopulateUnrecognizedStringAttrs(ar, 6);
        }
    }

	///<inheritdoc/>
	public override string ToString() => SaveUtils.AppendUnrecognizedStringAttrs(SaveState.SetCustomData(this, $"{BaseSaveString()}~{_panelPos.x}~{_panelPos.y}~{_colorType}~{_lgt}"), "~", unrecognizedAttributes);
}

/// <summary>
/// Climbable vine Dev UI representation
/// </summary>
public class ClimbJumpVineRepresentation : ResizeableObjectRepresentation
{
    private readonly int _panelLine, _panel;

	///<inheritdoc/>
	public ClimbJumpVineRepresentation(DevUI owner, string IDstring, DevUINode parentNode, PlacedObject pObj) : base(owner, IDstring, parentNode, pObj, pObj.type.ToString(), false)
    {
        subNodes.Add(new ClimbJumpVineControlPanel(owner, "Climb_Jump_Vine_Panel", this, new(0f, 100f)) { pos = (pObj.data as ClimbJumpVineData)!._panelPos });
        _panel = subNodes.Count - 1;
        fSprites.Add(new("pixel") { anchorY = 0f });
        _panelLine = fSprites.Count - 1;
        owner.placedObjectsContainer.AddChild(fSprites[_panelLine]);
    }

	///<inheritdoc/>
	public override void Refresh()
    {
        base.Refresh();
        MoveSprite(_panelLine, absPos);
        (pObj.data as ClimbJumpVineData)!._panelPos = (subNodes[_panel] as Panel)!.pos;
        fSprites[_panelLine].scaleY = (subNodes[_panel] as Panel)!.pos.magnitude;
        fSprites[_panelLine].rotation = AimFromOneVectorToAnother(absPos, (subNodes[_panel] as Panel)!.absPos);
    }

	private sealed class LgtSlider : Slider
	{
		private ClimbWireData Data
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get =>((parentNode.parentNode as ClimbJumpVineRepresentation)!.pObj.data as ClimbWireData)!;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal LgtSlider(DevUI owner, string IDstring, DevUINode parentNode, Vector2 pos) : base(owner, IDstring, parentNode, pos, "Length: ", false, 110f) { }

		public override void Refresh()
		{
			base.Refresh();
			if (Data is not ClimbWireData d)
				return;
			var num = d._lgt;
			NumberText = num.ToString();
			RefreshNubPos((num - 2f) / 398f);
		}

		public override void NubDragged(float nubPos)
		{
			if (Data is not ClimbWireData d)
				return;
			d._lgt = IntClamp((int)(nubPos * 398f + 2f), 2, 400);
			parentNode.parentNode.Refresh();
			Refresh();
		}
	}

	private sealed class ClimbJumpVineControlPanel : Panel, IDevUISignals
	{
		private ClimbJumpVineData Data
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => ((parentNode as ClimbJumpVineRepresentation)!.pObj.data as ClimbJumpVineData)!;
		}

		internal ClimbJumpVineControlPanel(DevUI owner, string IDstring, DevUINode parentNode, Vector2 pos) : base(owner, IDstring, parentNode, pos, new(250f, 25f), string.Empty)
		{
			fLabels[0].text = (parentNode as ClimbJumpVineRepresentation)!.pObj.type.ToString();
			subNodes.Add(new Button(owner, "Clr_Button", this, new(5f, 5f), 240f, "Color: " + Data._colorType));
			if (Data is ClimbWireData)
			{
				subNodes.Add(new LgtSlider(owner, "Lgt_Slider", this, new(5f, 25f)));
				size.y += 20f;
			}
		}

		void IDevUISignals.Signal(DevUISignalType type, DevUINode sender, string message)
		{
			if (sender is Button b && b.IDstring is "Clr_Button" && Data is ClimbJumpVineData d)
			{
				if (d._colorType != ClimbJumpVineData.Color.EffectColor2)
					d._colorType++;
				else
					d._colorType = ClimbJumpVineData.Color.Black;
				b.Text = "Color: " + d._colorType;
			}
		}
	}
}
