using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using Debug = UnityEngine.Debug;

// ReSharper disable once CheckNamespace
namespace MizoreRainy.Pandora.BuildUtility
{
	#region Enums

	/// <summary>
	/// Represents the type of versioning for a specific entity or system.
	/// </summary>
	/// <remarks>
	/// This enumeration can be used to categorize different types of versioning,
	/// such as development stages or release distinctions.
	/// </remarks>
	public enum VersionType
	{
		/// <summary>
		/// Represents a major version type in semantic versioning.
		/// Indicates significant changes or updates that may include
		/// backward-incompatible modifications or new features.
		/// </summary>
		Major,

		/// <summary>
		/// Represents a version type indicating a minor version update.
		/// This typically signifies the addition of new features or functionality
		/// that are backward-compatible with the existing major version.
		/// </summary>
		Minor,

		/// <summary>
		/// Represents a version type that indicates a patch release.
		/// A patch version is typically used to address bug fixes,
		/// minor improvements, or other small changes that do not
		/// introduce new features or breaking changes.
		/// </summary>
		Patch
	}

	#endregion

	/// <summary>
	/// A utility class designed to streamline and facilitate operations related to build settings.
	/// This class provides methods to handle and validate various build configurations.
	/// </summary>
	public class BuildSettingsUtility : EditorWindow
	{
		#region Const Fields

		/// <summary>
		/// Represents the file path to the configuration settings file used
		/// by the application. This variable stores the absolute or relative
		/// path of the settings file and is used to load or save application
		/// configuration data.
		/// </summary>
		private const string _SETTING_FILE_PATH = "Assets/Pandora/Settings/BuildSettings.asset";

		#endregion

		#region Private Fields

		/// <summary>
		/// Represents the build settings data configuration used for setting up
		/// or managing build parameters within a project. This variable typically
		/// stores information related to build configurations, such as platform
		/// targets, build modes, or custom settings required for the build process.
		/// </summary>
		private BuildSettingsData _BuildSettingsData;

		/// <summary>
		/// Represents the current scroll position within a scrollable container or control.
		/// </summary>
		/// <remarks>
		/// This variable is typically used to track and manage the vertical or horizontal
		/// scroll offset in UI components or other scrollable interfaces.
		/// </remarks>
		private Vector2 _ScrollPosition;

		/// <summary>
		/// Indicates whether the setup process is complete for the build settings utility.
		/// </summary>
		/// <remarks>
		/// This variable controls the flow of the `BuildSettingsUtility`'s functionality,
		/// particularly determining whether the setup wizard needs to be displayed. When set
		/// to <c>false</c>, the setup wizard will be initiated, allowing the user to configure
		/// the build settings. Once the setup is finalized, this variable will be updated to
		/// <c>true</c> to bypass the wizard and enable access to the main functionality.
		/// </remarks>
		private bool _IsSetupComplete;

		/// <summary>
		/// Represents a specific step or phase in a wizard workflow sequence.
		/// This variable can be used to define or track the current step
		/// within the multi-step wizard process.
		/// </summary>
		private int _WizardStep = 1;

		/// <summary>
		/// Specifies whether the wizard operates in wildcard mode, allowing for more flexible input handling.
		/// </summary>
		private bool _WizardUseWildcardMode;

		/// <summary>
		/// Represents the name of the wizard project used for initialization
		/// or configuration purposes within the application.
		/// </summary>
		private string _WizardProjectName = "";

		/// <summary>
		/// Represents a collection of code names associated with wizards.
		/// </summary>
		/// <remarks>
		/// This variable is intended to store identifiers or aliases used to distinguish different wizards
		/// within a specific context or application. It can be used for various purposes, such as mapping
		/// wizards to their corresponding functionalities or roles.
		/// </remarks>
		private List<string> _WizardCodeNames = new();

		/// <summary>
		/// Represents the file system path to the directory where the wizard build artifacts
		/// or related temporary files are stored. This variable is intended to be used for
		/// accessing the folder associated with the wizard's build operations.
		/// </summary>
		private string _WizardBuildFolderPath = "";

		/// <summary>
		/// Represents the index of the selected code name within the wizard's context.
		/// This variable is used to track the currently chosen option or selection
		/// during the execution of a wizard interface or process.
		/// </summary>
		private int _WizardSelectedCodeNameIndex;

		/// <summary>
		/// A dictionary that maps each wizard scene's code name to its corresponding scene object.
		/// </summary>
		/// <remarks>
		/// This variable is used to quickly retrieve wizard scene objects based on their unique code names,
		/// making scene management more efficient and organized.
		/// </remarks>
		private readonly Dictionary<string, List<SceneAsset>> _WizardScenesByCodeName = new();

		/// <summary>
		/// Stores a mapping of wizard profiles indexed by their respective code names.
		/// </summary>
		private readonly Dictionary<string, List<BuildProfile>> _WizardProfilesByCodeName = new();

		/// <summary>
		/// Represents a collection of scene configurations intended for a single mode operation in the wizard setup process.
		/// </summary>
		private readonly List<SceneAsset> _WizardSingleModeScenes = new();

		/// <summary>
		/// Stores the profiles associated with the wizard's single mode configuration.
		/// </summary>
		private readonly List<BuildProfile> _WizardSingleModeProfiles = new();

		/// <summary>
		/// Represents the index of the selected code name in a collection or list.
		/// </summary>
		private int _SelectedCodeNameIndex;

		/// <summary>
		/// Represents the placeholder or identifier for the next version of the software or application.
		/// This variable is used to track or denote the upcoming version during development processes.
		/// </summary>
		private string _NextVersion = "";

		/// <summary>
		/// Represents a collection or dictionary that tracks the folded or expanded state
		/// of various profile sections or categories within a user interface. This structure
		/// is commonly used to persist and manage the visual state (collapsed or expanded)
		/// of UI components for user profiles or similar hierarchical data.
		/// </summary>
		private readonly Dictionary<string, bool> _ProfileFoldStates = new();

		#endregion

		#region Public Methods

		/// <summary>
		/// Retrieves the path to the build folder based on the specified configuration or context.
		/// </summary>
		/// <returns>
		/// A string representing the full path to the build folder.
		/// </returns>
		public string GetBuildFolderPath()
		{
			return _BuildSettingsData?.BuildFolderPath ?? GetDefaultBuildFolder();
		}

		/// <summary>
		/// Sets the folder path where the build output will be stored.
		/// </summary>
		/// <param name="_path">The folder path to set as the build destination. This should be a valid directory path.</param>
		public void SetBuildFolderPath(string _path)
		{
			if (_BuildSettingsData is not null)
			{
				_BuildSettingsData.BuildFolderPath = _path;
				SaveSettings();
			}
		}

