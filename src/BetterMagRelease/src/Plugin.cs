using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
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
		public static ManualLogSource   Log;
		public void Start() {
			Log = Logger;
			EnableDebug = Config.Bind("General Settings", "Enable Debugging", false, "Logs to console if a firearm spawned does not have a mag release setting.");
			HasStartedUp  = Config.Bind("General Settings", "Has Started Up", false, "Enables mag release if false, then sets to true.");

			if (!HasStartedUp.Value)
			{
				HasStartedUp.Value = true;
				UtilsBepInExLoader.EnablePaddleMagRelease();
			}

			MagReplacerData.AssembleData();
			Harmony.CreateAndPatchAll(typeof(Plugin));
		}

		public enum LogType {
			Default,
			Debug
		}

		public static void log(object data, LogType type = LogType.Default) {
			if (type == LogType.Debug)
				return;
			Log.LogInfo(data);
		}
		
		[HarmonyPatch(typeof(FVRInteractiveObject), "Awake")]
		[HarmonyPrefix]
		public static bool ClosedBoltMagEjectionTriggerPatch(FVRInteractiveObject __instance)
		{
			if (__instance is ClosedBoltMagEjectionTrigger)
			{
				var met = __instance as ClosedBoltMagEjectionTrigger;
				if (met.IsSecondarySlotGrab)
					return true;
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
					if(EnableDebug.Value) Debug.LogWarning($"{met.Receiver.ObjectWrapper.ItemID} ({met.Receiver.ObjectWrapper.name}) does not have a setting!");
				}
			}
			
			//OPEN BOLT
			if (__instance is OpenBoltMagReleaseTrigger)
			{
				var met = __instance as OpenBoltMagReleaseTrigger;
				if (met.IsSecondarySlotGrab)
					return true;
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
					if(EnableDebug.Value) Debug.LogWarning($"{met.Receiver.ObjectWrapper.ItemID} ({met.Receiver.ObjectWrapper.name}) does not have a setting!");
				}
			}
			
			//BOLT ACTION
			if (__instance is BoltActionMagEjectionTrigger)
			{
				var met = __instance as BoltActionMagEjectionTrigger;
				if (met.IsSecondarySlotGrab)
					return true;
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
					if(EnableDebug.Value) Log.LogWarning(met.Rifle.ObjectWrapper.ItemID + " does not have a setting!");
				}
			}

			return true;
		}
	}
	
	static class MagReplacerData
	{
		/*public struct Directories
		{
			public static string DLLloc = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
			public static string PaddleMagReleaseLoc = Path.Combine(DLLloc, "ForcePaddleMagRelease.txt");
			public static string ForcedMagDrop = Path.Combine(DLLloc, "ForceForcedMagDrop.txt");
		}*/
		
		
		private static string[] _savedPaddleData = new string[]{};
		public static string[] GetPaddleData(bool reset = false)
		{
			if(reset)
				AssembleData();
			return _savedPaddleData;
		}

		private static string[] _savedMagDropData = new string[]{};
		public static string[] GetMagDropData(bool reset = false)
		{
			if(reset)
				AssembleData();
			return _savedMagDropData;
		}

		public static void AssembleData()
		{
			//load txts
			List<string> paddleData = LoadData("*_BMR_Paddle.txt").Distinct().ToList();
			List<string> magDropData = LoadData("*_BMR_Eject.txt").Distinct().ToList();
			//conflict resolution
			List<string> conflicts = paddleData.Intersect(magDropData).ToList();
			foreach (var conflict in conflicts)
			{
				int pIndex = paddleData.IndexOf(conflict);
				int mdIndex = magDropData.IndexOf(conflict);
				if (pIndex < mdIndex)
				{
					magDropData.RemoveAt(mdIndex);
					Debug.LogError("BetterMagRelease: There are conflicting sources for " + conflict + "! Chose Paddle.");
				}
				else
					paddleData.RemoveAt(pIndex);
				Debug.LogError("BetterMagRelease: There are conflicting sources for " + conflict + "! Chose Mag Drop.");
			}

			_savedPaddleData = paddleData.ToArray();
			_savedMagDropData = magDropData.ToArray();
		}

		public static List<string> LoadData(string searchPattern)
		{
			List<string> sources = new List<string>();
			List<string[]> sourcesContent = new List<string[]>();
			sources = Directory.GetFiles(Paths.BepInExRootPath, searchPattern, SearchOption.AllDirectories).ToList();
			foreach (var source in sources)
			{
				Debug.Log("BetterMagRelease: Adding source " + source);
				sourcesContent.Add(File.ReadAllLines(source));
			}
			sourcesContent = sourcesContent.OrderBy(x => x.Length).Reverse().ToList();
			IEnumerable<string> concat = Enumerable.Empty<string>();
			foreach (var content in sourcesContent)
				concat = concat.Concat(content);
			return concat.ToList();
		}
	}
}