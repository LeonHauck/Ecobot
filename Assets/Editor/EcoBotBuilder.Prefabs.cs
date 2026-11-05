using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.UI;

namespace EcoBot.EditorTools
{
    public static partial class EcoBotBuilder
    {
        static GameObject playerPrefab, slimePrefab, dronePrefab, hedgehogPrefab;
        static GameObject seedPrefab, batteryPrefab, leafPrefab, checkpointPrefab, goalPrefab;
        static GameObject spikesPrefab, movingPlatformPrefab, fallingPlatformPrefab, signPrefab;
        static GameObject fxSparkle, fxDust, fxPoof, fxHurt, fxConfetti, floatingTextPrefab;

        static void CreatePrefabs()
        {
            EnsureFolder(PrefabsRoot);
            CreateEffectPrefabs();
            CreatePlayerPrefab();
            CreateEnemyPrefabs();
            CreateItemPrefabs();
            CreateLevelObjectPrefabs();
        }

        /// <summary>Cria um filho "Sprite" com SpriteRenderer (e Animator opcional).</summary>
        static SpriteRenderer AddSprite(GameObject parent, Sprite sprite, int order, AnimatorController controller = null, Vector3 localPos = default)
        {
            var go = Child(parent, "Sprite", localPos);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
            if (controller != null) go.AddComponent<Animator>().runtimeAnimatorController = controller;
            return sr;
        }

        // ================================================================ partículas e texto
        static void CreateEffectPrefabs()
        {
            string dir = PrefabsRoot + "/Effects";
            fxSparkle = Particles(dir, "FX_Sparkle", S("fx_sparkle"), Hex("fff3a0"), Color.white, 14, new Vector2(2f, 5f), new Vector2(0.25f, 0.45f), new Vector2(0.3f, 0.6f), 0.3f, 0.2f);
            fxDust = Particles(dir, "FX_Dust", S("fx_dust"), Hex("d8cfc0"), Hex("a89c8a"), 6, new Vector2(0.5f, 1.5f), new Vector2(0.25f, 0.45f), new Vector2(0.25f, 0.4f), -0.05f, 0.25f);
            fxPoof = Particles(dir, "FX_Poof", S("fx_dust"), Color.white, Hex("b8a8c8"), 12, new Vector2(1.5f, 3.5f), new Vector2(0.35f, 0.7f), new Vector2(0.3f, 0.5f), 0f, 0.3f);
            fxHurt = Particles(dir, "FX_Hurt", S("fx_pixel"), Hex("ff5a5a"), Hex("ffd0a0"), 16, new Vector2(3f, 6f), new Vector2(0.12f, 0.22f), new Vector2(0.3f, 0.5f), 1f, 0.2f);
            fxConfetti = Particles(dir, "FX_Confetti", S("fx_pixel"), Hex("6ed246"), Hex("ffe16e"), 50, new Vector2(4f, 9f), new Vector2(0.12f, 0.25f), new Vector2(0.8f, 1.4f), 1.2f, 0.5f);

            var go = new GameObject("FloatingText");
            var tm = go.AddComponent<TextMesh>();
            tm.font = UIFont;
            tm.fontSize = 64;
            tm.characterSize = 0.07f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.fontStyle = FontStyle.Bold;
            var mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterial = UIFont.material;
            mr.sortingOrder = 60;
            go.AddComponent<FloatingText>();
            floatingTextPrefab = SavePrefab(go, dir + "/FloatingText.prefab");
        }

