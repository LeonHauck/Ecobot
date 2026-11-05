using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace EcoBot.EditorTools
{
    public static partial class EcoBotBuilder
    {
        // ================================================================ temas visuais
        class Theme
        {
            public string top;
            public string[] fill;
            public string oneWay;
            public string climb;
            public string bgFar, bgMid;
            public Color cameraColor;
            public string[] decor;
            public float decorChance;
        }

        static readonly Theme Forest = new Theme
        {
            top = "forest_top", fill = new[] { "forest_fill_0", "forest_fill_1" }, oneWay = "oneway_wood", climb = "vine",
            bgFar = "forest_far", bgMid = "forest_mid", cameraColor = new Color32(150, 205, 230, 255),
            decor = new[] { "grass_tuft", "grass_tuft", "flower_red", "flower_yellow", "mushroom", "bush", "rock", "trash_bag", "trash_can" },
            decorChance = 0.35f
        };

        static readonly Theme Cave = new Theme
        {
            top = "cave_top", fill = new[] { "cave_fill_0", "cave_fill_1" }, oneWay = "oneway_wood", climb = "vine",
            bgFar = "cave_far", bgMid = "cave_mid", cameraColor = new Color32(34, 28, 50, 255),
            decor = new[] { "crystal", "glow_mushroom", "stalagmite", "rock", "trash_can" },
            decorChance = 0.25f
        };

        static readonly Theme Factory = new Theme
        {
            top = "factory_top", fill = new[] { "factory_fill_0", "factory_fill_1" }, oneWay = "oneway_metal", climb = "ladder_metal",
            bgFar = "factory_far", bgMid = "factory_mid", cameraColor = new Color32(170, 145, 130, 255),
            decor = new[] { "barrel", "pipe", "gear", "trash_bag", "trash_can" },
            decorChance = 0.2f
        };

        static readonly Theme Tower = new Theme
        {
            top = "tower_top", fill = new[] { "tower_fill_0", "tower_fill_1" }, oneWay = "oneway_metal", climb = "ladder_metal",
            bgFar = "tower_far", bgMid = "tower_mid", cameraColor = new Color32(120, 80, 120, 255),
            decor = new[] { "gear", "pipe", "plant_pot", "barrel" },
            decorChance = 0.2f
        };

        // ================================================================ definição das fases
        class LevelDef
        {
            public string scene;
            public string mapFile;
            public string displayName;
            public string intro;
            public Theme theme;
            public string music;
            public float parTime;
            public string[] hints;   // placas '1'..'9' do mapa
        }

        static readonly LevelDef[] Levels =
        {
            new LevelDef
            {
                scene = "Level1", mapFile = "Level1", displayName = "Floresta Despertar", theme = Forest,
                intro = "Recupere as sementes e chegue à Muda Ancestral!", music = "level1_forest", parTime = 150,
                hints = new[]
                {
                    "Use A/D ou ←/→ para andar.",
                    "Aperte ESPAÇO para pular. Segure para pular mais alto!",
                    "Gosmas de lixo! Pule em cima delas para reciclá-las.",
                    "Buraco largo! Segure SHIFT para correr e pular mais longe.",
                    "Cipós podem ser escalados com W/S ou ↑/↓.",
                    "Espinhos tiram energia. Pule por cima deles!"
                }
            },
            new LevelDef
            {
                scene = "Level2", mapFile = "Level2", displayName = "Caverna das Raízes", theme = Cave,
                intro = "As raízes da floresta estão presas no escuro.", music = "level2_cave", parTime = 180,
                hints = new[]
                {
                    "Ouriços de sucata têm espinhos: NÃO pise neles. Pule por cima!",
                    "Plataformas rachadas desabam logo depois que você pisa. Seja rápido!",
                    "Corredor baixo: calcule bem o pulo sobre os ouriços.",
                    "Pule no cipó e segure ↑ para se agarrar. Vá de cipó em cipó!"
                }
            },
            new LevelDef
            {
                scene = "Level3", mapFile = "Level3", displayName = "Fábrica Cinzenta", theme = Factory,
                intro = "A origem da poluição. Cuidado com o lodo tóxico!", music = "level3_factory", parTime = 200,
                hints = new[]
                {
                    "O lodo tóxico é fatal! Use as plataformas móveis para atravessar.",
                    "Drones de fumaça patrulham o ar. Pule em cima deles para desligá-los!",
                    "Dica: segure ↓ e aperte ESPAÇO para descer de uma plataforma.",
                    "Plataformas verticais sobem e descem. Espere o momento certo!"
                }
            },
            new LevelDef
            {
                scene = "Level4", mapFile = "Level4", displayName = "Chaminé Final", theme = Tower,
                intro = "Suba até o topo e instale a Muda Purificadora!", music = "level4_tower", parTime = 180,
                hints = new[]
                {
                    "A Chaminé Final! Use tudo o que aprendeu para chegar ao topo.",
                    "Plataformas rachadas em sequência: não pare de pular!",
                    "Quase lá! O filtro da chaminé está logo à frente."
                }
            }
        };

        static void BuildAllScenes()
        {
            EnsureFolder(ScenesRoot);
            BuildMenuScene();
            for (int i = 0; i < Levels.Length; i++) BuildLevelScene(Levels[i], i);
            BuildVictoryScene();
            EditorBuildSettings.scenes = SceneOrder.Select(s => new EditorBuildSettingsScene(ScenePath(s), true)).ToArray();
        }

        // ================================================================ cena de fase
        static void BuildLevelScene(LevelDef def, int index)
        {
            ShowProgress("Montando " + def.displayName, 0.6f + index * 0.08f);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            string[] lines = File.ReadAllLines($"Assets/LevelData/{def.mapFile}.txt")
                .Where(l => l.Length > 0).ToArray();
            int height = lines.Length;
            int width = lines.Max(l => l.Length);
            char At(int x, int row) => row >= 0 && row < height && x >= 0 && x < lines[row].Length ? lines[row][x] : '.';
            Vector3Int Cell(int x, int row) => new Vector3Int(x, height - 1 - row, 0);
            Vector3 Center(int x, int row) => new Vector3(x + 0.5f, height - 1 - row + 0.5f, 0f);
            var theme = def.theme;
            var rnd = new System.Random(1000 + index * 77);

            // ---- câmera e fundo
            var cam = CreateCamera(theme.cameraColor);
            var follow = cam.gameObject.AddComponent<CameraFollow>();
            var background = new GameObject("Fundo");
            CreateParallax(background.transform, theme.bgFar, 0.92f, -100);
            CreateParallax(background.transform, theme.bgMid, 0.75f, -90);

            // ---- tilemaps
            var grid = new GameObject("Grid", typeof(Grid));
            var terrain = CreateTilemap(grid.transform, "Terreno", 0);
            AddTilemapCollider(terrain, oneWay: false);
            var platforms = CreateTilemap(grid.transform, "Plataformas", 1);
            AddTilemapCollider(platforms, oneWay: true);
            var climbables = CreateTilemap(grid.transform, "Escalaveis", -3);
            climbables.gameObject.AddComponent<TilemapCollider2D>().isTrigger = true;
            Set(climbables.gameObject.AddComponent<Climbable>(), "tilemap", climbables);
            var toxic = CreateTilemap(grid.transform, "LodoToxico", 2);
            toxic.gameObject.AddComponent<TilemapCollider2D>().isTrigger = true;
            Set(toxic.gameObject.AddComponent<Hazard>(), "instantKill", true);
            var decor = CreateTilemap(grid.transform, "Decoracao", -5);

            var items = new GameObject("Itens").transform;
            var enemies = new GameObject("Inimigos").transform;
            var objects = new GameObject("Objetos").transform;
            var signs = new GameObject("Dicas").transform;

            GameObject player = null;
            int movingCount = 0;

            for (int row = 0; row < height; row++)
            {
                for (int x = 0; x < width; x++)
                {
                    char c = At(x, row);
                    Vector3 pos = Center(x, row);
                    switch (c)
                    {
                        case '#':
                            bool covered = At(x, row - 1) == '#' || At(x, row - 1) == '~';
                            string tileName = covered ? theme.fill[rnd.Next(theme.fill.Length)] : theme.top;
                            terrain.SetTile(Cell(x, row), tiles[tileName]);
                            break;
                        case '=':
                            platforms.SetTile(Cell(x, row), tiles[theme.oneWay]);
                            if (At(x, row + 1) == 'H') climbables.SetTile(Cell(x, row), tiles[theme.climb]);
                            break;
                        case 'H':
                            climbables.SetTile(Cell(x, row), tiles[theme.climb]);
                            break;
                        case '~':
                            toxic.SetTile(Cell(x, row), tiles[At(x, row - 1) == '~' ? "toxic_fill" : "toxic_top"]);
                            break;
                        case 'o': Place(seedPrefab, pos, items); break;
                        case 'b': Place(batteryPrefab, pos, items); break;
                        case 'L': Place(leafPrefab, pos, items); break;
                        case '^': Place(spikesPrefab, pos, objects); break;
                        case 'C': Place(checkpointPrefab, pos, objects); break;
                        case 'G': Place(goalPrefab, pos, objects); break;
                        case 'F': Place(fallingPlatformPrefab, pos + new Vector3(0f, 0.25f), objects); break;
                        case 's': Place(slimePrefab, pos, enemies); break;
                        case 'h': Place(hedgehogPrefab, pos, enemies); break;
                        case 'P': player = Place(playerPrefab, pos, null); break;
                        case 'd':
                        {
                            var drone = Place(dronePrefab, pos, enemies);
                            int end = FindMarkerRight(lines[row], x);
                            Vector3 b = end >= 0 ? Center(end, row) : pos + new Vector3(3f, 0f);
                            var mover = drone.GetComponent<FlyingMover>();
                            Set(mover, "pointA", (Vector2)pos);
                            Set(mover, "pointB", (Vector2)b);
                            break;
                        }
                        case 'M':
                        case 'V':
                        {
                            // A plataforma ocupa 3 blocos a partir do marcador, na metade de cima do bloco.
                            Vector3 offset = new Vector3(1f, 0.25f);
                            Vector3 a = pos + offset;
                            Vector3 b = a + new Vector3(4f, 0f);
                            if (c == 'M')
                            {
                                int end = FindMarkerRight(lines[row], x);
                                if (end >= 0) b = Center(end, row) + offset;
                            }
                            else
                            {
                                b = a + new Vector3(0f, 4f);
                                for (int r = row - 1; r >= 0; r--)
                                    if (At(x, r) == ':') { b = Center(x, r) + offset; break; }
                            }
                            var mp = Place(movingPlatformPrefab, a, objects).GetComponent<MovingPlatform>();
                            Set(mp, "pointA", (Vector2)a);
                            Set(mp, "pointB", (Vector2)b);
                            Set(mp, "startAtB", movingCount % 2 == 1); // plataformas vizinhas alternam
                            movingCount++;
                            break;
                        }
                        default:
                            if (c >= '1' && c <= '9')
                            {
                                int hint = c - '1';
                                var sign = Place(signPrefab, pos, signs);
                                string text = hint < def.hints.Length ? def.hints[hint] : "";
                                Set(sign.GetComponent<TutorialSign>(), "message", text);
                            }
                            break;
                    }
                }
            }

            // ---- decoração automática sobre o chão livre
            for (int row = 1; row < height; row++)
                for (int x = 0; x < width; x++)
                    if (At(x, row) == '#' && At(x, row - 1) == '.' && rnd.NextDouble() < theme.decorChance)
                        decor.SetTile(Cell(x, row - 1), tiles[theme.decor[rnd.Next(theme.decor.Length)]]);

            // ---- abismo: cair abaixo da fase custa uma vida
            var abyss = new GameObject("Abismo");
            var abyssBox = abyss.AddComponent<BoxCollider2D>();
            abyssBox.isTrigger = true;
            abyssBox.size = new Vector2(width + 40f, 2f);
            abyss.transform.position = new Vector3(width / 2f, -3f, 0f);
            Set(abyss.AddComponent<Hazard>(), "instantKill", true);

            // ---- informações da fase
            var bounds = new Rect(0f, 0f, width, height + 2f);
            var info = new GameObject("LevelInfo").AddComponent<LevelInfo>();
            Set(info, "levelIndex", index);
            Set(info, "displayName", def.displayName);
            Set(info, "introText", def.intro);
            Set(info, "music", LoadAudio("Music/" + def.music));
            Set(info, "parTime", def.parTime);
            Set(info, "cameraBounds", bounds);

            if (player == null) throw new System.Exception($"EcoBot: o mapa {def.mapFile} não tem 'P' (início do jogador).");
            Set(follow, "target", player.transform);
            Set(follow, "bounds", bounds);
            cam.transform.position = new Vector3(player.transform.position.x, player.transform.position.y, -10f);

            GenerateTilemapColliders(grid);
            BuildHUD();
            EditorSceneManager.SaveScene(scene, ScenePath(def.scene));
        }

        /// <summary>
        /// Força a criação imediata das formas de colisão dos Tilemaps.
        /// Sem isso a Unity só as monta no próximo quadro e a cena seria salva com o chão "vazio".
        /// </summary>
        static void GenerateTilemapColliders(GameObject grid)
        {
            foreach (var tc in grid.GetComponentsInChildren<TilemapCollider2D>()) tc.ProcessTilemapChanges();
            foreach (var cc in grid.GetComponentsInChildren<CompositeCollider2D>()) cc.GenerateGeometry();
        }

        static int FindMarkerRight(string line, int fromX)
        {
            for (int i = fromX + 1; i < line.Length; i++)
                if (line[i] == ':') return i;
            return -1;
        }

        static GameObject Place(GameObject prefab, Vector3 position, Transform parent)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.position = position;
            return go;
        }

        // ================================================================ blocos de montagem
        static Camera CreateCamera(Color background)
        {
            var go = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = go.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 6.5f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = background;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 100f;
            go.transform.position = new Vector3(0f, 0f, -10f);
            go.AddComponent<AudioListener>();
            return cam;
        }

        static void CreateParallax(Transform parent, string spriteName, float follow, int order)
        {
            var sprite = S(spriteName);
            var go = new GameObject("Parallax_" + spriteName);
            go.transform.SetParent(parent, false);
            go.transform.localScale = new Vector3(2f, 2f, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.size = new Vector2(sprite.bounds.size.x * 4f, sprite.bounds.size.y);
            sr.sortingOrder = order;
            Set(go.AddComponent<ParallaxLayer>(), "follow", follow);
        }

        static Tilemap CreateTilemap(Transform grid, string name, int order)
        {
            var go = new GameObject(name, typeof(Tilemap), typeof(TilemapRenderer));
            go.transform.SetParent(grid, false);
            go.GetComponent<TilemapRenderer>().sortingOrder = order;
            return go.GetComponent<Tilemap>();
        }

        /// <summary>Collider de tilemap unificado (CompositeCollider2D) — opcionalmente de mão única.</summary>
        static void AddTilemapCollider(Tilemap tilemap, bool oneWay)
        {
            var go = tilemap.gameObject;
            go.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Static;
            var tc = go.AddComponent<TilemapCollider2D>();
            tc.compositeOperation = Collider2D.CompositeOperation.Merge;
            var composite = go.AddComponent<CompositeCollider2D>();
            composite.geometryType = CompositeCollider2D.GeometryType.Polygons;
            if (!oneWay) return;
            composite.usedByEffector = true;
            var effector = go.AddComponent<PlatformEffector2D>();
            effector.useOneWay = true;
            effector.surfaceArc = 170f;
            effector.useSideFriction = false;
            effector.useSideBounce = false;
        }

        /// <summary>Cenário decorativo simples (menu e tela final): chão de tiles + fundo.</summary>
        static void BuildDecorativeStage(Theme theme)
        {
            var background = new GameObject("Fundo");
            CreateParallax(background.transform, theme.bgFar, 0.92f, -100);
            CreateParallax(background.transform, theme.bgMid, 0.75f, -90);

            var grid = new GameObject("Grid", typeof(Grid));
            var ground = CreateTilemap(grid.transform, "Terreno", 0);
            var decor = CreateTilemap(grid.transform, "Decoracao", -5);
            var rnd = new System.Random(5);
            for (int x = -14; x < 14; x++)
            {
                ground.SetTile(new Vector3Int(x, -4, 0), tiles[theme.top]);
                for (int y = -7; y < -4; y++) ground.SetTile(new Vector3Int(x, y, 0), tiles[theme.fill[rnd.Next(theme.fill.Length)]]);
                if (rnd.NextDouble() < 0.45) decor.SetTile(new Vector3Int(x, -3, 0), tiles[theme.decor[rnd.Next(4)]]);
            }
        }
    }
}