		/// <summary>
		/// Opens the Build Settings window in the Unity Editor.
		/// </summary>
		[MenuItem("Pandora/Build Settings")]
		public static void ShowWindow()
		{
			GetWindow<BuildSettingsUtility>("Build Settings");
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Retrieves the path to the default build folder.
		/// </summary>
		private string GetDefaultBuildFolder()
		{
			return Path.Combine(Application.dataPath, "..", "Builds");
		}

		#endregion

		#region Unity Events

		/// <summary>
		/// Unity calls this method automatically when the script or object it's attached to is enabled.
		/// Use this method to initialize variables, set up state, or register event listeners.
		/// OnEnable is invoked before any of the Update methods for the script are called.
		/// </summary>
		/// <remarks>
		/// If the object is disabled and then re-enabled, OnEnable will be triggered again.
		/// It is commonly used to subscribe to events or reinitialize fields.
		/// </remarks>
		private void OnEnable()
		{
			LoadOrCreateSettings();
			EditorApplication.update += OnEditorUpdate;
		}

		/// <summary>
		/// This method is called when the MonoBehaviour becomes disabled or inactive.
		/// It is invoked automatically by Unity and can be used to perform any necessary
		/// cleanup or to stop processes that were started in OnEnable or during the
		/// lifetime of the object. Use this method to release resources, unsubscribe from
		/// events, or stop coroutines to ensure proper behavior of your application.
		/// </summary>
		private void OnDisable()
		{
			EditorApplication.update -= OnEditorUpdate;
		}

		/// <summary>
		/// Executes tasks or operations that need to be performed every time the
		/// editor updates. This method is typically called during the editor's
		/// update cycle, allowing runtime modifications or behaviors to be reflected
		/// in the Unity Editor.
		/// </summary>
		private void OnEditorUpdate()
		{
			// Force repaint when compilation state changes
			if (EditorApplication.isCompiling) Repaint();
		}

		/// <summary>
		/// Method invoked to handle GUI events in Unity.
		/// It is called automatically when the GUI system processes events.
		/// Typically used to define and render custom GUI controls or handle
		/// immediate-mode GUI functionality.
		/// </summary>
		/// <remarks>
		/// This method is called multiple times per frame, such as
		/// during layout and repaint events. It is important to
		/// include checks and use GUI-related APIs like GUILayout or GUI
		/// to define the interface, handle events, and avoid unnecessary overhead.
		/// </remarks>
		/// <example>
		/// This method can be used to create custom buttons, labels,
		/// and other interactive GUI elements using the Unity GUI system.
		/// </example>
		private void OnGUI()
		{
			// Check if Unity is compiling
			var isCompiling = EditorApplication.isCompiling;

			if (isCompiling)
			{
				GUI.enabled = false;
				EditorGUILayout.BeginVertical("box");
				GUILayout.Label("Unity is compiling...", EditorStyles.boldLabel);
				EditorGUILayout.HelpBox("Please wait for compilation to complete before using Build Settings.",
					MessageType.Info);
				EditorGUILayout.EndVertical();
				GUI.enabled = true;
				return;
			}

			if (!_IsSetupComplete)
			{
				DrawSetupWizard();
				return;
			}

			_ScrollPosition = EditorGUILayout.BeginScrollView(_ScrollPosition);

			// Add a Re-setup button
			if (GUILayout.Button("Re-run Setup Wizard"))
				if (EditorUtility.DisplayDialog("Re-run Setup Wizard",
					    "This will reset all current settings. Are you sure?", "Yes", "Cancel"))
					ResetToWizard();

			GUILayout.Space(10);

			if (_BuildSettingsData.UseWildcardMode)
				DrawWildcardModeUI();
			else
				DrawSingleModeUI();

			EditorGUILayout.EndScrollView();
		}

		#endregion

		#region Wizard Methods

		/// <summary>
		/// Resets the build settings to the initial wizard state for reconfiguration.
		/// </summary>
		/// <remarks>
		/// This method clears all current settings, resets the wizard step, and reinitialized
		/// related fields to their default values to allow re-running the setup wizard.
		/// </remarks>
		private void ResetToWizard()
		{
			_IsSetupComplete = false;
			_WizardStep = 1;
			_WizardUseWildcardMode = false;
			_WizardProjectName = "";
			_WizardCodeNames.Clear();
			_WizardBuildFolderPath = "";
			_WizardSelectedCodeNameIndex = 0;
			_WizardScenesByCodeName.Clear();
			_WizardProfilesByCodeName.Clear();
			_WizardSingleModeScenes.Clear();
			_WizardSingleModeProfiles.Clear();

			if (_BuildSettingsData is not null)
			{
				_BuildSettingsData.IsSetupComplete = false;
				SaveSettings();
			}
		}

		/// <summary>
		/// Opens and initializes the setup wizard for the application.
		/// </summary>
		/// <remarks>
		/// This method launches a guided setup wizard designed to assist users
		/// in configuring the application. It may involve collecting user inputs,
		/// verifying settings, and storing configurations required for proper functionality.
		/// </remarks>
		private void DrawSetupWizard()
		{
			GUILayout.Label("Build Settings Setup Wizard", EditorStyles.boldLabel);
			GUILayout.Space(10);

			// Add a scroll view for the wizard content
			_ScrollPosition = EditorGUILayout.BeginScrollView(_ScrollPosition);

			switch (_WizardStep)
			{
				case 1:
					DrawWizardStep1();
					break;
				case 2:
					DrawWizardStep2();
					break;
				case 3:
					DrawWizardStep3();
					break;
				case 4:
					if (_WizardUseWildcardMode)
						DrawWizardStep4Wildcard();
					else
						DrawWizardStep4();
					break;
				case 5:
					if (_WizardUseWildcardMode)
						DrawWizardStep5Wildcard();
					else
						DrawWizardStep5();
					break;
				case 6:
					DrawWizardComplete();
					break;
			}

			EditorGUILayout.EndScrollView();
		}


		/// <summary>
		/// Renders the first step of the build settings setup wizard GUI,
		/// which involves configuring basic project options and toggling wildcard mode.
		/// </summary>
		private void DrawWizardStep1()
		{
			GUILayout.Label("Step 1: Project Configuration", EditorStyles.boldLabel);

			_WizardUseWildcardMode = EditorGUILayout.Toggle("Use Wildcard Mode", _WizardUseWildcardMode);

			EditorGUILayout.HelpBox(
				_WizardUseWildcardMode
					? "Wildcard mode allows multiple code names (variants) of the same project."
					: "Single mode for projects with one code name.", MessageType.Info);

			GUILayout.Space(20);

			if (GUILayout.Button("Next")) _WizardStep = 2;
		}

		/// <summary>
		/// Executes the logic required to display and handle the operations for the second step of the wizard.
		/// </summary>
		/// <remarks>
		/// This method is part of a multi-step wizard interface and is responsible for managing the functionality
		/// of the second step. It may include rendering UI elements, validating input, or processing data specific to this step.
		/// Ensure that any dependencies or prerequisites required for this step are satisfied before execution.
		/// </remarks>
		private void DrawWizardStep2()
		{
			GUILayout.Label("Step 2: Project Name & Code Names", EditorStyles.boldLabel);

			_WizardProjectName = EditorGUILayout.TextField("Project Name", _WizardProjectName);

			GUILayout.Space(10);

			if (_WizardUseWildcardMode)
			{
				GUILayout.Label("Code Names:", EditorStyles.boldLabel);

				for (var i = 0; i < _WizardCodeNames.Count; i++)
				{
					EditorGUILayout.BeginHorizontal();
					_WizardCodeNames[i] = EditorGUILayout.TextField($"Code Name {i + 1}", _WizardCodeNames[i]);
					if (GUILayout.Button("Remove", GUILayout.Width(60)))
					{
						_WizardCodeNames.RemoveAt(i);
						i--;
					}

					EditorGUILayout.EndHorizontal();
				}

				if (GUILayout.Button("Add Code Name")) _WizardCodeNames.Add("");
			}
			else
			{
				// Single mode - just one code name
				if (_WizardCodeNames.Count == 0)
					_WizardCodeNames.Add(_WizardProjectName);

				if (_WizardCodeNames.Count > 0)
					_WizardCodeNames[0] = EditorGUILayout.TextField("Code Name", _WizardCodeNames[0]);
			}

			GUILayout.Space(20);

			EditorGUILayout.BeginHorizontal();
			if (GUILayout.Button("Back")) _WizardStep = 1;

			var canProceed = !string.IsNullOrEmpty(_WizardProjectName) &&
			                 _WizardCodeNames.Count > 0 &&
			                 _WizardCodeNames.All(_name => !string.IsNullOrEmpty(_name));

			GUI.enabled = canProceed;
			if (GUILayout.Button("Next")) _WizardStep = 3;
			GUI.enabled = true;
			EditorGUILayout.EndHorizontal();
		}

		/// <summary>
		/// Renders the interface for the third step of the build settings wizard,
		/// which allows the user to select and validate a build folder path.
		/// </summary>
		/// <remarks>
		/// This method provides UI elements for selecting a build folder path,
		/// including text input, a folder browser dialog, and a button to reset
		/// the path to a default value. It also validates the selected path to
		/// ensure it exists and can be used for the build process, providing
		/// appropriate feedback to the user.
		/// </remarks>
		private void DrawWizardStep3()
		{
			GUILayout.Label("Step 3: Build Folder", EditorStyles.boldLabel);

			EditorGUILayout.BeginHorizontal();
			_WizardBuildFolderPath = EditorGUILayout.TextField("Build Folder Path", _WizardBuildFolderPath);
			if (GUILayout.Button("Browse", GUILayout.Width(60)))
			{
				var selectedPath = EditorUtility.OpenFolderPanel("Select Build Folder", _WizardBuildFolderPath, "");
				if (!string.IsNullOrEmpty(selectedPath)) _WizardBuildFolderPath = selectedPath;
			}

			EditorGUILayout.EndHorizontal();

			if (GUILayout.Button("Use Default")) _WizardBuildFolderPath = GetDefaultBuildFolder();

			// Validation
			var isBuildFolderValid = ValidateStep3();

			if (string.IsNullOrEmpty(_WizardBuildFolderPath))
			{
				EditorGUILayout.HelpBox("Please select a build folder path", MessageType.Warning);
			}
			else if (!Directory.Exists(_WizardBuildFolderPath))
			{
				EditorGUILayout.HelpBox(
					"Build folder path does not exist. Please create it or select an existing folder.",
					MessageType.Error);

				if (GUILayout.Button("Create Folder", GUILayout.Width(100)))
					try
					{
						Directory.CreateDirectory(_WizardBuildFolderPath);
						Debug.Log($"Created build folder: {_WizardBuildFolderPath}");
					}
					catch (Exception ex)
					{
						Debug.LogError($"Failed to create folder: {ex.Message}");
						EditorUtility.DisplayDialog("Error", $"Failed to create folder:\n{ex.Message}", "OK");
					}
			}
			else
			{
				EditorGUILayout.HelpBox("Build folder path is valid", MessageType.Info);
			}

			GUILayout.Space(20);

			EditorGUILayout.BeginHorizontal();
			if (GUILayout.Button("Back")) _WizardStep = 2;

			GUI.enabled = isBuildFolderValid;
			if (GUILayout.Button("Next")) _WizardStep = 4;
			GUI.enabled = true;
			EditorGUILayout.EndHorizontal();
		}

		/// <summary>
		/// Executes the logic for rendering and handling the user interface
		/// and processes required during the fourth step of a wizard feature.
		/// </summary>
		/// <remarks>
		/// This method is responsible for managing the specific actions, validations,
		/// and state transitions associated with step four of the wizard sequence.
		/// Ensure that all necessary preconditions for this step have been met before calling this method.
		/// Changes in UI or state should comply with the associated wizard workflow.
		/// </remarks>
		private void DrawWizardStep4()
		{
			GUILayout.Label("Step 4: Scenes Configuration (Single Mode)", EditorStyles.boldLabel);

			EditorGUILayout.HelpBox("Configure scenes for your build", MessageType.Info);

			if (_WizardSingleModeScenes.Count == 0) _WizardSingleModeScenes.Add(null);

			EditorGUILayout.BeginVertical("box");

			for (var i = 0; i < _WizardSingleModeScenes.Count; i++)
			{
				EditorGUILayout.BeginHorizontal();
				_WizardSingleModeScenes[i] = (SceneAsset)EditorGUILayout.ObjectField($"Scene {i + 1}",
					_WizardSingleModeScenes[i], typeof(SceneAsset), false);

				if (GUILayout.Button("Remove", GUILayout.Width(60)))
				{
					_WizardSingleModeScenes.RemoveAt(i);
					i--;
				}

				EditorGUILayout.EndHorizontal();
			}

			if (GUILayout.Button("Add Scene")) _WizardSingleModeScenes.Add(null);

			EditorGUILayout.EndVertical();

			// Validation
			if (_WizardSingleModeScenes.Count == 0 || _WizardSingleModeScenes.All(_s => _s == null))
				EditorGUILayout.HelpBox("Please add at least one scene", MessageType.Warning);
			else if (_WizardSingleModeScenes.Any(_s => _s == null))
				EditorGUILayout.HelpBox("Some scenes are empty. Please assign scenes or remove empty slots.",
					MessageType.Warning);
			else
				EditorGUILayout.HelpBox("Scenes configuration is valid", MessageType.Info);

			GUILayout.Space(20);

			EditorGUILayout.BeginHorizontal();
			if (GUILayout.Button("Back")) _WizardStep = 3;

			if (GUILayout.Button("Next")) _WizardStep = 5;
			EditorGUILayout.EndHorizontal();
		}

		/// <summary>
		/// Executes the operations required to display and manage step 5 of the wizard process.
		/// This method is responsible for setting up the necessary UI parts,
		/// initializing relevant data, and handling user interactions specific to this step.
		/// </summary>
		/// <remarks>
		/// This method should be called as part of the sequential wizard flow.
		/// Ensure that all prerequisites from the previous steps are satisfied before invoking this method.
		/// </remarks>
		private void DrawWizardStep5()
		{
			GUILayout.Label("Step 5: Build Profiles (Single Mode)", EditorStyles.boldLabel);

			EditorGUILayout.HelpBox("Configure build profiles for your project", MessageType.Info);

			if (_WizardSingleModeProfiles.Count == 0)
			{
				var codeName = _WizardCodeNames.Count > 0 ? _WizardCodeNames[0] : _WizardProjectName;
				_WizardSingleModeProfiles.Add(new BuildProfile
				{
					ProfileName = $"{codeName} Production",
					DisplayName = "Production",
					BuildSuffix = "prd",
					ProductName = codeName,
					BuildTarget = BuildTarget.StandaloneWindows64,
					Development = false,
					ScriptDebugging = false,
					DefaultDevelopmentBuild = false
				});

				_WizardSingleModeProfiles.Add(new BuildProfile
				{
					ProfileName = $"{codeName} Development",
					DisplayName = "Development",
					BuildSuffix = "dev",
					ProductName = codeName,
					BuildTarget = BuildTarget.StandaloneWindows64,
					Development = true,
					ScriptDebugging = true,
					DefaultDevelopmentBuild = true
				});
			}

			// Display build profiles with a new UI
			for (var i = 0; i < _WizardSingleModeProfiles.Count; i++)
			{
				var index = i;
				DrawBuildProfileUI(_WizardSingleModeProfiles[index], index,
					() => { _WizardSingleModeProfiles.RemoveAt(index); }, null);
			}

			if (GUILayout.Button("Add Profile"))
			{
				var codeName = _WizardCodeNames.Count > 0 ? _WizardCodeNames[0] : _WizardProjectName;
				var newProfile = new BuildProfile
				{
					ProfileName = $"{codeName} Profile",
					DisplayName = "Profile",
					BuildSuffix = "prf",
					ProductName = codeName,
					BuildTarget = BuildTarget.StandaloneWindows64,
					Development = false,
					ScriptDebugging = false,
					DefaultDevelopmentBuild = false
				};
				_WizardSingleModeProfiles.Add(newProfile);
			}

			GUILayout.Space(20);

			EditorGUILayout.BeginHorizontal();
			if (GUILayout.Button("Back")) _WizardStep = 4;
			if (GUILayout.Button("Complete Setup")) _WizardStep = 6;
			EditorGUILayout.EndHorizontal();
		}

		/// <summary>
		/// Handles the rendering and configuration of Step 4 in the build settings setup wizard,
		/// operating in wildcard mode. This step allows the user to assign scenes for each
		/// project code name dynamically.
		/// </summary>
		private void DrawWizardStep4Wildcard()
		{
			GUILayout.Label("Step 4: Scenes Configuration (Wildcard Mode)", EditorStyles.boldLabel);

			foreach (var codeName in
			         _WizardCodeNames.Where(_codeName => !_WizardScenesByCodeName.ContainsKey(_codeName)))
				_WizardScenesByCodeName[codeName] = new List<SceneAsset>();

			// Code name selector tabs
			EditorGUILayout.BeginHorizontal();
			GUILayout.Label("Configure:", GUILayout.Width(70));

			for (var i = 0; i < _WizardCodeNames.Count; i++)
				if (!string.IsNullOrEmpty(_WizardCodeNames[i]))
				{
					var isSelected = _WizardSelectedCodeNameIndex == i;
					if (GUILayout.Toggle(isSelected, _WizardCodeNames[i], EditorStyles.miniButton))
						_WizardSelectedCodeNameIndex = i;
				}

			EditorGUILayout.EndHorizontal();

			// Show settings for the selected code name
			if (_WizardSelectedCodeNameIndex >= 0 && _WizardSelectedCodeNameIndex < _WizardCodeNames.Count)
			{
				var selectedCodeName = _WizardCodeNames[_WizardSelectedCodeNameIndex];
				GUILayout.Label($"Scenes for: {selectedCodeName}", EditorStyles.boldLabel);

				DrawWizardScenesForCodeName(selectedCodeName);
			}

			// Validation message
			var isValid = ValidateStep4();
			if (!isValid)
				EditorGUILayout.HelpBox("All code names must have at least one valid scene assigned.",
					MessageType.Error);

			GUILayout.Space(20);

			EditorGUILayout.BeginHorizontal();
			if (GUILayout.Button("Back")) _WizardStep = 3;

			GUI.enabled = isValid;
			if (GUILayout.Button("Next")) _WizardStep = 5;
			GUI.enabled = true;
			EditorGUILayout.EndHorizontal();
		}

		/// <summary>
		/// Represents a method that handles the creation or rendering of the fifth step
		/// in a wizard process that involves wildcards.
		/// </summary>
		/// <remarks>
		/// This method is designed to process data- or setup-specific configurations
		/// required for the fifth step in a wizard-like workflow.
		/// It may involve wildcard-related operations such as filtering or custom logic handling.
		/// Ensure that prerequisite steps are completed before invoking this method.
		/// </remarks>
		private void DrawWizardStep5Wildcard()
		{
			GUILayout.Label("Step 5: Build Profiles (Wildcard Mode)", EditorStyles.boldLabel);
			foreach (var codeName in _WizardCodeNames)
				if (!_WizardProfilesByCodeName.ContainsKey(codeName))
					_WizardProfilesByCodeName[codeName] = CreateDefaultProfiles(codeName);

			// Code name selector tabs
			EditorGUILayout.BeginHorizontal();
			GUILayout.Label("Configure:", GUILayout.Width(70));

			for (var i = 0; i < _WizardCodeNames.Count; i++)
				if (!string.IsNullOrEmpty(_WizardCodeNames[i]))
				{
					var isSelected = _WizardSelectedCodeNameIndex == i;
					if (GUILayout.Toggle(isSelected, _WizardCodeNames[i], EditorStyles.miniButton))
						_WizardSelectedCodeNameIndex = i;
				}

			EditorGUILayout.EndHorizontal();

			// Show settings for the selected code name
			if (_WizardSelectedCodeNameIndex >= 0 && _WizardSelectedCodeNameIndex < _WizardCodeNames.Count)
			{
				var selectedCodeName = _WizardCodeNames[_WizardSelectedCodeNameIndex];
				GUILayout.Label($"Build Profiles for: {selectedCodeName}", EditorStyles.boldLabel);

				DrawWizardBuildProfilesForCodeName(selectedCodeName);
			}

			GUILayout.Space(20);

			EditorGUILayout.BeginHorizontal();
			if (GUILayout.Button("Back")) _WizardStep = 4;
			if (GUILayout.Button("Complete Setup")) _WizardStep = 6;
			EditorGUILayout.EndHorizontal();
		}

		/// <summary>
		/// Handles the final step in the setup wizard, indicating completion of the build settings configuration.
		/// </summary>
		/// <remarks>
		/// This method is responsible for displaying a completion message and providing an option to finalize the setup.
		/// It ensures the user is informed that the setup process has been successfully completed.
		/// </remarks>
		private void DrawWizardComplete()
		{
			GUILayout.Label("Setup Complete!", EditorStyles.boldLabel);

			EditorGUILayout.HelpBox("Build settings have been configured successfully.", MessageType.Info);

			if (GUILayout.Button("Finish")) CompleteSetup();
		}

		#endregion

		#region Validation Methods

		/// <summary>
		/// Validates the build folder path used in the wizard to ensure it is not empty
		/// and that the directory exists on the file system.
		/// </summary>
		/// <returns>
		/// True if the build folder path is not null or empty and the directory exists; otherwise, false.
		/// </returns>
		private bool ValidateBuildFolderForWizard()
		{
			if (string.IsNullOrEmpty(_WizardBuildFolderPath))
				return false;

			return Directory.Exists(_WizardBuildFolderPath);
		}

		/// <summary>
		/// Validates that at least one scene is configured for single mode operations.
		/// </summary>
		/// <returns>
		/// A boolean value indicating whether there is at least one valid scene configured for a single mode.
		/// </returns>
		private bool ValidateScenesForSingleMode()
		{
			// Check if at least one scene is configured in a single mode
			return _WizardSingleModeScenes.Any(_scene => _scene is not null);
		}


		/// <summary>
		/// Validates scenes in the project to ensure compatibility with wildcard mode.
		/// </summary>
		/// <returns>
		/// A boolean value indicating whether all scenes meet the requirements for wildcard mode.
		/// Returns true if the validation is successful; otherwise, false.
		/// </returns>
		private bool ValidateScenesForWildcardMode()
		{
			if (!_WizardUseWildcardMode)
				return true;

			foreach (var codeName in _WizardCodeNames)
			{
				if (string.IsNullOrEmpty(codeName))
					continue;

				if (!_WizardScenesByCodeName.TryGetValue(codeName, out var scenes))
					return false;

				if (scenes.Count == 0)
					return false;

				if (scenes.Any(_scene => _scene == null))
					return false;
			}

			return true;
		}

		/// <summary>
		/// Validates the inputs and performs the necessary operations for Step 3 of the process.
		/// </summary>
		/// <returns>
		/// A boolean value indicating whether the validation for Step 3 was successful.
		/// Returns true if the validation passes; otherwise, false.
		/// </returns>
		private bool ValidateStep3()
		{
			return ValidateBuildFolderForWizard();
		}

		/// <summary>
		/// Validates the application logic and performs actions for Step 4 of the process.
		/// This method is responsible for ensuring that all necessary conditions and
		/// requirements for Step 4 are met before proceeding further.
		/// </summary>
		/// <returns>
		/// A boolean value indicating whether the validation for Step 4 was successful.
		/// Returns true if the validation passes, otherwise false.
		/// </returns>
		private bool ValidateStep4()
		{
			if (_WizardUseWildcardMode)
			{
				// Wildcard mode: validate that all code names have at least one scene
				return ValidateScenesForWildcardMode();
			}
			else
			{
				// Single mode: validate that at least one scene is configured
				return ValidateScenesForSingleMode();
			}
		}


		#endregion

		#region Helper Methods

		/// <summary>
		/// Creates a list of default build profiles for a given project code name.
		/// </summary>
		/// <param name="_codeName">The project code name for which the default build profiles are created.</param>
		/// <returns>A list of default build profiles, including production and development profiles.</returns>
		private List<BuildProfile> CreateDefaultProfiles(string _codeName)
		{
			return new List<BuildProfile>
			{
				BuildSettingsData.GetDefaultProductionProfile(_codeName),
				BuildSettingsData.GetDefaultDevelopmentProfile(_codeName)
			};
		}


		/// <summary>
		/// Generates and renders wizard scenes based on the provided code name.
		/// </summary>
		/// <param name="_codeName">The unique code name used to determine the wizard scenes to be drawn.</param>
		private void DrawWizardScenesForCodeName(string _codeName)
		{
			if (!_WizardScenesByCodeName.ContainsKey(_codeName))
				_WizardScenesByCodeName[_codeName] = new List<SceneAsset>();

			var scenes = _WizardScenesByCodeName[_codeName];

			for (var i = 0; i < scenes.Count; i++)
			{
				EditorGUILayout.BeginHorizontal();
				scenes[i] = (SceneAsset)EditorGUILayout.ObjectField($"Scene {i + 1}", scenes[i], typeof(SceneAsset),
					false);

				if (GUILayout.Button("Remove", GUILayout.Width(60)))
				{
					scenes.RemoveAt(i);
					i--;
				}

				EditorGUILayout.EndHorizontal();
			}

			if (GUILayout.Button("Add Scene")) scenes.Add(null);

			if (scenes.Count == 0)
				EditorGUILayout.HelpBox($"No scenes added for {_codeName}", MessageType.Warning);
			else if (scenes.Any(_s => _s == null))
				EditorGUILayout.HelpBox($"Some scenes are empty for {_codeName}", MessageType.Error);
			else
				EditorGUILayout.HelpBox($"Scenes valid for {_codeName}", MessageType.Info);
		}

		/// <summary>
		/// Draws the wizard UI for managing build profiles associated with a specific code name.
		/// </summary>
		/// <param name="_codeName">The code name for which the build profiles are being configured.</param>
		private void DrawWizardBuildProfilesForCodeName(string _codeName)
		{
			if (!_WizardProfilesByCodeName.ContainsKey(_codeName))
				_WizardProfilesByCodeName[_codeName] = new List<BuildProfile>
				{
					new()
					{
						ProfileName = $"{_codeName} Production",
						DisplayName = "Production",
						BuildSuffix = "prd",
						ProductName = _codeName,
						BuildTarget = BuildTarget.StandaloneWindows64,
						Development = false,
						ScriptDebugging = false
					},
					new()
					{
						ProfileName = $"{_codeName} Development",
						DisplayName = "Development",
						BuildSuffix = "dev",
						ProductName = _codeName,
						BuildTarget = BuildTarget.StandaloneWindows64,
						Development = true,
						ScriptDebugging = true
					}
				};

			var profiles = _WizardProfilesByCodeName[_codeName];

			for (var i = 0; i < profiles.Count; i++)
			{
				var index = i;
				DrawBuildProfileUI(profiles[index], index, () => { profiles.RemoveAt(index); }, null);
			}

			if (GUILayout.Button("Add Profile"))
			{
				var newProfile = new BuildProfile
				{
					ProfileName = $"{_codeName} Profile",
					DisplayName = "Profile",
					BuildSuffix = "prf",
					ProductName = _codeName,
					BuildTarget = BuildTarget.StandaloneWindows64,
					Development = false,
					ScriptDebugging = false
				};
				profiles.Add(newProfile);
			}
		}


		/// <summary>
		/// Renders the UI elements for a specific build profile, allowing for editing, removal, and build actions.
		/// </summary>
		/// <param name="_profile">The build profile to be displayed and edited.</param>
		/// <param name="_index">The index of the build profile in the list.</param>
		/// <param name="_onRemove">The callback action to invoke when the "Remove" button is clicked.</param>
		/// <param name="_onBuild">The callback action to invoke when the "Build" button is clicked.</param>
		private void DrawBuildProfileUI(BuildProfile _profile, int _index, Action _onRemove, Action _onBuild)
		{
			var profileKey = $"{_profile.ProfileName}_{_index}";
			_ProfileFoldStates.TryAdd(profileKey, _index == 0);

			EditorGUILayout.BeginVertical("box");

			// Header with foldout and remove button
			EditorGUILayout.BeginHorizontal();
			_ProfileFoldStates[profileKey] =
				EditorGUILayout.Foldout(_ProfileFoldStates[profileKey], _profile.DisplayName, true);
			GUILayout.FlexibleSpace();
			if (GUILayout.Button("Remove", GUILayout.Width(60)))
			{
				_onRemove?.Invoke();
				return;
			}

			EditorGUILayout.EndHorizontal();

			if (_ProfileFoldStates[profileKey])
			{
				EditorGUILayout.BeginHorizontal();

				// Icon area (64x64)
				var iconRect = GUILayoutUtility.GetRect(64, 64, GUILayout.Width(64), GUILayout.Height(64));
				_profile.Icon = (Texture2D)EditorGUI.ObjectField(iconRect, _profile.Icon, typeof(Texture2D), false);

				// Profile details
				EditorGUILayout.BeginVertical();
				_profile.DisplayName = EditorGUILayout.TextField("Display:", _profile.DisplayName);
				_profile.BuildSuffix = EditorGUILayout.TextField("Suffix:", _profile.BuildSuffix);
				_profile.ProductName = EditorGUILayout.TextField("Product:", _profile.ProductName);
				EditorGUILayout.EndVertical();

				EditorGUILayout.EndHorizontal();

				// Build options
				EditorGUILayout.BeginHorizontal();
				if (_onBuild != null)
				{
					var originalColor = GUI.backgroundColor;

					GUI.backgroundColor = Color.cyan;
					if (GUILayout.Button($"Build {_profile.DisplayName}")) _onBuild();

					GUI.backgroundColor = originalColor;
				}

				_profile.Development = EditorGUILayout.Toggle("Dev Build", _profile.Development);
				EditorGUILayout.EndHorizontal();

				// Additional settings
				_profile.BuildTarget = (BuildTarget)EditorGUILayout.EnumPopup("Build Target", _profile.BuildTarget);
				_profile.ScriptDebugging = EditorGUILayout.Toggle("Script Debugging", _profile.ScriptDebugging);
			}

			EditorGUILayout.EndVertical();
			GUILayout.Space(5);
		}

		#endregion

		#region Main UI Methods

		/// <summary>
		/// Configures and displays the user interface for the "Wildcard Mode".
		/// </summary>
		/// <remarks>
		/// This method is responsible for initializing, rendering, and handling the UI elements
		/// relevant to the "Wildcard Mode". It ensures all required UI components are set up properly
		/// and handles any actions or events triggered while the mode is active.
		/// </remarks>
		private void DrawWildcardModeUI()
		{
			GUILayout.Label("Wildcard Mode - Multiple Code Names", EditorStyles.boldLabel);

			// Code name selector tabs
			EditorGUILayout.BeginHorizontal();
			GUILayout.Label("Active Code Name:", GUILayout.Width(120));

			for (var i = 0; i < _BuildSettingsData.CodeNames.Count; i++)
			{
				var isSelected = _SelectedCodeNameIndex == i;
				if (GUILayout.Toggle(isSelected, _BuildSettingsData.CodeNames[i], EditorStyles.miniButton))
					if (_SelectedCodeNameIndex != i)
					{
						_SelectedCodeNameIndex = i;
						UpdateCurrentActiveCodeName();
					}
			}

			EditorGUILayout.EndHorizontal();

			// Show settings for the selected code name
			if (_SelectedCodeNameIndex >= 0 && _SelectedCodeNameIndex < _BuildSettingsData.CodeNames.Count)
			{
				var selectedCodeName = _BuildSettingsData.CodeNames[_SelectedCodeNameIndex];

				DrawProjectInfoSection();
				DrawVersionSection();
				DrawBuildFolderSection();
				DrawScenesForCodeName(selectedCodeName);
				DrawBuildProfilesForCodeName(selectedCodeName);
				DrawBuildActionsSection();
			}
		}

		/// <summary>
		/// Renders the user interface in single mode.
		/// </summary>
		/// <remarks>
		/// This method is responsible for drawing the user interface when
		/// the application is operating in a single mode. It ensures that
		/// the UI components are properly displayed and interactable
		/// in this specific mode of operation.
		/// </remarks>
		private void DrawSingleModeUI()
		{
			DrawProjectInfoSection();
			DrawCodeNameSwitchSection();
			DrawVersionSection();
			DrawBuildFolderSection();
			DrawScenesSection();
			DrawBuildProfilesSection();
			DrawBuildActionsSection();
		}

		/// <summary>
		/// Draws scenes associated with a specific code name.
		/// </summary>
		/// <param name="_codeName">The identifier used to determine which scenes to draw.</param>
		private void DrawScenesForCodeName(string _codeName)
		{
			GUILayout.Label($"Scenes for {_codeName}", EditorStyles.boldLabel);

			var config = _BuildSettingsData.CodeNameConfigs?.FirstOrDefault(_c => _c.CodeName == _codeName);
			if (config != null)
			{
				EditorGUILayout.BeginVertical("box");

				for (var i = 0; i < config.Scenes.Count; i++)
				{
					EditorGUILayout.BeginHorizontal();

					// Add an open scene button
					if (config.Scenes[i] is not null && GUILayout.Button("Open", GUILayout.Width(50)))
					{
						var scenePath = AssetDatabase.GetAssetPath(config.Scenes[i]);
						OpenScene(scenePath);
					}

					config.Scenes[i] = (SceneAsset)EditorGUILayout.ObjectField($"Scene {i + 1}", config.Scenes[i],
						typeof(SceneAsset), false);

					if (GUILayout.Button("Remove", GUILayout.Width(60)))
					{
						config.Scenes.RemoveAt(i);
						i--;
						SaveSettings();
					}

					EditorGUILayout.EndHorizontal();
				}

				if (GUILayout.Button("Add Scene"))
				{
					config.Scenes.Add(null);
					SaveSettings();
				}

				EditorGUILayout.EndVertical();
			}

			GUILayout.Space(10);
		}


		/// <summary>
		/// Draws the build profiles associated with the specified code name.
		/// </summary>
		/// <param name="_codeName">The code name for which the build profiles are displayed and managed.</param>
		private void DrawBuildProfilesForCodeName(string _codeName)
		{
			GUILayout.Label($"Build Profiles for {_codeName}", EditorStyles.boldLabel);

			var config = _BuildSettingsData.CodeNameConfigs?.FirstOrDefault(_c => _c.CodeName == _codeName);
			if (config != null)
			{
				for (var i = 0; i < config.BuildProfiles.Count; i++)
				{
					var index = i;
					DrawBuildProfileUI(config.BuildProfiles[index], index, () =>
					{
						config.BuildProfiles.RemoveAt(index);
						SaveSettings();
					}, () => { BuildProject(config.BuildProfiles[index]); });
				}

				if (GUILayout.Button("Add Profile"))
				{
					var newProfile = new BuildProfile
					{
						ProfileName = $"{_codeName} Profile",
						DisplayName = "Profile",
						BuildSuffix = "prf",
						ProductName = _codeName,
						BuildTarget = BuildTarget.StandaloneWindows64,
						Development = false,
						ScriptDebugging = false
					};
					config.BuildProfiles.Add(newProfile);
					SaveSettings();
				}
			}

			GUILayout.Space(10);
		}

		/// <summary>
		/// Renders the project information section within the application's interface.
		/// </summary>
		/// <remarks>
		/// This method is responsible for drawing the section of the application that
		/// displays relevant details about the project, such as its name, description,
		/// creation date, or other metadata. It ensures proper layout and visual representation
		/// based on the current UI design guidelines.
		/// </remarks>
		private void DrawProjectInfoSection()
		{
			GUILayout.Label("Project Information", EditorStyles.boldLabel);

			EditorGUILayout.BeginVertical("box");
			EditorGUILayout.LabelField("Project Name", _BuildSettingsData.ProjectName);
			EditorGUILayout.LabelField("Mode", _BuildSettingsData.UseWildcardMode ? "Wildcard" : "Single");
			EditorGUILayout.LabelField("Effective Name", _BuildSettingsData.GetEffectiveProjectName());
			EditorGUILayout.EndVertical();

			GUILayout.Space(10);
		}

		/// <summary>
		/// This method is responsible for rendering a switch section within a drawing application or context.
		/// It helps in toggling or switching the state of a visual element or set of elements within the user interface.
		/// </summary>
		/// <remarks>
		/// Ensure that the provided codeName matches the expected format and is not null or empty.
		/// This method may depend on external resources or settings, which should be configured appropriately before invocation.
		/// </remarks>
		private void DrawCodeNameSwitchSection()
		{
			GUILayout.Label("Code Name", EditorStyles.boldLabel);

			EditorGUILayout.BeginVertical("box");
			EditorGUILayout.LabelField("Active Code Name",
				_BuildSettingsData.CodeNames is { Count: > 0 }
					? _BuildSettingsData.CodeNames[0]
					: _BuildSettingsData.ProjectCodeName);
			EditorGUILayout.EndVertical();

			GUILayout.Space(10);
		}

		/// <summary>
		/// Renders the UI section responsible for managing version control
		/// within the Build Settings utility.
		/// </summary>
		/// <remarks>
		/// This method displays the current version retrieved from
		/// `PlayerSettings.bundleVersion` and provides options to increment
		/// the version using Major, Minor, or Patch buttons. Once modified,
		/// the "Apply Next Version" button allows the updated version to be
		/// applied. The next version is initialized to the current version if
		/// it has not been previously set
		/// </remarks>
		private void DrawVersionSection()
		{
			GUILayout.Label("Version Control", EditorStyles.boldLabel);

			EditorGUILayout.BeginVertical("box");

			var currentVersion = PlayerSettings.bundleVersion;
			EditorGUILayout.LabelField("Current Version:", currentVersion);

			// Initialize the next version if empty
			if (string.IsNullOrEmpty(_NextVersion)) _NextVersion = currentVersion;

			EditorGUILayout.LabelField("Next Version:", _NextVersion);

			// Version increment buttons
			EditorGUILayout.BeginHorizontal();
			if (GUILayout.Button("Major")) _NextVersion = IncrementVersionString(currentVersion, VersionType.Major);
			if (GUILayout.Button("Minor")) _NextVersion = IncrementVersionString(currentVersion, VersionType.Minor);
			if (GUILayout.Button("Patch")) _NextVersion = IncrementVersionString(currentVersion, VersionType.Patch);
			EditorGUILayout.EndHorizontal();

			// Apply version button
			EditorGUILayout.BeginHorizontal();
			if (GUILayout.Button("Apply Next Version")) ApplyVersionChange(_NextVersion);
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.EndVertical();

			GUILayout.Space(10);
		}

		/// <summary>
		/// Represents functionality to draw the build folder section within the UI.
		/// This method is typically called during the rendering process
		/// to display and organize components or data related to the build folder configuration.
		/// </summary>
		/// <remarks>
		/// It is expected that this method will be called within the larger context
		/// of UI rendering or application setup. Ensure that required resources or
		/// dependencies are initialized before invoking.
		/// </remarks>
		private void DrawBuildFolderSection()
		{
			GUILayout.Label("Build Folder", EditorStyles.boldLabel);

			EditorGUILayout.BeginVertical("box");
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("Path", _BuildSettingsData.BuildFolderPath);
			if (GUILayout.Button("Browse", GUILayout.Width(60))) SelectBuildFolder();
			EditorGUILayout.EndHorizontal();

			var isValid = ValidateBuildFolder();
			EditorGUILayout.LabelField("Status", isValid ? "Valid" : "Invalid");

			EditorGUILayout.EndVertical();

			GUILayout.Space(10);
		}

		/// <summary>
		/// Generates and draws a specific section of scenes within the application.
		/// </summary>
		/// <remarks>
		/// This method is responsible for rendering a designated portion of scenes
		/// based on predefined configurations or parameters provided in the calling context.
		/// It ensures proper layout and presentation of visual elements for the section.
		/// </remarks>
		private void DrawScenesSection()
		{
			GUILayout.Label("Scenes", EditorStyles.boldLabel);

			EditorGUILayout.BeginVertical("box");

			if (_BuildSettingsData.BuildScenes is { Count: > 0 })
				for (var i = 0; i < _BuildSettingsData.BuildScenes.Count; i++)
				{
					EditorGUILayout.BeginHorizontal();

					// Add an open scene button
					if (_BuildSettingsData.BuildScenes[i] is not null && GUILayout.Button("Open", GUILayout.Width(50)))
					{
						var scenePath = AssetDatabase.GetAssetPath(_BuildSettingsData.BuildScenes[i]);
						OpenScene(scenePath);
					}

					_BuildSettingsData.BuildScenes[i] = (SceneAsset)EditorGUILayout.ObjectField($"Scene {i + 1}",
						_BuildSettingsData.BuildScenes[i], typeof(SceneAsset), false);

					if (GUILayout.Button("Remove", GUILayout.Width(60)))
					{
						_BuildSettingsData.BuildScenes.RemoveAt(i);
						i--;
						SaveSettings();
					}

					EditorGUILayout.EndHorizontal();
				}
			else
				EditorGUILayout.LabelField("No scenes configured");

			if (GUILayout.Button("Add Scene"))
			{
				_BuildSettingsData.BuildScenes!.Add(null);
				SaveSettings();
			}

			EditorGUILayout.EndVertical();

			GUILayout.Space(10);
		}

		/// <summary>
		/// Renders the Build Profiles section in the user interface.
		/// </summary>
		/// <remarks>
		/// This method is responsible for creating and displaying the UI elements
		/// associated with the Build Profiles section. It may include functionality
		/// to handle user interactions, populate data, and update the UI dynamically.
		/// </remarks>
		private void DrawBuildProfilesSection()
		{
			GUILayout.Label("Build Profiles", EditorStyles.boldLabel);

			if (_BuildSettingsData.BuildProfiles is { Count: > 0 })
			{
				for (var i = 0; i < _BuildSettingsData.BuildProfiles.Count; i++)
				{
					var index = i;
					DrawBuildProfileUI(_BuildSettingsData.BuildProfiles[index], index, () =>
					{
						_BuildSettingsData.BuildProfiles.RemoveAt(index);
						SaveSettings();
					}, () => { BuildProject(_BuildSettingsData.BuildProfiles[index]); });
				}
			}
			else
			{
				EditorGUILayout.BeginVertical("box");
				EditorGUILayout.LabelField("No build profiles configured");
				EditorGUILayout.EndVertical();
			}

			if (GUILayout.Button("Add Profile"))
			{
				var newProfile = new BuildProfile
				{
					ProfileName = "New Profile",
					DisplayName = "Profile",
					BuildSuffix = "prf",
					ProductName = _BuildSettingsData.GetEffectiveProjectName(),
					BuildTarget = BuildTarget.StandaloneWindows64,
					Development = false,
					ScriptDebugging = false
				};
				_BuildSettingsData.BuildProfiles.Add(newProfile);
				SaveSettings();
			}

			GUILayout.Space(10);
		}

		/// <summary>
		/// This method is responsible for drawing the Build Actions section of the user interface.
		/// It encapsulates the logic for rendering UI elements associated with build actions,
		/// typically in configuration or toolset applications.
		/// The method may use certain UI components or libraries to generate
		/// the visual representation required for user interaction with build options.
		/// </summary>
		/// <remarks>
		/// Override or customize this method to implement specific behavior for
		/// the Build Actions section. Ensure any dependencies required for rendering
		/// are properly handled before invoking this method to avoid runtime errors.
		/// </remarks>
		private void DrawBuildActionsSection()
		{
			GUILayout.Label("Build Actions", EditorStyles.boldLabel);

			EditorGUILayout.BeginVertical("box");

			EditorGUILayout.BeginHorizontal();
			if (GUILayout.Button("Validate Build Settings")) ValidateBuildFolder();

			if (GUILayout.Button("Open Build Folder"))
			{
				if (Directory.Exists(_BuildSettingsData.BuildFolderPath))
					Process.Start(_BuildSettingsData.BuildFolderPath);
				else
					EditorUtility.DisplayDialog("Error", "Build folder does not exist", "OK");
			}

			EditorGUILayout.EndHorizontal();

			EditorGUILayout.EndVertical();
		}

		#endregion

		#region Script Define Symbol Management

		/// <summary>
		/// Updates the currently active code name to a new value.
		/// </summary>
		private void UpdateCurrentActiveCodeName()
		{
			if (_BuildSettingsData == null || !_BuildSettingsData.UseWildcardMode) return;

			if (_SelectedCodeNameIndex >= 0 && _SelectedCodeNameIndex < _BuildSettingsData.CodeNames.Count)
			{
				var activeCodeName = _BuildSettingsData.CodeNames[_SelectedCodeNameIndex];
				var config = _BuildSettingsData.CodeNameConfigs?.FirstOrDefault(_c => _c.CodeName == activeCodeName);
				if (config != null) SwitchToCodeName(config);
			}
		}

		/// <summary>
		/// Switches the current build configuration to the specified code name configuration.
		/// Update script defines symbols, build settings scenes, and other related settings
		/// based on the provided configuration.
		/// </summary>
		/// <param name="_config">
		/// The <see cref="CodeNameConfig"/> object containing the code name settings, script define symbols,
		/// and associated scenes to apply.
		/// </param>
		private void SwitchToCodeName(CodeNameConfig _config)
		{
			if (_config == null) return;

			// Update script defines symbols - remove old ones and adds new one
			var currentDefineSymbols =
				PlayerSettings.GetScriptingDefineSymbols(
					NamedBuildTarget.FromBuildTargetGroup(BuildTargetGroup.Standalone));
			var defineList = currentDefineSymbols.Split(';').ToList();

			// Remove all code names define symbols
			foreach (var codeNameConfig in _BuildSettingsData.CodeNameConfigs)
				if (!string.IsNullOrEmpty(codeNameConfig.ScriptDefineSymbol))
					defineList.RemoveAll(_d =>
						_d.Equals(codeNameConfig.ScriptDefineSymbol, StringComparison.OrdinalIgnoreCase));

			// Add the new code name define symbol
			if (!string.IsNullOrEmpty(_config.ScriptDefineSymbol)) defineList.Add(_config.ScriptDefineSymbol);

			// Update player settings
			var newDefineSymbols = string.Join(";", defineList.Where(_d => !string.IsNullOrEmpty(_d)).Distinct());
			PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.FromBuildTargetGroup(BuildTargetGroup.Standalone),
				newDefineSymbols);

			// Update build settings scenes
			var sceneAssets = _config.Scenes.Where(_s => _s is not null).ToArray();
			var buildScenes = new EditorBuildSettingsScene[sceneAssets.Length];

			for (var i = 0; i < sceneAssets.Length; i++)
			{
				var scenePath = AssetDatabase.GetAssetPath(sceneAssets[i]);
				buildScenes[i] = new EditorBuildSettingsScene(scenePath, true);
			}

			EditorBuildSettings.scenes = buildScenes;

			// Update product name
			PlayerSettings.productName = _config.CodeName;

			Debug.Log($"Switched to code name: {_config.CodeName} with define symbol: {_config.ScriptDefineSymbol}");
		}

