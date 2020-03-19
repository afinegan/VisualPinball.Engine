﻿// ReSharper disable MemberCanBeMadeStatic.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMember.Global

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using VisualPinball.Engine.Common;
using Registry = Microsoft.Win32.Registry;

namespace VisualPinball.Engine.PinMame
{
	/// <summary>
	/// PinMAME, a pinball ROM emulator.
	/// </summary>
	///
	/// <remarks>
	/// Note this doesn't use Visual PinMAME but is based on a cross-platform
	/// library.
	/// </remarks>
	public class PinMame
	{
		public bool IsRunning => _isRunning;

		private int _gameIndex = -1;
		private DmdDimensions _dmd;
		private byte[] _frame;
		private int[] _changedLamps;
		private int[] _changedSolenoids;
		private int[] _changedGIs;
		private bool _isRunning;

		private static PinMame _instance;
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		/// <summary>
		/// Creates or retrieves the PinMame instance.
		/// </summary>
		/// <param name="sampleRate">Audio sample rate</param>
		/// <param name="vpmPath">Fallback path for VPM folder, if VPM is not registered</param>
		/// <exception cref="InvalidOperationException">If VPM cannot be found</exception>
		public static PinMame Instance(int sampleRate = 48000, string vpmPath = null) =>
			_instance ?? (_instance = new PinMame(sampleRate, vpmPath));