        static GameObject Particles(string dir, string name, Sprite sprite, Color colorA, Color colorB, int count,
            Vector2 speed, Vector2 size, Vector2 lifetime, float gravity, float radius)
        {
            var go = new GameObject(name);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.duration = 1f;
            main.loop = false;
            main.playOnAwake = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(lifetime.x, lifetime.y);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed.x, speed.y);
            main.startSize = new ParticleSystem.MinMaxCurve(size.x, size.y);
            main.startColor = new ParticleSystem.MinMaxGradient(colorA, colorB);
            main.gravityModifier = gravity;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.stopAction = ParticleSystemStopAction.Destroy;
            main.maxParticles = count * 2;

            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = radius;

            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.6f), new GradientAlphaKey(0f, 1f) });
            col.color = g;

            var sizeOverLife = ps.sizeOverLifetime;
            sizeOverLife.enabled = true;
            sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.3f));

            var sheet = ps.textureSheetAnimation;
            sheet.enabled = true;
            sheet.mode = ParticleSystemAnimationMode.Sprites;
            if (sheet.spriteCount > 0) sheet.SetSprite(0, sprite);
            else sheet.AddSprite(sprite);

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = SpriteMaterial;
            renderer.sortingOrder = 50;

            return SavePrefab(go, $"{dir}/{name}.prefab");
        }

        // ================================================================ jogador
        static void CreatePlayerPrefab()
        {
            var noFriction = new PhysicsMaterial2D("SemAtrito") { friction = 0f, bounciness = 0f };
            EnsureFolder("Assets/Physics");
            string matPath = "Assets/Physics/SemAtrito.physicsMaterial2D";
            AssetDatabase.DeleteAsset(matPath);
            AssetDatabase.CreateAsset(noFriction, matPath);

            var go = new GameObject("Player") { tag = "Player" };
            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 3.5f;
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            var box = go.AddComponent<BoxCollider2D>();
            box.size = new Vector2(0.6f, 0.85f);
            box.offset = new Vector2(0f, -0.075f);      // base do collider = pés (y -0.5)
            box.sharedMaterial = noFriction;

            // Visual: pivô nos pés para o squash & stretch não "descolar" do chão.
            var visual = Child(go, "Visual", new Vector3(0f, -0.5f, 0f));
            var sr = AddSprite(visual, S("eco_idle_0"), 10, playerController, new Vector3(0f, 0.5f, 0f));

            var controller = go.AddComponent<PlayerController>();
            Set(controller, "animator", sr.GetComponent<Animator>());
            Set(controller, "spriteRenderer", sr);
            Set(controller, "visual", visual.transform);
            var health = go.AddComponent<PlayerHealth>();
            Set(health, "spriteRenderer", sr);

            playerPrefab = SavePrefab(go, PrefabsRoot + "/Player.prefab");
        }

        // ================================================================ inimigos
        static void CreateEnemyPrefabs()
        {
            string dir = PrefabsRoot + "/Enemies";
            slimePrefab = EnemyPrefab(dir, "GosmaDeLixo", S("slime_walk_0"), slimeController, true, new Vector2(0.8f, 0.6f), new Vector2(0f, -0.2f), patrolSpeed: 1.5f);
            hedgehogPrefab = EnemyPrefab(dir, "OuricoDeSucata", S("hedgehog_walk_0"), hedgehogController, false, new Vector2(0.85f, 0.6f), new Vector2(0f, -0.2f), patrolSpeed: 2.2f);
            dronePrefab = EnemyPrefab(dir, "DroneDeFumaca", S("drone_fly_0"), droneController, true, new Vector2(0.8f, 0.6f), new Vector2(0f, 0f), patrolSpeed: 0f);
        }

        static GameObject EnemyPrefab(string dir, string name, Sprite sprite, AnimatorController ac, bool stompable,
            Vector2 colliderSize, Vector2 colliderOffset, float patrolSpeed)
        {
            var go = new GameObject(name);
            var rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            var box = go.AddComponent<BoxCollider2D>();
            box.size = colliderSize;
            box.offset = colliderOffset;
            var sr = AddSprite(go, sprite, 6, ac);

            var enemy = go.AddComponent<Enemy>();
            Set(enemy, "canBeStomped", stompable);
            Set(enemy, "animator", sr.GetComponent<Animator>());

            if (patrolSpeed > 0f)
            {
                var mover = go.AddComponent<PatrolMover>();
                Set(mover, "speed", patrolSpeed);
                Set(mover, "spriteRenderer", sr);
            }
            else
            {
                var mover = go.AddComponent<FlyingMover>();
                Set(mover, "spriteRenderer", sr);
            }
            return SavePrefab(go, $"{dir}/{name}.prefab");
        }

        // ================================================================ itens
        static void CreateItemPrefabs()
        {
            string dir = PrefabsRoot + "/Items";
            seedPrefab = ItemPrefab(dir, "Semente", S("seed_0"), seedController, CollectibleType.Seed, 100);
            batteryPrefab = ItemPrefab(dir, "Bateria", S("battery_0"), batteryController, CollectibleType.Battery, 125);
            leafPrefab = ItemPrefab(dir, "FolhaDourada", S("leaf_0"), leafController, CollectibleType.GoldenLeaf, 0);
        }

        static GameObject ItemPrefab(string dir, string name, Sprite sprite, AnimatorController ac, CollectibleType type, int points)
        {
            var go = new GameObject(name);
            var circle = go.AddComponent<CircleCollider2D>();
            circle.isTrigger = true;
            circle.radius = 0.38f;
            var c = go.AddComponent<Collectible>();
            Set(c, "type", type);
            Set(c, "points", points);
            var sr = AddSprite(go, sprite, 5, ac);
            sr.gameObject.AddComponent<Bobbing>();
            return SavePrefab(go, $"{dir}/{name}.prefab");
        }

        // ================================================================ objetos de fase
        static void CreateLevelObjectPrefabs()
        {
            string dir = PrefabsRoot + "/Level";

            // Estação de recarga (checkpoint) — 1x2 blocos
            var cp = new GameObject("EstacaoDeRecarga");
            var cpBox = cp.AddComponent<BoxCollider2D>();
            cpBox.isTrigger = true;
            cpBox.size = new Vector2(1f, 2f);
            cpBox.offset = new Vector2(0f, 0.5f);
            var cpSr = AddSprite(cp, S("checkpoint_off"), 4, checkpointController, new Vector3(0f, 0.5f, 0f));
            Set(cp.AddComponent<Checkpoint>(), "animator", cpSr.GetComponent<Animator>());
            checkpointPrefab = SavePrefab(cp, dir + "/EstacaoDeRecarga.prefab");

            // Muda Ancestral (fim da fase) — 2x2 blocos
            var goal = new GameObject("MudaAncestral");
            var goalBox = goal.AddComponent<BoxCollider2D>();
            goalBox.isTrigger = true;
            goalBox.size = new Vector2(1.2f, 2f);
            goalBox.offset = new Vector2(0f, 0.5f);
            AddSprite(goal, S("goal_0"), 4, goalController, new Vector3(0f, 0.5f, 0f));
            goal.AddComponent<LevelGoal>();
            goalPrefab = SavePrefab(goal, dir + "/MudaAncestral.prefab");

            // Espinhos
            var spikes = new GameObject("Espinhos");
            spikes.AddComponent<SpriteRenderer>().sprite = S("spikes");
            spikes.GetComponent<SpriteRenderer>().sortingOrder = 4;
            var spBox = spikes.AddComponent<BoxCollider2D>();
            spBox.isTrigger = true;
            spBox.size = new Vector2(0.85f, 0.4f);
            spBox.offset = new Vector2(0f, -0.28f);
            spikes.AddComponent<Hazard>();
            spikesPrefab = SavePrefab(spikes, dir + "/Espinhos.prefab");

            // Plataforma móvel (3 blocos de largura)
            var mp = new GameObject("PlataformaMovel");
            var mpRb = mp.AddComponent<Rigidbody2D>();
            mpRb.bodyType = RigidbodyType2D.Kinematic;
            var mpBox = mp.AddComponent<BoxCollider2D>();
            mpBox.size = new Vector2(3f, 0.5f);
            var mpSr = mp.AddComponent<SpriteRenderer>();
            mpSr.sprite = S("moving_platform");
            mpSr.sortingOrder = 3;
            mp.AddComponent<MovingPlatform>();
            movingPlatformPrefab = SavePrefab(mp, dir + "/PlataformaMovel.prefab");

            // Plataforma que cai
            var fp = new GameObject("PlataformaRachada");
            var fpRb = fp.AddComponent<Rigidbody2D>();
            fpRb.bodyType = RigidbodyType2D.Kinematic;
            var fpBox = fp.AddComponent<BoxCollider2D>();
            fpBox.size = new Vector2(1f, 0.5f);
            var fpSr = AddSprite(fp, S("falling_0"), 3);
            var falling = fp.AddComponent<FallingPlatform>();
            Set(falling, "spriteRenderer", fpSr);
            Set(falling, "crackedSprite", S("falling_1"));
            fallingPlatformPrefab = SavePrefab(fp, dir + "/PlataformaRachada.prefab");

            // Placa de dica
            var sign = new GameObject("PlacaDeDica");
            var signSr = sign.AddComponent<SpriteRenderer>();
            signSr.sprite = S("sign");
            signSr.sortingOrder = -4;
            var signBox = sign.AddComponent<BoxCollider2D>();
            signBox.isTrigger = true;
            signBox.size = new Vector2(3f, 3f);
            signBox.offset = new Vector2(0f, 1f);
            sign.AddComponent<TutorialSign>();
            signPrefab = SavePrefab(sign, dir + "/PlacaDeDica.prefab");
        }

        // ================================================================ Systems (Resources)
        static void CreateSystemsPrefab()
        {
            var go = new GameObject("Systems");
            go.AddComponent<GameManager>();

            // Áudio: uma fonte para música e outra para efeitos.
            var music = go.AddComponent<AudioSource>();
            music.playOnAwake = false;
            music.loop = true;
            var sfx = go.AddComponent<AudioSource>();
            sfx.playOnAwake = false;
            var audio = go.AddComponent<AudioManager>();
            Set(audio, "musicSource", music);
            Set(audio, "sfxSource", sfx);
            Set(audio, "menuMusic", LoadAudio("Music/menu_theme"));
            Set(audio, "victoryMusic", LoadAudio("Music/victory"));
            SetSfxList(audio, new (Sfx, string, float, bool)[]
            {
                (Sfx.Jump, "jump", 0.6f, true),
                (Sfx.Land, "land", 0.5f, true),
                (Sfx.Collect, "collect", 0.7f, true),
                (Sfx.ExtraLife, "extra_life", 0.8f, false),
                (Sfx.Energy, "energy", 0.8f, false),
                (Sfx.Hurt, "hurt", 0.8f, false),
                (Sfx.Stomp, "stomp", 0.9f, true),
                (Sfx.EnemyHit, "enemy_hit", 0.7f, false),
                (Sfx.Checkpoint, "checkpoint", 0.8f, false),
                (Sfx.Crumble, "crumble", 0.7f, true),
                (Sfx.LevelComplete, "level_complete", 0.9f, false),
                (Sfx.GameOver, "game_over", 0.9f, false),
                (Sfx.UiMove, "ui_move", 0.4f, false),
                (Sfx.UiSelect, "ui_select", 0.6f, false),
                (Sfx.Pause, "pause", 0.6f, false),
            });

            var fx = go.AddComponent<Effects>();
            Set(fx, "sparkle", fxSparkle);
            Set(fx, "dust", fxDust);
            Set(fx, "poof", fxPoof);
            Set(fx, "hurt", fxHurt);
            Set(fx, "confetti", fxConfetti);
            Set(fx, "floatingText", floatingTextPrefab.GetComponent<FloatingText>());

            // Tela preta de transição entre cenas.
            var canvas = NewCanvas("Fader", 1000);
            canvas.transform.SetParent(go.transform, false);
            var black = NewImage(canvas.transform, "Preto", null, Color.black);
            Stretch(black.rectTransform);
            var group = black.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            Set(go.AddComponent<SceneFader>(), "group", group);

            EnsureFolder("Assets/Resources");
            SavePrefab(go, "Assets/Resources/Systems.prefab");
        }

        static void SetSfxList(AudioManager audio, (Sfx id, string file, float volume, bool randomPitch)[] list)
        {
            var so = new SerializedObject(audio);
            var arr = so.FindProperty("effects");
            arr.arraySize = list.Length;
            for (int i = 0; i < list.Length; i++)
            {
                var e = arr.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("id").intValue = (int)list[i].id;
                e.FindPropertyRelative("clip").objectReferenceValue = LoadAudio("SFX/" + list[i].file);
                e.FindPropertyRelative("volume").floatValue = list[i].volume;
                e.FindPropertyRelative("randomPitch").boolValue = list[i].randomPitch;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