		#endregion

		#region Version Control

		/// <summary>
		/// Increments the given version string based on the specified version type.
		/// </summary>
		/// <param name="_currentVersion">The current version strings to be incremented, formatted as "Major.Minor.Patch".</param>
		/// <param name="_versionType">The type of version increments to apply: Major, Minor, or Patch.</param>
		/// <returns>
		/// A string representing the incremented version number in the format "Major.Minor.Patch".
		/// </returns>
		private string IncrementVersionString(string _currentVersion, VersionType _versionType)
		{
			var versionParts = _currentVersion.Split('.');

			var major = int.Parse(versionParts.Length > 0 ? versionParts[0] : "0");
			var minor = int.Parse(versionParts.Length > 1 ? versionParts[1] : "0");
			var patch = int.Parse(versionParts.Length > 2 ? versionParts[2] : "0");

			switch (_versionType)
			{
				case VersionType.Major:
					major++;
					break;
				case VersionType.Minor:
					minor++;
					break;
				case VersionType.Patch:
					patch++;
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(_versionType), _versionType, null);
			}

			return $"{major}.{minor}.{patch}";
		}

		/// <summary>
		/// Applies a version change by updating the Player Settings and internal build settings data with the specified new version.
		/// </summary>
		/// <param name="_newVersion">The new version strings to be applied.</param>
		private void ApplyVersionChange(string _newVersion)
		{
			PlayerSettings.bundleVersion = _newVersion;
			_BuildSettingsData.CurrentVersion = _newVersion;
			SaveSettings();

			Debug.Log($"Version changed to: {_newVersion}");
		}

