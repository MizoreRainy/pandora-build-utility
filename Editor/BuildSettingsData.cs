using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace MizoreRainy.Pandora.BuildUtility
{
	#region Build Profile

	/// <summary>
	///     Represents a build profile with specific settings for a build.
	/// </summary>
	[Serializable]
	public class BuildProfile
	{
		#region Public Fields

		public string ProfileName = "";
		public string DisplayName = "";
		public string BuildSuffix = "";
		public string ProductName = "";
		public Texture2D Icon;
		public BuildTarget BuildTarget = BuildTarget.StandaloneWindows64;
		public bool Development;
		public bool ScriptDebugging;
		public bool DefaultDevelopmentBuild;
		public bool DevelopmentBuildEnabled;

		#endregion

		#region Public Methods

		/// <summary>
		///     Called by Unity after deserialization. Sets the development build state from the default if not already set.
		/// </summary>
		public void OnAfterDeserialize()
		{
			if (!DevelopmentBuildEnabled && DefaultDevelopmentBuild) DevelopmentBuildEnabled = DefaultDevelopmentBuild;
		}

		/// <summary>
		///     Initializes the development build checkbox state based on the default setting.
		/// </summary>
		public void InitializeCheckboxState()
		{
			DevelopmentBuildEnabled = DefaultDevelopmentBuild;
		}

		#endregion
	}

	#endregion

	#region Code Name Configuration

	/// <summary>
	///     Stores configuration specific to a code name variant, used in Wildcard Mode.
	/// </summary>
	[Serializable]
	public class CodeNameConfig
	{
		#region Fields

		public string CodeName = "";
		public string ScriptDefineSymbol = "";
		public List<SceneAsset> Scenes = new();
		public List<SceneAsset> BuildScenes = new(); // For backward compatibility
		public List<BuildProfile> BuildProfiles = new();

		#endregion

		#region Public Methods

		/// <summary>
		///     Retrieves the asset paths of all valid scenes for building.
		/// </summary>
		/// <returns>An array of scene paths.</returns>
		public string[] GetScenePaths()
		{
			var scenesToUse = Scenes.Count > 0 ? Scenes : BuildScenes;
			return (from scene in scenesToUse where scene is not null select AssetDatabase.GetAssetPath(scene)).ToArray();
		}

		#endregion
	}

	#endregion

	/// <summary>
	///     A ScriptableObject that stores all build settings for the project,
	///     supporting both single-project and multi-variant (wildcard) modes.
	/// </summary>
	[CreateAssetMenu(fileName = "BuildSettings", menuName = "Pandora/Build Settings", order = 1)]
	[Serializable]
	public class BuildSettingsData : ScriptableObject
	{
		#region Public Fields

		[Header("Project Configuration")] public string ProjectName = "";
		public bool UseWildcardMode;
		public string ProjectCodeName = "NONE";
		public string WildcardProjectName = "";
		public string CurrentVersion = "";
		public string BuildFolderPath = "";
		public List<string> CodeNames = new();

		[Header("Single Project Mode - Scene Configuration")]
		public List<SceneAsset> BuildScenes = new();

		[Header("Single Project Mode - Build Profiles")]
		public List<BuildProfile> BuildProfiles = new();

		[Header("Wildcard Mode - Code Name Configurations")]
		public List<CodeNameConfig> CodeNameConfigs = new();

		[Header("Setup Status")]
		public bool IsSetupComplete;

		#endregion

		#region Unity Events

		/// <summary>
		///     Called when the script is loaded or a value is changed in the Inspector.
		///     Initializes the state of development build checkboxes.
		/// </summary>
		private void OnEnable()
		{
			// Initialize defaults first
			InitializeDefaults();

			foreach (var profile in
			         BuildProfiles.Where(_p => !_p.DevelopmentBuildEnabled && _p.DefaultDevelopmentBuild))
				profile.InitializeCheckboxState();

			foreach (var profile in CodeNameConfigs.SelectMany(_config =>
				         _config.BuildProfiles.Where(_p =>
					         !_p.DevelopmentBuildEnabled && _p.DefaultDevelopmentBuild)))
				profile.InitializeCheckboxState();
		}

		#endregion

		#region Public Methods

		/// <summary>
		///     Gets the effective project name based on whether wildcard mode is active.
		/// </summary>
		/// <returns>The effective project name as a string.</returns>
		public string GetEffectiveProjectName()
		{
			if (UseWildcardMode) return !string.IsNullOrEmpty(WildcardProjectName) ? WildcardProjectName : ProjectName;
			return !string.IsNullOrEmpty(ProjectCodeName) ? ProjectCodeName : ProjectName;
		}

		/// <summary>
		///     Gets the scene paths for building in single project mode.
		/// </summary>
		/// <returns>An array of scene asset paths.</returns>
		public string[] GetScenePaths()
		{
			return BuildScenes.Where(_scene => _scene is not null).Select(AssetDatabase.GetAssetPath).ToArray();
		}

        /// <summary>
        ///     Gets the active code name configuration by checking the current scripting define symbols.
        /// </summary>
        /// <returns>The active <see cref="CodeNameConfig" />, or the first one if none match.</returns>
        public CodeNameConfig GetActiveCodeNameConfig()
		{
			if (!UseWildcardMode) return null;

			var currentDefineSymbols =
				PlayerSettings.GetScriptingDefineSymbols(
					NamedBuildTarget.FromBuildTargetGroup(BuildTargetGroup.Standalone));
			return CodeNameConfigs.FirstOrDefault(_config =>
				       !string.IsNullOrEmpty(_config.ScriptDefineSymbol) &&
				       currentDefineSymbols.Contains(_config.ScriptDefineSymbol)) ??
			       CodeNameConfigs.FirstOrDefault();
		}

        /// <summary>
        ///     Finds a code name configuration by its name.
        /// </summary>
        /// <param name="_codeName">The code name to find.</param>
        /// <returns>The matching <see cref="CodeNameConfig" /> or null if not found.</returns>
        public CodeNameConfig GetCodeNameConfig(string _codeName)
		{
			return CodeNameConfigs.FirstOrDefault(_c =>
				_c.CodeName.Equals(_codeName, StringComparison.OrdinalIgnoreCase));
		}

		/// <summary>
		/// Gets the current version, falling back to PlayerSettings.bundleVersion if CurrentVersion is null or empty.
		/// </summary>
		public string GetCurrentVersion()
		{
			return !string.IsNullOrEmpty(CurrentVersion)
				? CurrentVersion
				: (!string.IsNullOrEmpty(PlayerSettings.bundleVersion)
					? PlayerSettings.bundleVersion
					: "0.0.1");
		}


		/// <summary>
		///     Initializes the settings with default values if they are not already set.
		/// </summary>
		public void InitializeDefaults()
		{
			if (string.IsNullOrEmpty(CurrentVersion))
				CurrentVersion = string.IsNullOrEmpty(PlayerSettings.bundleVersion)
					? "0.0.1"
					: PlayerSettings.bundleVersion;

			if (UseWildcardMode)
			{
				if (CodeNameConfigs.Count == 0) SetupDefaultCodeNameConfigs();
			}
			else
			{
				if (BuildProfiles.Count == 0) SetupDefaultProfiles();
			}
		}

		/// <summary>
		///     Sets up default build profiles (Production, Development) for single project mode.
		/// </summary>
		public void SetupDefaultProfiles()
		{
			BuildProfiles.Clear();
			BuildProfiles.Add(GetDefaultProductionProfile(GetEffectiveProjectName()));
			BuildProfiles.Add(GetDefaultDevelopmentProfile(GetEffectiveProjectName()));
		}

		/// <summary>
		///     Sets up default configurations for each code name in wildcard mode.
		/// </summary>
		public void SetupDefaultCodeNameConfigs()
		{
			CodeNameConfigs.Clear();
			foreach (var codeName in CodeNames.Where(_cn => !string.IsNullOrEmpty(_cn)))
			{
				var codeNameConfig = new CodeNameConfig
				{
					CodeName = codeName,
					ScriptDefineSymbol = codeName.ToUpper().Replace(" ", "_"),
					Scenes = new List<SceneAsset>(),
					BuildScenes = new List<SceneAsset>(),
					BuildProfiles = new List<BuildProfile>()
				};
				SetupDefaultProfilesForCodeName(codeNameConfig, codeName);
				CodeNameConfigs.Add(codeNameConfig);
			}
		}

		/// <summary>
		///     Sets up default build profiles for a specific code name configuration.
		/// </summary>
		/// <param name="_codeNameConfig">The configuration to add profiles to.</param>
		/// <param name="_codeName">The code name to use for the profiles.</param>
		public void SetupDefaultProfilesForCodeName(CodeNameConfig _codeNameConfig, string _codeName)
		{
			_codeNameConfig.BuildProfiles.Add(GetDefaultProductionProfile(_codeName));
			_codeNameConfig.BuildProfiles.Add(GetDefaultDevelopmentProfile(_codeName));
		}

        /// <summary>
        ///     Creates a default production build profile.
        /// </summary>
        /// <param name="_projectCodeName">The code name for the project.</param>
        /// <returns>A new production <see cref="BuildProfile" />.</returns>
        public static BuildProfile GetDefaultProductionProfile(string _projectCodeName)
		{
			return new BuildProfile
			{
				ProfileName = $"{_projectCodeName} Production",
				DisplayName = "Production",
				BuildSuffix = "prd",
				ProductName = _projectCodeName,
				BuildTarget = BuildTarget.StandaloneWindows64,
				Development = false,
				ScriptDebugging = false,
				DefaultDevelopmentBuild = false
			};
		}

        /// <summary>
        ///     Creates a default development build profile.
        /// </summary>
        /// <param name="_projectCodeName">The code name for the project.</param>
        /// <returns>A new development <see cref="BuildProfile" />.</returns>
        public static BuildProfile GetDefaultDevelopmentProfile(string _projectCodeName)
		{
			return new BuildProfile
			{
				ProfileName = $"{_projectCodeName} Development",
				DisplayName = "Development",
				BuildSuffix = "dev",
				ProductName = _projectCodeName,
				BuildTarget = BuildTarget.StandaloneWindows64,
				Development = true,
				ScriptDebugging = true,
				DefaultDevelopmentBuild = true
			};
		}

		/// <summary>
		///     Adds a new code name configuration.
		/// </summary>
		/// <param name="_codeName">The new code name.</param>
		/// <param name="_scriptDefineSymbol">The associated scripting define symbol.</param>
		public void AddCodeNameConfig(string _codeName, string _scriptDefineSymbol)
		{
			if (CodeNameConfigs.Any(_c => _c.CodeName.Equals(_codeName, StringComparison.OrdinalIgnoreCase)))
			{
				Debug.LogWarning($"Code name '{_codeName}' already exists!");
				return;
			}

			var newConfig = new CodeNameConfig
			{
				CodeName = _codeName,
				ScriptDefineSymbol = _scriptDefineSymbol,
				Scenes = new List<SceneAsset>(),
				BuildScenes = new List<SceneAsset>(),
				BuildProfiles = new List<BuildProfile>()
			};

			SetupDefaultProfilesForCodeName(newConfig, _codeName);
			CodeNameConfigs.Add(newConfig);

			if (!CodeNames.Contains(_codeName)) CodeNames.Add(_codeName);
		}

		/// <summary>
		///     Removes a code name configuration by its name.
		/// </summary>
		/// <param name="_codeName">The code name to remove.</param>
		public void RemoveCodeNameConfig(string _codeName)
		{
			CodeNameConfigs.RemoveAll(_c => _c.CodeName.Equals(_codeName, StringComparison.OrdinalIgnoreCase));
			CodeNames.RemoveAll(_c => _c.Equals(_codeName, StringComparison.OrdinalIgnoreCase));
		}

		#endregion
	}
}