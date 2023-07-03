using System.Linq;
using FistVR;
using HarmonyLib;
using UnityEngine;

namespace BetterMagRelease {
	public class Extractor {
		[HarmonyPatch(typeof(BreakActionWeapon), "Awake")]
		[HarmonyPrefix]
		public static bool ExtractedRoundsReleasePatch(BreakActionWeapon __instance) {
			if (!Plugin.ExtractedRounds_IsEnabled.Value)
				return true;
			if (MagReplacerData.SavedExtractedRoundsData.Contains(__instance.ObjectWrapper.ItemID))
				__instance.SetEjectOnOpen(false);
			return true;
		}

		[HarmonyPatch(typeof(BreakActionWeapon), "FVRFixedUpdate")]
		[HarmonyPrefix]
		public static bool ExtractedRoundsShuckPatch(BreakActionWeapon __instance) {
			if (!MagReplacerData.SavedExtractedRoundsData.Contains(__instance.ObjectWrapper.ItemID) ||
			    !Plugin.Shucking_IsEnabled.Value)
				return true;
			float shuckStr = 0;
			foreach (var barrel in __instance.Barrels) {
				//Gets the velocity of the hand going backwards relative to the gun.
				if (__instance.m_hand != null)
					shuckStr = Vector3.Dot(Vector3.back,
					                       __instance.transform.InverseTransformDirection(__instance.m_hand.Input
						                                                                     .VelLinearWorld));
				//GENERALLY SPEAKING, a shucking movement will produce a vel of 1.
				if (!__instance.IsLatched && barrel.Chamber.IsFull) {
					if (shuckStr > Plugin.Shucking_Strength.Value) {
						//eject the round. This is just modified popoutempties so that
						//they dont eject so fast.
						__instance.PlayAudioEvent(FirearmAudioEventType.MagazineEjectRound, 1f);
						Vector3 ejectionAngularVelocity = UnityEngine.Random.onUnitSphere * 10f;
						barrel.Chamber
.EjectRound(barrel.Chamber.transform.position + barrel.Chamber.transform.forward * __instance.EjectOffset,
            barrel.Chamber.transform.forward * -shuckStr, ejectionAngularVelocity, false);
					}
				}
			}
			return true;
		}
		
