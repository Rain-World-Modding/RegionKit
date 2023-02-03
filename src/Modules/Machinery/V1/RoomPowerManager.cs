using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

using static RegionKit.Modules.Machinery._Module;
using static UnityEngine.Mathf;

namespace RegionKit.Modules.Machinery.V1;
/// <summary>
/// Replaces <see cref="Room.ElectricPower"/> behavior when present with custom modifiers. See: <see cref="IRoomPowerModifier"/>.
/// </summary>
public class RoomPowerManager : UpdatableAndDeletable
{
	/// <summary>
	/// POM ctor
	/// </summary>
	public RoomPowerManager(Room rm, PlacedObject pobj)
	{
		var h = rm.GetHashCode();
		if (__managersByRoomHash.ContainsKey(h)) __managersByRoomHash[h] = this;
		else __managersByRoomHash.Add(h, this);
		_PO = pobj;
		__logger.LogDebug($"({rm.abstractRoom.name}): created a RoomPowerManager.");
	}
	///<inheritdoc/>
	public override void Update(bool eu)
	{
		base.Update(eu);
		_selfCheckTimer++;
		if (_selfCheckTimer > 10) ValidateDeviceSet();
	}
	internal PowerManagerData pmData { get { _pmd = _pmd ?? _PO?.data as PowerManagerData ?? new PowerManagerData(null); return _pmd; } }
	private PowerManagerData? _pmd;
	private PlacedObject _PO;
	private int _selfCheckTimer = 0;
	/// <summary>
	/// Local resulting power. Applied on top of <see cref="GetGlobalPower"/>.
	/// </summary>
	/// <param name="point"></param>
	/// <returns></returns>
	public float GetPowerForPoint(Vector2 point)
	{
		var res = pmData.basePowerLevel;
		res += GetGlobalPower();
		foreach (var unit in _subs) if (unit.Enabled) res += unit.BonusForPoint(point);
		res = Clamp01(res);
		return res;
	}
	/// <summary>
	/// Sum of global power bonuses from all subscribers.
	/// </summary>
	/// <returns></returns>
	public float GetGlobalPower()
	{
		var res = pmData.basePowerLevel;
		foreach (var unit in _subs) { if (unit.Enabled) res += unit.GlobalBonus(); }
		res = Clamp01(res);
		return res;
	}
	/// <summary>
	/// Adds a powerModifier to current instance's sources
	/// </summary>
	/// <param name="obj"></param>
	public void RegisterPowerDevice(IRoomPowerModifier obj)
	{
		_subs.Add(obj);
		ValidateDeviceSet();
	}
	private void ValidateDeviceSet()
	{
		_selfCheckTimer = 0;
		for (int i = _subs.Count - 1; i >= 0; i--)
		{
			if (_subs[i].RemoveOnValidation) _subs.RemoveAt(i);
		}
	}

	private List<IRoomPowerModifier> _subs = new List<IRoomPowerModifier>();
	/// <summary>
	/// Use this interface to modify room power levels. Has to be impl by an <see cref="UpdatableAndDeletable"/>.
	/// </summary>
	public interface IRoomPowerModifier
	{
		/// <summary>
		/// Whether <see cref="RoomPowerManager"/> should remove the instance soon. Warning: several frames may pass between this being set to true and manager actually getting rid of the subscriber list! Make sure to handle your <see cref="Enabled"/> properly if this matters.
		/// </summary>
		bool RemoveOnValidation { get; }
		/// <summary>
		/// Whether power bonus for object should be applied.
		/// </summary>
		bool Enabled { get; }
		/// <summary>
		/// Power bonus for a specified position in the room. <see cref="RoomPowerManager.GetPowerForPoint(Vector2)"/> applies those ON TOP of <see cref="GlobalBonus"/>.
		/// </summary>
		/// <param name="point"></param>
		/// <returns></returns>
		float BonusForPoint(Vector2 point);
		/// <summary>
		/// General power bonus for room.
		/// </summary>
		/// <returns></returns>
		float GlobalBonus();
	}
}
