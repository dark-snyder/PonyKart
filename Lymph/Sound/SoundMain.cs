﻿using IrrKlang;
using Lymph.Actors;
using Lymph.Levels;
using Lymph.Phys;
using Mogre;
using Mogre.PhysX;
using Math = Mogre.Math;

/* http://www.ogre3d.org/tikiwiki/MogreFreeSL
 * 
 * PROTIPS: 
 * 
 * - The Reference Distance is the distance away from the camera at which the
 *   SoundObject's volume starts decreasing, and the Maximum Distance is the distance
 *   from the camera at which the SoundObject will be at its quietest.
 */
namespace Lymph.Sound {
	public class SoundMain {
		bool quit = false;
		ISound music;
		public ISoundEngine Engine { get; private set; }

		/// <summary>
		/// The sound manager class.
		/// </summary>
		public SoundMain() {
			Launch.Log("[Loading] Creating IrrKlang and SoundMain...");
			var levelManager = LKernel.Get<LevelManager>();

			levelManager.OnLevelLoad += OnLevelLoad;
			levelManager.OnLevelUnload += (ea) => Engine.RemoveAllSoundSources();

			LKernel.Get<Root>().FrameStarted += FrameStarted;

			LKernel.Get<ContactReporter>().AddEvent(Groups.CollidableNonPushableID, Groups.CollidablePushableID, HandleAntibodyCollision);

			Engine = new ISoundEngine();
			Engine.Default3DSoundMinDistance = 2;
			RolloffFactor = 3;

			Launch.Log("[Loading] IrrKlang and SoundMain initialised!");
		}

		#region Level un/loading stuff
		/// <summary>
		/// Runs whenever a new level is loaded.
		/// </summary>
		void OnLevelLoad(LevelChangedEventArgs eventArgs) {
			if (LKernel.Get<LevelManager>().CurrentLevel.Name == "Level1")
				music = CreateAmbientSound("media/sound/Casey LaLonde - Danced In The Alleyway.ogg", "bgmusic", true);
			else
				music = CreateAmbientSound("media/sound/Renard - Breathe In Time.ogg", "bgmusic", true);
		}
		#endregion

		readonly Vector3 lookDir = new Vector3(0, -1, 0);
		private Vector3 pos;
		private float timesince = 0;

		// only need to update this twice a second
		bool FrameStarted(FrameEvent evt) {
			if (!LKernel.Get<LevelManager>().IsValidLevel)
				return true;

			timesince += evt.timeSinceLastFrame;
			if (timesince > 0.5f) {
				timesince = 0;
				pos = LKernel.Get<Player>().Node.Position;
				Engine.SetListenerPosition(pos.x, pos.y, pos.z, lookDir.x, lookDir.y, lookDir.z);
				Engine.Update();
			}
			return !quit;
		}

		/// <summary>
		/// responds to the enemy-projectile event
		/// </summary>
		void HandleAntibodyCollision(ContactPair pair, ContactPairFlags flags) {
			int rand = (int) Math.RangeRandom(0, 10) + 1;
			// check to make sure one of these is an antibody
			Antibody source = pair.ActorFirst.UserData as Antibody;
			if (source == null)
				// actorfirst is not an antibody, so lets try actorsecond
				source = pair.ActorSecond.UserData as Antibody;
			
			if (source != null)
				// actorsecond was the antibody
				CreateObjectSound("media/sound/pop" + (rand > 10 ? 10 : rand) + ".wav", source.Node.Position, source.Node.Name, false);
		}

		#region Sound object creation
		/// <summary>
		/// Creates an ambient sound. These have no 3D position or effects or anything, so this is ideal for level music and whatnot.
		/// Also hooks in a stop event receiver so the sound disposes of itself when it's finished playing.
		/// </summary>
		/// <param name="filePath">The file path of the sound you want to play.</param>
		/// <param name="objectName">The name of the sound object</param>
		/// <param name="looping">Make this sound loop?</param>
		/// <returns>The ISound you just created</returns>
		public ISound CreateAmbientSound(string filePath, string objectName, bool looping) {
			if (!Constants.MUSIC)
				return null;
			Launch.Log("[Sounds] Creating ambient sound: " + filePath + " Looping: " + looping);
			ISound sound = Engine.Play2D(filePath, looping);
			return sound;
		}

		/// <summary>
		/// Creates an object sound. These sounds do have a 3D position and are attached to SceneNodes. Use these for sound effects and stuff.
		/// Also hooks in a stop event receiver so the sound disposes of itself when it's finished playing.
		/// </summary>
		/// <param name="filePath">The file path of the sound you want to play.</param>
		/// <param name="pos">The Position you want this sound to play at.</param>
		/// <param name="name">The name of the node or whatever. Is optional.</param>
		/// <param name="looping">Make this sound loop?</param>
		/// <returns>The ISound you just created</returns>
		public ISound CreateObjectSound(string filePath, Vector3 pos, string name, bool looping) {
			if (!Constants.SOUNDS || pos == null)
				return null;
			Launch.Log("[Sounds] Creating object sound: " + filePath + " Node: " + name + " Looping: " + looping);
			ISound sound = Engine.Play3D(filePath, pos.x, pos.y, pos.z, looping);
			return sound;
		}

		/// <summary>
		/// Creates an object sound. These sounds do have a 3D position and are attached to SceneNodes. Use these for sound effects and stuff.
		/// Also hooks in a stop event receiver so the sound disposes of itself when it's finished playing.
		/// </summary>
		/// <param name="filePath">The file path of the sound you want to play.</param>
		/// <param name="pos">The Position you want this sound to play at.</param>
		/// <param name="looping">Make this sound loop?</param>
		/// <returns>The ISound you just created</returns>
		public ISound CreateObjectSound(string filePath, Vector3 pos, bool looping) {
			return CreateObjectSound(filePath, pos, "(unspecified)", looping);
		}
		#endregion


		#region Properties
		float rolloff;
		/// <summary>
		/// 0 = no rolloff
		/// 1 = default rolloff
		/// </summary>
		public float RolloffFactor {
			get {
				return rolloff;
			}
			set {
				rolloff = value;
				Engine.SetRolloffFactor(value);
			}
		}
		#endregion
	}
}