﻿using System;
using Mogre;
using Ponykart.Actors;
using Ponykart.Core;
using Ponykart.Lua;
using Ponykart.Properties;

namespace Ponykart.Levels {
	public delegate void LevelEvent(LevelChangedEventArgs eventArgs);

	public class LevelManager {
		public Level CurrentLevel { get; private set; }
		/// <summary>
		/// Is fired a few frames before we even start unloading anything. Mostly used for stuff that still requires screen rendering, such as putting up a loading screen
		/// </summary>
		public event LevelEvent OnLevelPreUnload;
		/// <summary>
		/// Is fired after the .muffins have been read and the .scene file has been used, but before we start actually creating any Things
		/// </summary>
		public event LevelEvent OnLevelLoad;
		/// <summary>
		/// Is fired after the level handlers have been disposed, but before we clean out the scene manager.
		/// </summary>
		public event LevelEvent OnLevelUnload;
		/// <summary>
		/// Is fired a few frames after the entire level load process, including after scripts have been run.
		/// </summary>
		public event LevelEvent OnLevelPostLoad;

		/// <summary>
		/// constructor
		/// </summary>
		public LevelManager() {
			this.IsValidLevel = false;
		}


		private bool hasRunPostInitEvents = false;
		/// <summary>
		/// runs level manager stuff that needs to run immediately after kernel setup
		/// </summary>
		public void RunPostInitEvents() {
			// don't let this run twice
			if (hasRunPostInitEvents)
				throw new ApplicationException("The LevelManager has already run its post-initialisation events!");

			CurrentLevel = new Level(Settings.Default.MainMenuName);

			// run level loading events
			var args = new LevelChangedEventArgs {
				OldLevel = CurrentLevel,
				NewLevel = new Level(null),
			};
			Invoke(OnLevelLoad, args);

			IsValidLevel = true;

			// make sure this won't run again
			hasRunPostInitEvents = true;
			// pause it for the main menu
			Pauser.IsPaused = true;
			// we don't want any input to go while we're in the middle of changing levels
			LKernel.GetG<InputSwallowerManager>().AddSwallower(() => !IsValidLevel, this);
		}

		/// <summary>
		/// Unloads the current level
		/// - Sets IsValidLevel to false
		/// - Runs the levelUnload events
		/// - Tells the kernel to unload all level objects
		/// - Disposes the current level
		/// </summary>
		private void UnloadLevel(LevelChangedEventArgs eventArgs) {
			if (CurrentLevel.Name != null) {
				Launch.Log("======= Level unloading: " + CurrentLevel.Name + " =======");

				IsValidLevel = false;

				//CurrentLevel.Save();

				LKernel.UnloadLevelHandlers();

				// invoke the level unloading events
				Invoke(OnLevelUnload, eventArgs);

				LKernel.Cleanup();

				CurrentLevel.Dispose();
			}
		}

		/// <summary>
		/// Unloads the current level and loads the new level
		/// </summary>
		private void LoadLevelNow(LevelChangedEventArgs args) {
			Level newLevel = args.NewLevel;

			// Unload current level
			UnloadLevel(args);

			CurrentLevel = newLevel;

			// Load new Level
			if (newLevel != null) {
				Launch.Log("======= Level loading: " + newLevel.Name + " =======");
				// load up the world definition from the .muffin file
				newLevel.ReadMuffin();

				// create the enviroment
				newLevel.CreateEnvironment();

				// run our level loading events
				Launch.Log("[Loading] Loading everything else...");
				Invoke(OnLevelLoad, args);

				// then put Things into our world
				newLevel.CreateEntities();
				// then load the rest of the handlers
				LKernel.LoadLevelHandlers(newLevel.Type);

				IsValidLevel = true;

				LKernel.GetG<LuaMain>().LoadScriptFiles(newLevel.Name);
				// run our scripts
				newLevel.RunLevelScripts();

				LKernel.GetG<StaticGeometryManager>().Build();
			}

			// if we're on the main menu, pause it
			if (newLevel.Name == Settings.Default.MainMenuName)
				Pauser.IsPaused = true;

			// last bit of cleanup
			GC.Collect();

			// post load event needs to be delayed
			if (newLevel != null) {
				// reset these
				elapsed = 0;
				frameOneRendered = frameTwoRendered = false;

				// set up our handler
				postLoadFrameStartedHandler = new FrameListener.FrameStartedHandler(
					(fe) => {
						return DelayedRun_FrameStarted(fe, INITIAL_DELAY, (a) => { Invoke(OnLevelPostLoad, a); }, args);
					});
				LKernel.Get<Root>().FrameStarted += postLoadFrameStartedHandler;
			}
		}

