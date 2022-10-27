﻿using AssetRipper.Assets;
using AssetRipper.Assets.Collections;
using AssetRipper.Assets.Export;
using AssetRipper.Core.SourceGenExtensions;
using AssetRipper.SourceGenerated.Classes.ClassID_1;
using AssetRipper.SourceGenerated.Classes.ClassID_114;
using AssetRipper.SourceGenerated.Classes.ClassID_141;
using AssetRipper.SourceGenerated.Classes.ClassID_2;
using AssetRipper.SourceGenerated.Classes.ClassID_3;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.RegularExpressions;

namespace AssetRipper.Core.Project.Collections
{
	public static class SceneExportHelpers
	{
		private const string AssetsName = "Assets/";
		private const string LevelName = "level";
		private const string MainSceneName = "maindata";

		private static readonly Regex s_sceneNameFormat = new Regex($"^{LevelName}(0|[1-9][0-9]*)$");

		public static int FileNameToSceneIndex(string name, UnityVersion version)
		{
			if (HasMainData(version))
			{
				if (name == MainSceneName)
				{
					return 0;
				}

				return int.Parse(name.AsSpan(LevelName.Length)) + 1;
			}
			else
			{
				return int.Parse(name.AsSpan(LevelName.Length));
			}
		}

		/// <summary>
		/// Less than 5.3.0
		/// </summary>
		public static bool HasMainData(UnityVersion version) => version.IsLess(5, 3);

		/// <summary>
		/// GameObject, Classes Inherited From Level Game Manager, Monobehaviours with GameObjects, Components
		/// </summary>
		public static bool IsSceneCompatible(IUnityObjectBase asset)
		{
			return asset switch
			{
				IGameObject => true,
				ILevelGameManager => true,
				IMonoBehaviour monoBeh => monoBeh.IsSceneObject(),
				IComponent => true,
				_ => false
			};
		}

		public static string SceneIndexToFileName(int index, UnityVersion version)
		{
			if (HasMainData(version))
			{
				if (index == 0)
				{
					return MainSceneName;
				}
				return $"{LevelName}{index - 1}";
			}
			return $"{LevelName}{index}";
		}

		public static bool TryGetScenePath(AssetCollection serializedFile, [NotNullWhen(true)] IBuildSettings? buildSettings, [NotNullWhen(true)] out string? result)
		{
			if (buildSettings is not null && IsSceneName(serializedFile.Name))
			{
				int index = FileNameToSceneIndex(serializedFile.Name, serializedFile.Version);
				string scenePath = buildSettings.Scenes_C141[index].String;
				if (scenePath.StartsWith(AssetsName, StringComparison.Ordinal))
				{
					string extension = Path.GetExtension(scenePath);
					result = scenePath[..^extension.Length];
					return true;
				}
				else if (Path.IsPathRooted(scenePath))
				{
					// pull/uTiny 617
					// NOTE: absolute project path may contain Assets/ in its name so in this case we get incorrect scene path, but there is no way to bypass this issue
					int assetIndex = scenePath.IndexOf(AssetsName);
					string extension = Path.GetExtension(scenePath);
					result = scenePath.Substring(assetIndex, scenePath.Length - assetIndex - extension.Length);
					return true;
				}
				else if (scenePath.Length == 0)
				{
					// if you build a game without included scenes, Unity create one with empty name
					result = null;
					return false;
				}
				else
				{
					result = Path.Combine("Assets/Scenes", scenePath);
					return true;
				}
			}
			result = null;
			return false;
		}

		internal static bool IsSceneDuplicate(int sceneIndex, IBuildSettings? buildSettings)
		{
			if (buildSettings == null)
			{
				return false;
			}

			string sceneName = buildSettings.Scenes_C141[sceneIndex].String;
			for (int i = 0; i < buildSettings.Scenes_C141.Count; i++)
			{
				if (buildSettings.Scenes_C141[i] == sceneName)
				{
					if (i != sceneIndex)
					{
						return true;
					}
				}
			}
			return false;
		}

		internal static bool IsDuplicate(IExportContainer container, AssetCollection serializedFile)
		{
			if (IsSceneName(serializedFile.Name))
			{
				int index = FileNameToSceneIndex(serializedFile.Name, serializedFile.Version);
				return container.IsSceneDuplicate(index);
			}
			return false;
		}

		private static bool IsSceneName(string name) => name == MainSceneName || s_sceneNameFormat.IsMatch(name);
	}
}
