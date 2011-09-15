﻿using LuaNetInterface;

namespace Ponykart.Lua {
	/// <summary>
	/// A wrapper class for input and output
	/// </summary>
	[LuaPackage(null, null)]
	public class IOWrapper {

		public IOWrapper() {
			LKernel.GetG<LuaMain>().RegisterLuaFunctions(this);
		}

		/*[LuaFunction("save", "Saves the current level.")]
		public static void Save() {
			var levelManager = LKernel.GetG<LevelManager>();
			if (levelManager != null && levelManager.CurrentLevel != null) {
				levelManager.CurrentLevel.Save();
			}
		}*/
	}
}