		/// <summary>
		/// Loads a level!
		/// </summary>
		/// <param name="newLevelName">The name of the level to load</param>
		/// <param name="delay">
		/// Minimum time to wait (in seconds) before we load the level, to let stuff like loading screens have a chance to render.
		/// Pass 0 to load the new level instantly.
		/// </param>
		public void LoadLevel(string newLevelName, float delay = INITIAL_DELAY) {
			Pauser.IsPaused = false;
			var eventArgs = new LevelChangedEventArgs {
				NewLevel = new Level(newLevelName),
				OldLevel = CurrentLevel,
			};

			// fire our preUnload events
			Console.WriteLine("Pre unload");
			Invoke(OnLevelPreUnload, eventArgs);

			if (delay > 0) {
				// reset these
				elapsed = 0;
				frameOneRendered = frameTwoRendered = false;

				// set up a little frame started handler with our events
				preUnloadFrameStartedHandler = new FrameListener.FrameStartedHandler(
					(fe) => {
						return DelayedRun_FrameStarted(fe, delay, LoadLevelNow, eventArgs);
					});
				LKernel.Get<Root>().FrameStarted += preUnloadFrameStartedHandler;
			}
			else {
				// if our delay is 0, just load the level and don't do any of the delayed stuff
				LoadLevelNow(eventArgs);
			}
		}

		// a little hacky workaround so we can still have a FrameStarted event run but with a few extra arguments
		private FrameListener.FrameStartedHandler preUnloadFrameStartedHandler;
		private FrameListener.FrameStartedHandler postLoadFrameStartedHandler;
		// time to wait until we run the event
		private const float INITIAL_DELAY = 0.2f;
		// used in the FrameStarted method
		private float elapsed = 0;
		/// keeps track of how many frames we've rendered
		private bool frameOneRendered = false, frameTwoRendered = false;

		/// <summary>
		/// Runs something after both the specified time has passed and two frames have been rendered.
		/// </summary>
		/// <param name="evt">Passed in from Root.FrameStarted</param>
		/// <param name="delay">The minimum time that must pass before we run the method</param>
		/// <param name="action">The method to run</param>
		/// <param name="args">Arguments for the method</param>
		/// <param name="handler">The handler to detach after the method has been ran</param>
		private bool DelayedRun_FrameStarted(FrameEvent evt, float delay, Action<LevelChangedEventArgs> action, LevelChangedEventArgs args) {
			if (!frameOneRendered && elapsed > delay) {
				// rendered one frame
				frameOneRendered = true;
				elapsed = delay;
			}
			else if (!frameTwoRendered && elapsed > delay) {
				// rendered two frames
				frameTwoRendered = true;
				elapsed = delay;
			}
			else if (frameTwoRendered && elapsed > delay) {
				// rendered three frames! Detach and run our method
				Detach();
				action(args);
			}

			elapsed += evt.timeSinceLastFrame;
			return true;
		}

		/// <summary>
		/// Unhook from the frame started event
		/// </summary>
		private void Detach() {
			LKernel.Get<Root>().FrameStarted -= preUnloadFrameStartedHandler;
			LKernel.Get<Root>().FrameStarted -= postLoadFrameStartedHandler;
		}

		/// <summary>
		/// helper
		/// </summary>
		private void Invoke(LevelEvent e, LevelChangedEventArgs args) {
			if (e != null)
				e(args);
		}

		/// <summary>
		/// Tells whether this level is valid or not.
		/// </summary>
		public bool IsValidLevel { get; private set; }

		/// <summary>
		/// Returns true if the current level is valid and not a main menu.
		/// Note that it doesn't say anything about whether the level's actually finished loading yet! Use IsValidLevel for that!
		/// </summary>
		public bool IsPlayableLevel {
			get {
				return CurrentLevel != null && CurrentLevel.Type == LevelType.Race && CurrentLevel.Name != Settings.Default.MainMenuName;
			}
		}
	}

}