		#endregion

		#region Setup and Save Methods

		/// <summary>
		/// Finalizes the build settings configuration process by creating and populating
		/// a BuildSettingsData instance with the provided wizard input values.
		/// </summary>
		/// <remarks>
		/// This method sets up the project using the specified wizard configuration, including project
		/// name, code names, wildcard mode, build a folder path, and associated profiles or scenes.
		/// Depending on the wizard configuration, the setup can be completed in either single mode
		/// or wildcard mode. After configuration, this method marks the setup process as complete
		/// and saves the settings.
		/// </remarks>
		private void CompleteSetup()
		{
			// Create the build settings data
			_BuildSettingsData = CreateInstance<BuildSettingsData>();
			_BuildSettingsData.ProjectName = _WizardProjectName;
			_BuildSettingsData.UseWildcardMode = _WizardUseWildcardMode;
			_BuildSettingsData.CodeNames = new List<string>(_WizardCodeNames);
			_BuildSettingsData.BuildFolderPath = _WizardBuildFolderPath;

			if (_WizardUseWildcardMode)
			{
				_BuildSettingsData.WildcardProjectName = _WizardProjectName;

				// Initialize profiles and scenes for each code name
				foreach (var codeName in _WizardCodeNames)
				{
					var config = new CodeNameConfig
					{
						CodeName = codeName,
						ScriptDefineSymbol = codeName.ToUpper().Replace(" ", "_"),
						Scenes = _WizardScenesByCodeName.TryGetValue(codeName, out var sceneValue)
							? sceneValue
							: new List<SceneAsset>(),
						BuildProfiles = _WizardProfilesByCodeName.TryGetValue(codeName, out var profileValue)
							? profileValue
							: CreateDefaultProfiles(codeName) // Create default profiles if none exist
					};
					_BuildSettingsData.CodeNameConfigs.Add(config);
				}
			}
			else
			{
				// Single mode setup
				_BuildSettingsData.ProjectCodeName =
					_WizardCodeNames.Count > 0 ? _WizardCodeNames[0] : _WizardProjectName;

				// Set scenes for a single mode
				_BuildSettingsData.BuildScenes = new List<SceneAsset>(_WizardSingleModeScenes.Where(_s => _s is not null));

				// Set build profiles for a single mode
				_BuildSettingsData.BuildProfiles = new List<BuildProfile>(_WizardSingleModeProfiles);
			}

			_BuildSettingsData.IsSetupComplete = true;
			_IsSetupComplete = true;
			SaveSettings();
		}