		private PinMame(int sampleRate, string vpmPath)
		{
			var path = GetVpmPath() ?? vpmPath;
			if (path == null) {
				throw new InvalidOperationException("Could not determine VPM path. Either install VPinMAME or provide it manually.");
			}
			if (!Directory.Exists(path)) {
				throw new InvalidOperationException("Could not find VPM path, " + path + " does not exist.");
			}
			PinMameApi.SetVPMPath(path + @"\");
			PinMameApi.SetSampleRate(sampleRate);
		}

		/// <summary>
		/// Starts a new game.
		/// </summary>
		/// <param name="gameName">Name of the game, e.g. "tz_94h"</param>
		/// <param name="timeout">Timeout in milliseconds to wait for game to start</param>
		/// <param name="showConsole">If true, open PinMAME console</param>
		/// <exception cref="InvalidOperationException">If there is already a game running.</exception>
		/// <exception cref="ArgumentException">If the game name is invalid.</exception>
		/// <exception cref="TimeoutException">If game did not start in time.</exception>
		public async Task StartGame(string gameName, int timeout = 5000, bool showConsole = false)
		{
			if (_isRunning) {
				throw new InvalidOperationException("Game is running, must stop first.");
			}

			_gameIndex = PinMameApi.StartThreadedGame(gameName, showConsole);

			if (_gameIndex < 0) {
				throw new ArgumentException("Unknown game \"" + gameName + "\".");
			}

			const int sleep = 10;
			await Task.Run(() => {
				var n = timeout / sleep;
				var i = 0;
				while (i++ < n && !PinMameApi.IsGameReady()) {
					Thread.Sleep(sleep);
				}
			});

			if (!PinMameApi.IsGameReady()) {
				throw new TimeoutException("Timed out waiting for game to start.");
			}

			_isRunning = true;
			_dmd = GetDmdDimensions();
			_frame = new byte[_dmd.Width * _dmd.Height];
			_changedLamps = new int[GetMaxLamps() * 2];
			_changedSolenoids = new int[GetMaxSolenoids() * 2];
			_changedGIs = new int[GetMaxGIs() * 2];
		}

		public void StopGame()
		{
			_isRunning = false;
			PinMameApi.StopThreadedGame(true);
		}

		public void ResetGame()
		{
			PinMameApi.ResetGame();
		}

		/// <summary>
		/// Returns the dimensions of the DMD in pixels.
		/// </summary>
		/// <exception cref="InvalidOperationException">If there is no game running.</exception>
		/// <returns>Dimensions</returns>
		public DmdDimensions GetDmdDimensions()
		{
			AssertRunningGame();
			return new DmdDimensions(PinMameApi.GetRawDMDWidth(), PinMameApi.GetRawDMDHeight());
		}

		/// <summary>
		/// Returns whether the DMD changed since the pixels were last
		/// retrieved.
		/// </summary>
		/// <exception cref="InvalidOperationException">If there is no game running.</exception>
		/// <returns>True if DMD changed, false otherwise.</returns>
		public bool NeedsDmdUpdate()
		{
			AssertRunningGame();
			return PinMameApi.NeedsDMDUpdate();
		}

		/// <summary>
		/// Returns the pixels of the DMD.
		/// </summary>
		/// <returns>Current DMD frame</returns>
		/// <exception cref="InvalidOperationException">If there is no game running.</exception>
		/// <exception cref="InvalidOperationException">If retrieving the pixels failed otherwise.</exception>
		public byte[] GetDmdPixels()
		{
			var res = PinMameApi.GetRawDMDPixels(_frame);
			if (res < 0) {
				throw new InvalidOperationException($"Got {res} from GetRawDMDPixels().");
			}
			return _frame;
		}

		/// <summary>
		/// Returns the state of a given switch.
		/// </summary>
		/// <param name="slot">Slot number of the switch</param>
		/// <returns>Value of the switch</returns>
		public bool GetSwitch(int slot) => PinMameApi.GetSwitch(slot);

		/// <summary>
		/// Sets the state of a given switch.
		/// </summary>
		/// <param name="slot">Slot number of the switch</param>
		/// <param name="state">New value of the switch</param>
		public void SetSwitch(int slot, bool state) => PinMameApi.SetSwitch(slot, state);

		/// <summary>
		/// Returns the maximal supported number of lamps.
		/// </summary>
		/// <returns>Number of lamps</returns>
		public int GetMaxLamps() => PinMameApi.GetMaxLamps();

		/// <summary>
		/// Returns an array of all changed lamps since the last call. <p/>
		///
		/// The returned array contains pairs, where the first element is the
		/// lamp number, and the second element the value.
		/// </summary>
		public Span<int> GetChangedLamps()
		{
			var num = PinMameApi.GetChangedLamps(_changedLamps);
			return _changedLamps.AsSpan().Slice(0, num * 2);
		}

		/// <summary>
		/// Returns the maximal supported number of solenoids.
		/// </summary>
		/// <returns>Number of solenoids</returns>
		public int GetMaxSolenoids() => PinMameApi.GetMaxSolenoids();

		/// <summary>
		/// Returns an array of all changed solenoids since the last call. <p/>
		///
		/// The returned array contains pairs, where the first element is the
		/// solenoid number, and the second element the value.
		/// </summary>
		public Span<int> GetChangedSolenoids()
		{
			var num = PinMameApi.GetChangedSolenoids(_changedSolenoids);
			return _changedSolenoids.AsSpan().Slice(0, num * 2);
		}

		/// <summary>
		/// Returns the maximal supported number of GIs.
		/// </summary>
		/// <returns>Number of GIs</returns>
		public int GetMaxGIs() => PinMameApi.GetMaxGIStrings();

		/// <summary>
		/// Returns an array of all changed GIs since the last call. <p/>
		///
		/// The returned array contains pairs, where the first element is the
		/// GI number, and the second element the value.
		/// </summary>
		public Span<int> GetChangedGIs()
		{
			var num = PinMameApi.GetChangedGIs(_changedGIs);
			return _changedGIs.AsSpan().Slice(0, num * 2);
		}

		private void AssertRunningGame()
		{
			if (!_isRunning) {
				throw new InvalidOperationException("Game must be running.");
			}
		}

		private static string GetVpmPath()
		{
			try {
				var reg = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Classes\VPinMAME.Controller\CLSID");
				if (reg != null) {
					var clsId = reg.GetValue(null).ToString();
					var x64Suffix = OsUtil.Is64BitOperatingSystem ? @"WOW6432Node\" : "";
					reg = Registry.ClassesRoot.OpenSubKey(x64Suffix + @"CLSID\" + clsId + @"\InprocServer32");
					if (reg != null) {
						return Path.GetDirectoryName(reg.GetValue(null).ToString());
					}
					Logger.Warn($"Could not find CLSID {clsId} of VPinMAME.dll.");

				} else {
					Logger.Warn("Looks like VPinMAME.Controller is not registered on the system!");
				}
			} catch (Exception e) {
				Logger.Error("ERROR: " + e);
			}
			return null;
		}
	}

	public struct DmdDimensions
	{
		/// <summary>
		/// Width in pixels
		/// </summary>
		public readonly int Width;

		/// <summary>
		/// Height in pixels
		/// </summary>
		public readonly int Height;

		public DmdDimensions(int width, int height)
		{
			Width = width;
			Height = height;
		}
	}
}
