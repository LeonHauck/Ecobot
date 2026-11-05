using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EcoBot.EditorTools
{
    /// <summary>
    /// Construtor do projeto: transforma os sprites, sons e mapas ASCII em
    /// animações, prefabs e cenas prontas. Menu: EcoBot > 1. Gerar projeto completo.
    ///
    /// Dividido em arquivos parciais:
    ///   EcoBotBuilder.cs          menu, fluxo principal e utilitários
    ///   EcoBotBuilder.Assets.cs   tiles e animações (Animator Controllers)
    ///   EcoBotBuilder.Prefabs.cs  prefabs (jogador, inimigos, itens, partículas, Systems)
    ///   EcoBotBuilder.Levels.cs   definição das fases e montagem das cenas
    ///   EcoBotBuilder.UI.cs       HUD, menus e telas
    /// </summary>
    public static partial class EcoBotBuilder
    {
        const string SpritesRoot = "Assets/Art/Sprites";
        const string AudioRoot = "Assets/Audio";
        const string ScenesRoot = "Assets/Scenes";
        const string PrefabsRoot = "Assets/Prefabs";
        const string AnimRoot = "Assets/Animations";
        const string TilesRoot = "Assets/Tiles";
        const string BuildPath = "Builds/Windows/EcoBot.exe";

        static readonly string[] SceneOrder = { "MainMenu", "Level1", "Level2", "Level3", "Level4", "Victory" };

        // ================================================================ menus
        [MenuItem("EcoBot/1. Gerar projeto completo", priority = 1)]
        static void BuildAllMenu()
        {
            if (!EditorUtility.DisplayDialog("EcoBot",
                    "Gerar animações, prefabs e todas as cenas do jogo?\n\n" +
                    "As cenas e prefabs gerados anteriormente serão substituídos.", "Gerar", "Cancelar"))
                return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            BuildAll();
            EditorUtility.DisplayDialog("EcoBot", "Projeto gerado! Abra a cena MainMenu e aperte Play.", "OK");
        }

        [MenuItem("EcoBot/2. Exportar executável (Windows)", priority = 2)]
        static void BuildWindowsMenu()
        {
            if (BuildWindows()) EditorUtility.RevealInFinder(BuildPath);
        }

        [MenuItem("EcoBot/Abrir cena do menu", priority = 20)]
        static void OpenMenuScene() => EditorSceneManager.OpenScene(ScenePath("MainMenu"));

        /// <summary>Para linha de comando: Unity.exe -batchmode -quit -projectPath . -executeMethod EcoBot.EditorTools.EcoBotBuilder.BatchBuildAll</summary>
        public static void BatchBuildAll()
        {
            BuildAll();
            if (!BuildWindows()) EditorApplication.Exit(1);
        }

        /// <summary>Gera tudo sem exportar (útil na linha de comando).</summary>
        public static void BatchGenerateOnly() => BuildAll();

        // ================================================================ fluxo principal
        public static void BuildAll()
        {
            spriteCache = null;
            try
            {
                ShowProgress("Configurando o projeto", 0.05f);
                ConfigurePlayerSettings();
                ShowProgress("Importando sprites", 0.1f);
                FixArtImport();
                ShowProgress("Criando tiles", 0.2f);
                CreateTiles();
                ShowProgress("Criando animações", 0.3f);
                CreateAnimations();
                ShowProgress("Criando prefabs", 0.45f);
                CreatePrefabs();
                CreateSystemsPrefab();
                ShowProgress("Montando cenas", 0.6f);
                BuildAllScenes();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
            EditorSceneManager.OpenScene(ScenePath("MainMenu"));
            Debug.Log("<b>EcoBot</b>: projeto gerado com sucesso! Abra a cena MainMenu e aperte Play.");
        }

        static bool BuildWindows()
        {
            var scenes = SceneOrder.Select(ScenePath).Where(File.Exists).ToArray();
            if (scenes.Length < SceneOrder.Length)
            {
                Debug.LogError("EcoBot: gere o projeto antes de exportar (menu EcoBot > 1).");
                return false;
            }
            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = BuildPath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"<b>EcoBot</b>: executável gerado em {Path.GetFullPath(BuildPath)}");
                return true;
            }
            Debug.LogError("EcoBot: falha ao exportar: " + report.summary.result);
            return false;
        }

        static void ConfigurePlayerSettings()
        {
            PlayerSettings.companyName = "UniFECAF";
            PlayerSettings.productName = "EcoBot - Raizes do Amanha";
            PlayerSettings.defaultScreenWidth = 1280;
            PlayerSettings.defaultScreenHeight = 720;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = true;
        }

        static void FixArtImport()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Art" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetImporter.GetAtPath(path) is TextureImporter ti && ArtImportSettings.NeedsFix(ti))
                {
                    ArtImportSettings.Apply(ti, path);
                    ti.SaveAndReimport();
                }
            }
        }

        // ================================================================ utilitários
        static void ShowProgress(string msg, float t)
        {
            if (!Application.isBatchMode) EditorUtility.DisplayProgressBar("EcoBot", msg, t);
        }

        static string ScenePath(string name) => $"{ScenesRoot}/{name}.unity";

        static Dictionary<string, Sprite> spriteCache;

        /// <summary>Busca um sprite pelo nome do arquivo (sem extensão).</summary>
        static Sprite S(string name)
        {
            if (spriteCache == null)
            {
                spriteCache = new Dictionary<string, Sprite>();
                foreach (var guid in AssetDatabase.FindAssets("t:Sprite", new[] { SpritesRoot }))
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                    if (sprite != null) spriteCache[Path.GetFileNameWithoutExtension(path)] = sprite;
                }
            }
            if (!spriteCache.TryGetValue(name, out var s))
                throw new Exception($"EcoBot: sprite '{name}' não encontrado em {SpritesRoot}. Rode Tools/generate_sprites.py.");
            return s;
        }

        static Sprite[] Frames(string prefix, int count)
        {
            var arr = new Sprite[count];
            for (int i = 0; i < count; i++) arr[i] = S($"{prefix}_{i}");
            return arr;
        }

        static AudioClip LoadAudio(string relativePath)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{AudioRoot}/{relativePath}.wav");
            if (clip == null) throw new Exception($"EcoBot: áudio '{relativePath}' não encontrado. Rode Tools/generate_audio.py.");
            return clip;
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        static GameObject SavePrefab(GameObject go, string path)
        {
            EnsureFolder(Path.GetDirectoryName(path)?.Replace('\\', '/'));
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        static GameObject Child(GameObject parent, string name, Vector3 localPos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            go.transform.localPosition = localPos;
            return go;
        }

        /// <summary>
        /// Preenche um campo [SerializeField] privado de um componente pelo nome.
        /// Lança erro claro se o campo não existir (evita referências esquecidas).
        /// </summary>
        static void Set(Object target, string field, object value)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(field);
            if (p == null) throw new Exception($"EcoBot: campo '{field}' não existe em {target.GetType().Name}.");

            switch (value)
            {
                case null: p.objectReferenceValue = null; break;
                case Object o: p.objectReferenceValue = o; break;
                case Object[] arr:
                    p.arraySize = arr.Length;
                    for (int i = 0; i < arr.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = arr[i];
                    break;
                case Enum e: p.intValue = Convert.ToInt32(e); break;
                case int i: p.intValue = i; break;
                case float f: p.floatValue = f; break;
                case bool b: p.boolValue = b; break;
                case string s: p.stringValue = s; break;
                case Vector2 v2: p.vector2Value = v2; break;
                case Vector3 v3: p.vector3Value = v3; break;
                case Rect r: p.rectValue = r; break;
                case Color c: p.colorValue = c; break;
                default: throw new Exception($"EcoBot: tipo não suportado em Set: {value.GetType()}");
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static Material SpriteMaterial => AssetDatabase.GetBuiltinExtraResource<Material>("Sprites-Default.mat");
        static Font UIFont => Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out var c);
            return c;
        }
    }
}