		/// <summary>
		/// Loads the build settings data from the predefined asset file.
		/// If no settings file is found, a new one is created and initialized with default values.
		/// </summary>
		private void LoadOrCreateSettings()
		{
			_BuildSettingsData = AssetDatabase.LoadAssetAtPath<BuildSettingsData>(_SETTING_FILE_PATH);

			Debug.Log($"Loaded build settings data: {_BuildSettingsData}\nCompleted {_IsSetupComplete}");

			if (_BuildSettingsData == null)
			{
				_IsSetupComplete = false;
				_WizardStep = 1;
				_WizardCodeNames = new List<string>();
				_WizardBuildFolderPath = GetDefaultBuildFolder();
			}
			else
			{
				_IsSetupComplete = _BuildSettingsData.IsSetupComplete;
				if (_IsSetupComplete)
				{
					InitializeWizardFromSettings();
				}
				else
				{
					_WizardStep = 1;
					_WizardCodeNames = new List<string>();
					_WizardBuildFolderPath = GetDefaultBuildFolder();
				}
			}
		}

		/// <summary>
		/// Initializes the wizard's settings and data fields with values retrieved from the current build settings configuration.
		/// </summary>
		private void InitializeWizardFromSettings()
		{
			if (_BuildSettingsData == null) return;

			_WizardProjectName = _BuildSettingsData.ProjectName;
			_WizardUseWildcardMode = _BuildSettingsData.UseWildcardMode;
			_WizardCodeNames = new List<string>(_BuildSettingsData.CodeNames);
			_WizardBuildFolderPath = _BuildSettingsData.BuildFolderPath;

			// Initialize wizard dictionaries from existing data
			_WizardScenesByCodeName.Clear();
			_WizardProfilesByCodeName.Clear();

			if (_BuildSettingsData.UseWildcardMode && _BuildSettingsData.CodeNameConfigs != null)
				foreach (var config in _BuildSettingsData.CodeNameConfigs)
				{
					_WizardScenesByCodeName[config.CodeName] = new List<SceneAsset>(config.Scenes);
					_WizardProfilesByCodeName[config.CodeName] = new List<BuildProfile>(config.BuildProfiles);
				}
		}

