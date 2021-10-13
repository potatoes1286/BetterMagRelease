using System;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using FistVR;
using H3VRUtils;
using HarmonyLib;
using UnityEngine;

namespace BetterMagRelease
{
	[BepInPlugin(PluginInfo.GUID, PluginInfo.NAME, PluginInfo.VERSION)]
	[BepInProcess("h3vr.exe")]
	public class Plugin : BaseUnityPlugin
	{
		public static ConfigEntry<bool> EnableDebug;
		public static ConfigEntry<bool> HasStartedUp;
		
		public void Start()
		{
			Harmony.CreateAndPatchAll(typeof(Plugin));
			
			EnableDebug = Config.Bind("General Settings", "Enable Debugging", false, "Logs to console if a firearm spawned does not have a mag release setting.");
			HasStartedUp  = Config.Bind("General Settings", "Has Started Up", false, "Enables mag release if false, then sets to true.");

			if (!HasStartedUp.Value)
			{
				HasStartedUp.Value = true;
				UtilsBepInExLoader.EnablePaddleMagRelease();
			}
		}

		[HarmonyPatch(typeof(FVRInteractiveObject), "Awake")]
		[HarmonyPrefix]
		public static bool ClosedBoltMagEjectionTriggerPatch(FVRInteractiveObject __instance)
		{
			if (__instance is ClosedBoltMagEjectionTrigger)
			{
				var met = __instance as ClosedBoltMagEjectionTrigger;
				var magdrop = MagReplacerData.GetMagDropData();
				var paddle = MagReplacerData.GetPaddleData();
				var both = paddle.Concat(magdrop).ToArray();
				if (both.Contains(met.Receiver.ObjectWrapper.ItemID))
				{
					var mr = met.gameObject.AddComponent(typeof(H3VRUtilsMagRelease)) as H3VRUtilsMagRelease;
					mr.PositionInterpSpeed = 4;
					mr.RotationInterpSpeed = 4;
					mr.EndInteractionIfDistant = true;
					mr.EndInteractionDistance = 0.25f;
					mr.ClosedBoltReceiver = met.Receiver;
					mr.PressDownToRelease = true;
					if (paddle.Contains(met.Receiver.ObjectWrapper.ItemID))
					{
						mr.TouchpadDir = H3VRUtilsMagRelease.TouchpadDirType.Down;
					}
					else
					{
						mr.TouchpadDir = H3VRUtilsMagRelease.TouchpadDirType.NoDirection;
						met.Receiver.HasMagReleaseButton = true;
					}
					mr.setWepType();
					Destroy(met);
				}
				else
				{
					if(EnableDebug.Value) Debug.Log(met.Receiver.ObjectWrapper.ItemID + " does not have a setting!");
				}
			}
			
			//OPEN BOLT
			if (__instance is OpenBoltMagReleaseTrigger)
			{
				var met = __instance as OpenBoltMagReleaseTrigger;
				var magdrop = MagReplacerData.GetMagDropData();
				var paddle = MagReplacerData.GetPaddleData();
				var both = paddle.Concat(magdrop).ToArray();
				if (both.Contains(met.Receiver.ObjectWrapper.ItemID))
				{
					var mr = met.gameObject.AddComponent(typeof(H3VRUtilsMagRelease)) as H3VRUtilsMagRelease;
					mr.PositionInterpSpeed = 4;
					mr.RotationInterpSpeed = 4;
					mr.EndInteractionIfDistant = true;
					mr.EndInteractionDistance = 0.25f;
					mr.OpenBoltWeapon = met.Receiver;
					mr.PressDownToRelease = true;
					if (paddle.Contains(met.Receiver.ObjectWrapper.ItemID))
					{
						mr.TouchpadDir = H3VRUtilsMagRelease.TouchpadDirType.Down;
					}
					else
					{
						mr.TouchpadDir = H3VRUtilsMagRelease.TouchpadDirType.NoDirection;
					}
					mr.setWepType();
					Destroy(met);
				}
				else
				{
					if(EnableDebug.Value) Debug.Log(met.Receiver.ObjectWrapper.ItemID + " does not have a setting!");
				}
			}
			
			//BOLT ACTION
			if (__instance is BoltActionMagEjectionTrigger)
			{
				var met = __instance as BoltActionMagEjectionTrigger;
				var magdrop = MagReplacerData.GetMagDropData();
				var paddle = MagReplacerData.GetPaddleData();
				var both = paddle.Concat(magdrop).ToArray();
				if (both.Contains(met.Rifle.ObjectWrapper.ItemID))
				{
					var mr = met.gameObject.AddComponent(typeof(H3VRUtilsMagRelease)) as H3VRUtilsMagRelease;
					mr.PositionInterpSpeed = 4;
					mr.RotationInterpSpeed = 4;
					mr.EndInteractionIfDistant = true;
					mr.EndInteractionDistance = 0.25f;
					mr.BoltActionWeapon = met.Rifle;
					mr.PressDownToRelease = true;
					if (paddle.Contains(met.Rifle.ObjectWrapper.ItemID))
					{
						mr.TouchpadDir = H3VRUtilsMagRelease.TouchpadDirType.Down;
					}
					else
					{
						mr.TouchpadDir = H3VRUtilsMagRelease.TouchpadDirType.NoDirection;
					}
					mr.setWepType();
					Destroy(met);
				}
				else
				{
					if(EnableDebug.Value) Debug.Log(met.Rifle.ObjectWrapper.ItemID + " does not have a setting!");
				}
			}

			return true;
		}
	}
	
	static class MagReplacerData
	{
		public struct Directories
		{
			public static string DLLloc = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
			public static string PaddleMagReleaseLoc = Path.Combine(DLLloc, "ForcePaddleMagRelease.txt");
			public static string ForcedMagDrop = Path.Combine(DLLloc, "ForceForcedMagDrop.txt");
		}

		private static string[] _savedPaddleData = null;
		public static string[] GetPaddleData(bool reset = false)
		{
			if (!File.Exists(Directories.PaddleMagReleaseLoc)) { File.CreateText(Directories.PaddleMagReleaseLoc); }
			if (_savedPaddleData != null && !reset) return _savedPaddleData;
			_savedPaddleData = File.ReadAllLines(Directories.PaddleMagReleaseLoc);
			return _savedPaddleData;
		}

		private static string[] _savedMagDropData = null;
		public static string[] GetMagDropData(bool reset = false)
		{
			if (!File.Exists(Directories.ForcedMagDrop)) { File.CreateText(Directories.ForcedMagDrop); }
			if (_savedMagDropData != null && !reset) return _savedMagDropData;
			_savedMagDropData = File.ReadAllLines(Directories.ForcedMagDrop);
			return _savedMagDropData;
		}
	}
}