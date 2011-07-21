﻿using LuaNetInterface;
using Ponykart.Actors;
using Ponykart.Players;

namespace Ponykart.Lua {
	[LuaPackage(null, null)]
	public class LKernelWrapper {

		public LKernelWrapper() {
			LKernel.Get<LuaMain>().RegisterLuaFunctions(this);
		}

		[LuaFunction("pk", "dox")]
		public static Kart GetPlayerKart() {
			var playermanager = LKernel.Get<PlayerManager>();
			if (playermanager != null) {
				return playermanager.MainPlayer.Kart;
			}
			else return null;
		}

		[LuaFunction("eslip", "", "f")]
		public static void ExtremumSlip(float f) {
			Kart.LatExtremumSlip = f;
		}

		[LuaFunction("evalue", "", "f")]
		public static void ExtremumValue(float f) {
			Kart.LatExtremumValue = f;
		}

		[LuaFunction("aslip", "", "f")]
		public static void AsymptoteSlip(float f) {
			Kart.LatAsymptoteSlip = f;
		}

		[LuaFunction("avalue", "", "f")]
		public static void AsymptoteValue(float f) {
			Kart.LatAsymptoteValue = f;
		}
	}
}