		/// <summary>
		/// Saves the current build settings data to a predefined asset file.
		/// Ensures that the data is persisted and can be retrieved in later editor sessions.
		/// </summary>
		private void SaveSettings()
		{
			Debug.Log($"Saving build settings data: {_BuildSettingsData}");
			if (_BuildSettingsData is null) return;

			var directory = Path.GetDirectoryName(_SETTING_FILE_PATH);

			Debug.Log($"Saving build settings data to: {directory}");

			if (!Directory.Exists(directory))
				if (directory != null)
					Directory.CreateDirectory(directory);

			Debug.Log($"Saving build settings data to: {directory}");

			if (AssetDatabase.LoadAssetAtPath<BuildSettingsData>(_SETTING_FILE_PATH) is not null)
			{
				Debug.Log($"Overwriting existing build settings data: {_BuildSettingsData}");
				Debug.Log($"Setup Completed: {_BuildSettingsData.IsSetupComplete}");
				EditorUtility.SetDirty(_BuildSettingsData);
			}
			else
			{
				Debug.Log($"Creating new build settings data: {_BuildSettingsData}");
				AssetDatabase.CreateAsset(_BuildSettingsData, _SETTING_FILE_PATH);
			}

			Debug.Log($"Saved build settings data: {_BuildSettingsData}");

			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
		}