		/* fix this shit l8r
		#region Derringer
		//if default value is wrong, override
		[HarmonyPatch(typeof(Derringer), "Awake")]
		[HarmonyPrefix]
		public static bool DerringerEjectPatch(Derringer __instance) {
			if (!MagReplacerData.SavedExtractedRoundsData.Contains(__instance.ObjectWrapper.ItemID) ||
			    !Plugin.Shucking_IsEnabled.Value)
				return true;
			__instance.DoesAutoEjectRounds = false;
			return true;
		}

		[HarmonyPatch(typeof(Derringer), "FVRUpdate")]
		[HarmonyPrefix]
		public static bool DerringerShuckPatch(Derringer __instance) {
			if (!MagReplacerData.SavedExtractedRoundsData.Contains(__instance.ObjectWrapper.ItemID) ||
			    !Plugin.Shucking_IsEnabled.Value)
				return true;
			float shuckStr = 0;
			foreach (var barrel in __instance.Barrels) {
				//Gets the velocity of the hand going backwards relative to the gun.
				if (__instance.m_hand != null)
					shuckStr = Vector3.Dot(Vector3.back, 
					                       __instance.transform.InverseTransformDirection(__instance.m_hand.Input
						                                                                     .VelLinearWorld));
				//GENERALLY SPEAKING, a shucking movement will produce a vel of 1.
				if (__instance.m_hingeState == Derringer.HingeState.Open && barrel.Chamber.IsFull) {
					if (shuckStr > Plugin.Shucking_Strength.Value) {
						//eject the round. This is just modified popoutempties so that
						//they dont eject so fast.
						__instance.PlayAudioEvent(FirearmAudioEventType.MagazineEjectRound, 1f);
						Vector3 ejectionAngularVelocity = UnityEngine.Random.onUnitSphere * 10f;
						barrel.Chamber.EjectRound(
						                          barrel.Chamber.transform.position + barrel.Chamber.transform.forward * -0.03f, 
						                          barrel.Chamber.transform.forward * -0.3f * -shuckStr,
						                          ejectionAngularVelocity, false);
					}
				}
			}
			return true;
		}
		#endregion

		#region Flare Gun

		//if default value is wrong, override
		[HarmonyPatch(typeof(Flaregun), "Awake")]
		[HarmonyPrefix]
		public static bool FlaregunEjectPatch(Flaregun __instance) {
			if (!MagReplacerData.SavedExtractedRoundsData.Contains(__instance.ObjectWrapper.ItemID) ||
			    !Plugin.Shucking_IsEnabled.Value)
				return true;
			__instance.AutoEject = false;
			return true;
		}

		[HarmonyPatch(typeof(Derringer), "FVRUpdate")]
		[HarmonyPrefix]
		public static bool FlaregunTipPatch(Flaregun __instance) {
			if (!MagReplacerData.SavedExtractedRoundsData.Contains(__instance.ObjectWrapper.ItemID) ||
			    !Plugin.Shucking_IsEnabled.Value)
				return true;
			if (__instance.m_hingeState == Flaregun.HingeState.Opening)
			{
				__instance.m_hingeLerp += Time.deltaTime * Mathf.Lerp(4f, 30f, __instance.m_hingeLerp * __instance.m_hingeLerp);
				if (__instance.m_hingeLerp >= 1f)
				{
					__instance.m_hingeState = Flaregun.HingeState.Open;
					__instance.m_hingeLerp = 1f;
				}
				__instance.SetAnimatedComponent(__instance.Hinge, Mathf.Lerp(0f, __instance.RotOut, __instance.m_hingeLerp), FVRPhysicalObject.InterpStyle.Rotation, __instance.HingeAxis);
			}
			else if (__instance.m_hingeState == Flaregun.HingeState.Closing)
			{
				__instance.m_hingeLerp -= Time.deltaTime * Mathf.Lerp(3f, 25f, (__instance.m_hingeLerp - 1f) * (__instance.m_hingeLerp - 1f));
				if (__instance.m_hingeLerp <= 0f)
				{
					__instance.m_hingeState = Flaregun.HingeState.Closed;
					__instance.m_hingeLerp = 0f;
				}
				__instance.SetAnimatedComponent(__instance.Hinge, Mathf.Lerp(0f, __instance.RotOut, __instance.m_hingeLerp), FVRPhysicalObject.InterpStyle.Rotation, __instance.HingeAxis);
			}
			if (__instance.m_hingeState == Flaregun.HingeState.Open && __instance.AutoEject && Vector3.Angle(Vector3.up, __instance.Chamber.transform.forward) < 70f && __instance.Chamber.IsFull && __instance.Chamber.IsSpent && !__instance.IsHighPressureTolerant)
			{
				__instance.PlayAudioEvent(FirearmAudioEventType.MagazineEjectRound, 1f);
				__instance.Chamber.EjectRound(__instance.Chamber.transform.position + __instance.Chamber.transform.forward * -0.08f, __instance.Chamber.transform.forward * -0.3f, Vector3.right, false);
			}
			
			float shuckStr = 0;
			//Gets the velocity of the hand going backwards relative to the gun.
			if (__instance.m_hand != null)
				shuckStr = Vector3.Dot(Vector3.back,
				                       __instance.transform.InverseTransformDirection(__instance.m_hand.Input
					                                                                     .VelLinearWorld));
			//GENERALLY SPEAKING, a shucking movement will produce a vel of 1.
			if (__instance.m_hingeState == Flaregun.HingeState.Open && __instance.Chamber.IsFull) {
				if (shuckStr > Plugin.Shucking_Strength.Value) {
					//eject the round. This is just modified popoutempties so that
					//they dont eject so fast.
					__instance.PlayAudioEvent(FirearmAudioEventType.MagazineEjectRound, 1f);
					Vector3 ejectionAngularVelocity = UnityEngine.Random.onUnitSphere * 10f;
					__instance.Chamber.EjectRound(
					                              __instance.Chamber.transform.position +
					                              __instance.Chamber.transform.forward * -0.03f,
					                              __instance.Chamber.transform.forward * -0.3f * -shuckStr,
					                              ejectionAngularVelocity, false);
				}
			}
			return false;
		}

		#endregion

		#region Rolling Block
		//if default value is wrong, override
		[HarmonyPatch(typeof(RollingBlock), "OpenBreach")]
		[HarmonyPrefix]
		public static bool RollingBlockEjectPatch(RollingBlock __instance) {
			if (!MagReplacerData.SavedExtractedRoundsData.Contains(__instance.ObjectWrapper.ItemID) ||
			    !Plugin.Shucking_IsEnabled.Value)
				return true;
			__instance.m_tarBreachRot = __instance.BreachBlockRots.y;
			__instance.PlayAudioEvent(FirearmAudioEventType.BreachOpen, 1f);
			__instance.IsBreachOpenForGasOut = true;
			__instance.Chamber.IsAccessible = true;
			return false;
		}
		
		[HarmonyPatch(typeof(RollingBlock), "FVRUpdate")]
		[HarmonyPrefix]
		public static bool RollingBlockShuckPatch(RollingBlock __instance) {
			if (!MagReplacerData.SavedExtractedRoundsData.Contains(__instance.ObjectWrapper.ItemID) ||
			    !Plugin.Shucking_IsEnabled.Value)
				return true;
			float shuckStr = 0;
			//Gets the velocity of the hand going backwards relative to the gun.
			if (__instance.m_hand != null)
				shuckStr = Vector3.Dot(Vector3.back,
				                       __instance.transform.InverseTransformDirection(__instance.m_hand.Input
					                                                                     .VelLinearWorld));
			//GENERALLY SPEAKING, a shucking movement will produce a vel of 1.
			if (__instance.m_state == RollingBlock.RollingBlockState.HammerBackBreachOpen && __instance.Chamber.IsFull) {
				if (shuckStr > Plugin.Shucking_Strength.Value) {
					//eject the round. This is just modified popoutempties so that
					//they dont eject so fast.
					__instance.PlayAudioEvent(FirearmAudioEventType.MagazineEjectRound, 1f);
					Vector3 ejectionAngularVelocity = UnityEngine.Random.onUnitSphere * 10f;
					__instance.Chamber.EjectRound(
					                              __instance.Chamber.transform.position +
					                              __instance.Chamber.transform.forward * -0.03f,
					                              __instance.Chamber.transform.forward * -0.3f * -shuckStr,
					                              ejectionAngularVelocity, false);
				}
			}

			return true;
		}
		

		#endregion
		*/
		
	}
}