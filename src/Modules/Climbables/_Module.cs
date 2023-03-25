using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RegionKit.Modules.Effects;
using static RegionKit.Modules.DevUIMisc.SettingsSaveOptions;
using DevInterface;
using System.Diagnostics;
using MonoMod.RuntimeDetour;
using Climbables;
using System.CodeDom;

namespace RegionKit.Modules.Climbables;


[RegionKitModule(nameof(Enable), nameof(Disable), moduleName: "Climbables")]
public static class _Module
{
	private static bool __appliedOnce = false;
	internal static void Enable()
	{
		if (!__appliedOnce)
		{
			//what is this for
		}
		__appliedOnce = true;

		On.ClimbableVinesSystem.VineSwitch += ClimbableVinesSystem_VineSwitch_hk;

		RegisterFullyManagedObjectType(new ManagedField[] { new IntVector2Field("vector", new IntVector2(), IntVector2Field.IntVectorReprType.rect) }, typeof(ClimbablePoleH), "ClimbablePoleH", RK_POM_CATEGORY);
		RegisterFullyManagedObjectType(new ManagedField[] { new IntVector2Field("vector", new IntVector2(), IntVector2Field.IntVectorReprType.rect) }, typeof(ClimbablePoleV), "ClimbablePoleV", RK_POM_CATEGORY);
		RegisterFullyManagedObjectType(new ManagedField[] { new Vector2Field("vector", new Vector2(), Vector2Field.VectorReprType.line) }, typeof(ClimbableRope), "ClimbableRope", RK_POM_CATEGORY);
		RegisterManagedObject<ClimbableArc, BezierObjectData, BezierObjectRepresentation>("ClimbableArc", RK_POM_CATEGORY);
	}
	internal static void Disable()
	{
		On.ClimbableVinesSystem.VineSwitch -= ClimbableVinesSystem_VineSwitch_hk;
	}


	private static ClimbableVinesSystem.VinePosition ClimbableVinesSystem_VineSwitch_hk(On.ClimbableVinesSystem.orig_VineSwitch orig, ClimbableVinesSystem self, ClimbableVinesSystem.VinePosition vPos, UnityEngine.Vector2 goalPos, float rad)
	{
		ClimbableVinesSystem.VinePosition newPos = orig(self, vPos, goalPos, rad);

		if (self.vines[vPos.vine] is ClimbableArc && newPos == null && (vPos.floatPos == 0f || vPos.floatPos == 1f))
		{
			// Copypaste from orig but bypassing the dotprod check
			int num = self.PrevSegAtFloat(vPos.vine, vPos.floatPos);
			int num2 = Custom.IntClamp(num + 1, 0, self.vines[vPos.vine].TotalPositions() - 1);
			float t = Mathf.InverseLerp(self.FloatAtSegment(vPos.vine, num), self.FloatAtSegment(vPos.vine, num2), vPos.floatPos);
			Vector2 vector = Vector2.Lerp(self.vines[vPos.vine].Pos(num), self.vines[vPos.vine].Pos(num2), t);
			goalPos = vector + (vector - goalPos).normalized * 0.1f; // shorten that range a tiny bit.
			float f = Vector2.Dot((self.vines[vPos.vine].Pos(num) - self.vines[vPos.vine].Pos(num2)).normalized, (vector - goalPos).normalized);
			if (Mathf.Abs(f) > 0.5f)
			{
				float num3 = float.MaxValue;
				//ClimbableVinesSystem.VinePosition result = null;
				for (int i = 0; i < self.vines.Count; i++)
				{
					for (int j = 0; j < self.vines[i].TotalPositions() - 1; j++)
					{
						if (self.OverlappingSegment(self.vines[i].Pos(j), self.vines[i].Rad(j), self.vines[i].Pos(j + 1), self.vines[i].Rad(j + 1), vector, rad))
						{
							Vector2 vector2 = self.ClosestPointOnSegment(self.vines[i].Pos(j), self.vines[i].Pos(j + 1), vector);
							float num4 = Vector2.Distance(vector2, goalPos);
							num4 *= 1f - 0.25f * Mathf.Abs(Vector2.Dot((self.vines[i].Pos(j) - self.vines[i].Pos(j + 1)).normalized, (vector - goalPos).normalized));
							if (i == vPos.vine)
							{
								float num5 = Mathf.Lerp(self.FloatAtSegment(i, j), self.FloatAtSegment(i, j + 1), Mathf.InverseLerp(0f, Vector2.Distance(self.vines[i].Pos(j), self.vines[i].Pos(j + 1)), Vector2.Distance(self.vines[i].Pos(j), vector2))) * self.TotalLength(i);
								if (Mathf.Abs(vPos.floatPos * self.TotalLength(vPos.vine) - num5) < 100f)
								{
									num4 = float.MaxValue;
								}
							}
							if (num4 < num3)
							{
								num3 = num4;
								float t2 = Mathf.InverseLerp(0f, Vector2.Distance(self.vines[i].Pos(j), self.vines[i].Pos(j + 1)), Vector2.Distance(self.vines[i].Pos(j), vector2));
								newPos = new ClimbableVinesSystem.VinePosition(i, Mathf.Lerp(self.FloatAtSegment(i, j), self.FloatAtSegment(i, j + 1), t2));
							}
						}
					}
				}
			}
		}

		return newPos;
	}

}