		#endregion

		#region Build Methods

		/// <summary>
		/// Initiates the build process for the specified build profile, including scene configuration and build execution.
		/// </summary>
		/// <param name="_profile">
		/// The build profile containing settings such as platform target and build options.
		/// </param>
		private void BuildProject(BuildProfile _profile)
		{
			Debug.Log($"Building profile: {_profile.ProfileName}");

			// Get scenes for the current code name
			string[] scenes = null;
			if (_BuildSettingsData.UseWildcardMode)
			{
				if (_SelectedCodeNameIndex >= 0 && _SelectedCodeNameIndex < _BuildSettingsData.CodeNames.Count)
				{
					var activeCodeName = _BuildSettingsData.CodeNames[_SelectedCodeNameIndex];
					var config =
						_BuildSettingsData.CodeNameConfigs?.FirstOrDefault(_c => _c.CodeName == activeCodeName);
					if (config != null) scenes = config.GetScenePaths();
				}
			}
			else
			{
				scenes = _BuildSettingsData.GetScenePaths();
			}

			if (scenes == null || scenes.Length == 0)
			{
				EditorUtility.DisplayDialog("Error", "No scenes configured for build", "OK");
				return;
			}

			PerformBuild(_profile, scenes);
		}

		/// <summary>
		/// Executes the build process for the specified build profile and scenes, generating the necessary artifacts.
		/// </summary>
		/// <param name="_profile">The build profile containing configuration details for the build.</param>
		/// <param name="_scenes">An array of scene paths to include in the build.</param>
		private void PerformBuild(BuildProfile _profile, string[] _scenes)
		{
			if (!ValidateBuildFolder())
			{
				EditorUtility.DisplayDialog("Error",
					"Invalid build folder. Please check the path in your build settings.", "OK");
				return;
			}

			// Apply build icon if available
			if (_profile.Icon is not null) ApplyBuildIcon(_profile.Icon);

			string codeName;
			var outputDirectory = _BuildSettingsData.BuildFolderPath;

			// Determine the code name and base output directory based on the mode
			if (_BuildSettingsData.UseWildcardMode)
			{
				// In wildcard mode, the structure is build_folder/code_name/
				codeName = _BuildSettingsData.CodeNames[_SelectedCodeNameIndex];
				outputDirectory = Path.Combine(outputDirectory, codeName);
			}
			else
			{
				// In non-wildcard mode, use the single project code name
				codeName = _BuildSettingsData.ProjectCodeName;
			}

			// Ensure the final output directory exists
			Directory.CreateDirectory(outputDirectory);

			var versionString = _BuildSettingsData.CurrentVersion.Replace('.', '-');
			var timestamp = DateTime.Now.ToString("yyMMdd");
			var baseArtifactName = $"{codeName.ToLower()}-{_profile.BuildSuffix}-v{versionString}-{timestamp}";
			var revisionNumber = 1;
			string finalArtifactName;
			while (true)
			{
				finalArtifactName = $"{baseArtifactName}-rev{revisionNumber}";
				string pathToCheck;

				// For Standalone builds, check for a directory
				if (_profile.BuildTarget is BuildTarget.StandaloneWindows or BuildTarget.StandaloneWindows64
				    or BuildTarget.StandaloneOSX)
				{
					pathToCheck = Path.Combine(outputDirectory, finalArtifactName);
					if (!Directory.Exists(pathToCheck)) break; // Found an available revision number
				}
				// For other builds, check for a file with the correct extension
				else
				{
					pathToCheck = Path.Combine(outputDirectory,
						finalArtifactName + GetBuildExtension(_profile.BuildTarget));
					if (!File.Exists(pathToCheck)) break; // Found an available revision number
				}

				revisionNumber++;
			}

			string buildPath;
			var extension = GetBuildExtension(_profile.BuildTarget);

			// Construct the final build path
			if (_profile.BuildTarget is BuildTarget.StandaloneWindows or BuildTarget.StandaloneWindows64
			    or BuildTarget.StandaloneOSX)
				// The final path is outputDirectory/finalArtifactName/productName.exe
				buildPath = Path.Combine(outputDirectory, finalArtifactName, _profile.ProductName + extension);
			else
				// The final path is outputDirectory/finalArtifactName.apk
				buildPath = Path.Combine(outputDirectory, finalArtifactName + extension);

			var buildOptions = new BuildPlayerOptions
			{
				scenes = _scenes,
				locationPathName = buildPath,
				target = _profile.BuildTarget,
				options = BuildOptions.None
			};

			if (_profile.Development) buildOptions.options |= BuildOptions.Development;

			if (_profile.ScriptDebugging) buildOptions.options |= BuildOptions.AllowDebugging;

			Debug.Log($"Starting build at: {buildPath}");

			var report = BuildPipeline.BuildPlayer(buildOptions);

			if (report.summary.result == BuildResult.Succeeded)
			{
				Debug.Log($"Build succeeded: {buildPath} ({report.summary.totalSize} bytes)");
				Process.Start(Path.GetDirectoryName(buildPath) ?? string.Empty);
			}
			else
			{
				Debug.LogError($"Build failed: {report.summary.result}");
				EditorUtility.DisplayDialog("Build Failed",
					$"Build failed with result: {report.summary.result}\n\nCheck the console for more details.", "OK");
			}
		}

		/// <summary>
		/// Validates the build folder path by checking its existence and creating the directory if it does not exist.
		/// </summary>
		/// <returns>
		/// A boolean value indicating whether the build folder exists or was successfully created.
		/// </returns>
		private bool ValidateBuildFolder()
		{
			var isValid = !string.IsNullOrEmpty(_BuildSettingsData.BuildFolderPath);

			if (isValid && !Directory.Exists(_BuildSettingsData.BuildFolderPath))
				try
				{
					Directory.CreateDirectory(_BuildSettingsData.BuildFolderPath);
					Debug.Log($"Created build folder: {_BuildSettingsData.BuildFolderPath}");
				}
				catch (Exception ex)
				{
					Debug.LogError($"Failed to create build folder: {ex.Message}");
					isValid = false;
				}

			return isValid;
		}

		/// <summary>
		/// Opens a folder selection dialog to allow the user to choose a build folder.
		/// The selected folder's path is stored in the build settings data, and the settings are saved
		/// after a valid selection is made.
		/// </summary>
		private void SelectBuildFolder()
		{
			var selectedPath =
				EditorUtility.OpenFolderPanel("Select Build Folder", _BuildSettingsData.BuildFolderPath, "");
			if (!string.IsNullOrEmpty(selectedPath))
			{
				_BuildSettingsData.BuildFolderPath = selectedPath;
				SaveSettings();
			}
		}

		/// <summary>
		/// Applies the specified icon to the build settings for the current project.
		/// </summary>
		/// <param name="_selectedIcon">
		/// A Texture2D object representing the icon to be applied to the build.
		/// If null, the method will skip the icon application process.
		/// </param>
		private void ApplyBuildIcon(Texture2D _selectedIcon)
		{
			if (_selectedIcon is not null)
			{
				// Get current icon sizes
				var iconSizes = PlayerSettings.GetIconSizes(NamedBuildTarget.Standalone, IconKind.Any);

				// If no icon sizes are defined, we need to initialize them by setting icons
				if (iconSizes.Length == 0)
				{
					// Create a default array of 8 icons (common for Standalone)
					// Unity will automatically create the appropriate sizes
					var defaultIcons = new Texture2D[8];
					for (var i = 0; i < 8; i++) defaultIcons[i] = _selectedIcon;

					// This will initialize the icon sizes and set the icons
					PlayerSettings.SetIcons(NamedBuildTarget.Standalone, defaultIcons, IconKind.Any);
				}
				else
				{
					// Icon sizes are already defined, apply the icon
					var iconsToApply = new Texture2D[iconSizes.Length];
					for (var i = 0; i < iconSizes.Length; i++) iconsToApply[i] = _selectedIcon;

					PlayerSettings.SetIcons(NamedBuildTarget.Standalone, iconsToApply, IconKind.Any);
				}
			}
			else
			{
				Debug.Log("No icon selected, skipping icon application");
			}
		}

		/// <summary>
		/// Determines the appropriate file extension for a build artifact based on the specified build target.
		/// </summary>
		/// <param name="_target">The build target platform for which the file extension is to be determined.</param>
		/// <returns>
		/// A string representing the file extension used for the specified build target.
		/// </returns>
		private string GetBuildExtension(BuildTarget _target)
		{
			return _target switch
			{
				BuildTarget.StandaloneWindows or BuildTarget.StandaloneWindows64 => ".exe",
				BuildTarget.StandaloneOSX => ".app",
				BuildTarget.StandaloneLinux64 => "",
				BuildTarget.Android => ".apk",
				_ => ""
			};
		}

		/// <summary>
		/// Opens a scene in the Unity Editor from the specified path.
		/// </summary>
		/// <param name="_scenePath">The file path of the scene to be opened. Must point to a valid scene file.</param>
		private void OpenScene(string _scenePath)
		{
			if (!string.IsNullOrEmpty(_scenePath) && File.Exists(_scenePath))
				EditorSceneManager.OpenScene(_scenePath);
			else
				Debug.LogWarning($"Scene not found: {_scenePath}");
		}

		#endregion
	}
}