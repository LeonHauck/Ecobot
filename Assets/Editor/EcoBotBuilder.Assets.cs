using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace EcoBot.EditorTools
{
    public static partial class EcoBotBuilder
    {
        static readonly Dictionary<string, Tile> tiles = new Dictionary<string, Tile>();

        static AnimatorController playerController, slimeController, droneController, hedgehogController;
        static AnimatorController seedController, batteryController, leafController, checkpointController, goalController;

        // ================================================================ TILES
        static void CreateTiles()
        {
            tiles.Clear();
            EnsureFolder(TilesRoot);

            string[] solid =
            {
                "forest_top", "forest_fill_0", "forest_fill_1", "cave_top", "cave_fill_0", "cave_fill_1",
                "factory_top", "factory_fill_0", "factory_fill_1", "tower_top", "tower_fill_0", "tower_fill_1",
                "oneway_wood", "oneway_metal", "vine", "ladder_wood", "ladder_metal", "toxic_top", "toxic_fill"
            };
            foreach (var name in solid) MakeTile(name, Tile.ColliderType.Grid);

            string[] decor =
            {
                "grass_tuft", "flower_red", "flower_yellow", "mushroom", "bush", "rock", "trash_bag", "trash_can",
                "crystal", "glow_mushroom", "stalagmite", "barrel", "gear", "pipe", "plant_pot"
            };
            foreach (var name in decor) MakeTile(name, Tile.ColliderType.None);
        }

        static void MakeTile(string spriteName, Tile.ColliderType collider)
        {
            string path = $"{TilesRoot}/{spriteName}.asset";
            var tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
            if (tile == null)
            {
                tile = ScriptableObject.CreateInstance<Tile>();
                AssetDatabase.CreateAsset(tile, path);
            }
            tile.sprite = S(spriteName);
            tile.colliderType = collider;
            tile.color = Color.white;
            EditorUtility.SetDirty(tile);
            tiles[spriteName] = tile;
        }

        // ================================================================ ANIMAÇÕES
        static void CreateAnimations()
        {
            EnsureFolder(AnimRoot + "/Player");
            EnsureFolder(AnimRoot + "/Enemies");
            EnsureFolder(AnimRoot + "/Items");
            CreatePlayerAnimator();
            CreateEnemyAnimators();
            CreateItemAnimators();
        }

        /// <summary>Cria (ou atualiza) um AnimationClip que troca o sprite do SpriteRenderer quadro a quadro.</summary>
        static AnimationClip Clip(string name, Sprite[] frames, float fps, bool loop)
        {
            string path = $"{AnimRoot}/{name}.anim";
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            bool isNew = clip == null;
            if (isNew) clip = new AnimationClip();
            clip.frameRate = fps;

            var binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
            var keys = new ObjectReferenceKeyframe[frames.Length + 1];
            for (int i = 0; i < frames.Length; i++)
                keys[i] = new ObjectReferenceKeyframe { time = i / fps, value = frames[i] };
            // Repete o último quadro no final para ele durar um quadro inteiro.
            keys[frames.Length] = new ObjectReferenceKeyframe { time = frames.Length / fps, value = frames[frames.Length - 1] };
            AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = loop;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            if (isNew) AssetDatabase.CreateAsset(clip, path);
            else EditorUtility.SetDirty(clip);
            return clip;
        }

        static AnimatorController NewController(string name)
        {
            string path = $"{AnimRoot}/{name}.controller";
            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(path) != null) AssetDatabase.DeleteAsset(path);
            return AnimatorController.CreateAnimatorControllerAtPath(path);
        }

        static AnimatorState State(AnimatorStateMachine sm, string name, Motion motion, Vector2 pos)
        {
            var s = sm.AddState(name, new Vector3(pos.x, pos.y, 0f));
            s.motion = motion;
            s.writeDefaultValues = false;
            return s;
        }

        static AnimatorStateTransition Go(AnimatorState from, AnimatorState to, params (AnimatorConditionMode mode, float value, string param)[] conditions)
        {
            var t = from.AddTransition(to);
            t.hasExitTime = false;
            t.duration = 0f;
            t.hasFixedDuration = true;
            foreach (var c in conditions) t.AddCondition(c.mode, c.value, c.param);
            return t;
        }

        static AnimatorStateTransition FromAny(AnimatorStateMachine sm, AnimatorState to, params (AnimatorConditionMode mode, float value, string param)[] conditions)
        {
            var t = sm.AddAnyStateTransition(to);
            t.hasExitTime = false;
            t.duration = 0f;
            t.canTransitionToSelf = false;
            foreach (var c in conditions) t.AddCondition(c.mode, c.value, c.param);
            return t;
        }

        const AnimatorConditionMode If = AnimatorConditionMode.If;
        const AnimatorConditionMode IfNot = AnimatorConditionMode.IfNot;
        const AnimatorConditionMode Greater = AnimatorConditionMode.Greater;
        const AnimatorConditionMode Less = AnimatorConditionMode.Less;

        /// <summary>
        /// Máquina de estados do Eco:
        /// Idle ⇄ Walk ⇄ Run (pela velocidade), Jump/Fall (no ar), Climb, Hurt e Win.
        /// </summary>
        static void CreatePlayerAnimator()
        {
            var idle0 = S("eco_idle_0");
            var idle1 = S("eco_idle_1");
            var idle2 = S("eco_idle_2");
            var idle = Clip("Player/Eco_Idle", new[] { idle0, idle1, idle0, idle1, idle0, idle2 }, 4f, true);
            var walk = Clip("Player/Eco_Walk", Frames("eco_walk", 4), 10f, true);
            var run = Clip("Player/Eco_Run", Frames("eco_run", 4), 14f, true);
            var jump = Clip("Player/Eco_Jump", new[] { S("eco_jump") }, 10f, true);
            var fall = Clip("Player/Eco_Fall", new[] { S("eco_fall") }, 10f, true);
            var climb = Clip("Player/Eco_Climb", Frames("eco_climb", 2), 6f, true);
            var hurtS = S("eco_hurt");
            var hurt = Clip("Player/Eco_Hurt", new[] { hurtS, hurtS, hurtS, hurtS }, 10f, false);
            var win = Clip("Player/Eco_Win", Frames("eco_win", 2), 4f, true);

            var ac = NewController("Player/Eco");
            ac.AddParameter("Speed", AnimatorControllerParameterType.Float);
            ac.AddParameter("Grounded", AnimatorControllerParameterType.Bool);
            ac.AddParameter("VelY", AnimatorControllerParameterType.Float);
            ac.AddParameter("Climbing", AnimatorControllerParameterType.Bool);
            ac.AddParameter("ClimbMove", AnimatorControllerParameterType.Float);
            ac.AddParameter("Hurt", AnimatorControllerParameterType.Trigger);
            ac.AddParameter("Win", AnimatorControllerParameterType.Trigger);

            var sm = ac.layers[0].stateMachine;
            var sIdle = State(sm, "Idle", idle, new Vector2(300, 0));
            var sWalk = State(sm, "Walk", walk, new Vector2(550, -80));
            var sRun = State(sm, "Run", run, new Vector2(800, 0));
            var sJump = State(sm, "Jump", jump, new Vector2(400, 200));
            var sFall = State(sm, "Fall", fall, new Vector2(700, 200));
            var sClimb = State(sm, "Climb", climb, new Vector2(50, 200));
            var sHurt = State(sm, "Hurt", hurt, new Vector2(50, -120));
            var sWin = State(sm, "Win", win, new Vector2(50, 360));
            sm.defaultState = sIdle;

            // A animação de escalar só anda enquanto o jogador se move no cipó.
            sClimb.speedParameterActive = true;
            sClimb.speedParameter = "ClimbMove";

            FromAny(sm, sWin, (If, 0, "Win"));
            FromAny(sm, sHurt, (If, 0, "Hurt"));
            FromAny(sm, sClimb, (If, 0, "Climbing"));

            Go(sClimb, sIdle, (IfNot, 0, "Climbing"));
            var hurtExit = sHurt.AddTransition(sIdle);
            hurtExit.hasExitTime = true;
            hurtExit.exitTime = 1f;
            hurtExit.duration = 0f;

            foreach (var ground in new[] { sIdle, sWalk, sRun })
            {
                Go(ground, sJump, (IfNot, 0, "Grounded"), (Greater, 0.1f, "VelY"));
                Go(ground, sFall, (IfNot, 0, "Grounded"), (Less, -1f, "VelY"));
            }
            Go(sIdle, sRun, (Greater, 7f, "Speed"));
            Go(sIdle, sWalk, (Greater, 0.1f, "Speed"));
            Go(sWalk, sIdle, (Less, 0.1f, "Speed"));
            Go(sWalk, sRun, (Greater, 7f, "Speed"));
            Go(sRun, sIdle, (Less, 0.1f, "Speed"));
            Go(sRun, sWalk, (Less, 7f, "Speed"));
            Go(sJump, sIdle, (If, 0, "Grounded"));
            Go(sJump, sFall, (Less, 0f, "VelY"));
            Go(sFall, sIdle, (If, 0, "Grounded"));

            EditorUtility.SetDirty(ac);
            playerController = ac;
        }

        static AnimatorController LoopWithDeath(string name, AnimationClip loop, AnimationClip death)
        {
            var ac = NewController(name);
            var sm = ac.layers[0].stateMachine;
            var sLoop = State(sm, "Move", loop, new Vector2(300, 0));
            sm.defaultState = sLoop;
            if (death != null)
            {
                ac.AddParameter("Die", AnimatorControllerParameterType.Trigger);
                var sDie = State(sm, "Die", death, new Vector2(300, 150));
                Go(sLoop, sDie, (If, 0, "Die"));
            }
            EditorUtility.SetDirty(ac);
            return ac;
        }

        static void CreateEnemyAnimators()
        {
            slimeController = LoopWithDeath("Enemies/Slime",
                Clip("Enemies/Slime_Walk", Frames("slime_walk", 4), 6f, true),
                Clip("Enemies/Slime_Die", new[] { S("slime_dead") }, 10f, false));
            droneController = LoopWithDeath("Enemies/Drone",
                Clip("Enemies/Drone_Fly", Frames("drone_fly", 3), 14f, true),
                Clip("Enemies/Drone_Die", new[] { S("drone_dead") }, 10f, false));
            hedgehogController = LoopWithDeath("Enemies/Hedgehog",
                Clip("Enemies/Hedgehog_Walk", Frames("hedgehog_walk", 4), 8f, true), null);
        }

        static void CreateItemAnimators()
        {
            seedController = LoopWithDeath("Items/Seed", Clip("Items/Seed_Spin", Frames("seed", 6), 10f, true), null);
            batteryController = LoopWithDeath("Items/Battery", Clip("Items/Battery_Glow", Frames("battery", 2), 3f, true), null);
            leafController = LoopWithDeath("Items/Leaf", Clip("Items/Leaf_Shine", Frames("leaf", 4), 6f, true), null);
            goalController = LoopWithDeath("Items/Goal", Clip("Items/Goal_Glow", Frames("goal", 4), 6f, true), null);

            var off = Clip("Items/Checkpoint_Off", new[] { S("checkpoint_off") }, 4f, true);
            var on = Clip("Items/Checkpoint_On", new[] { S("checkpoint_on_0"), S("checkpoint_on_1") }, 4f, true);
            var ac = NewController("Items/Checkpoint");
            ac.AddParameter("Active", AnimatorControllerParameterType.Bool);
            var sm = ac.layers[0].stateMachine;
            var sOff = State(sm, "Off", off, new Vector2(300, 0));
            var sOn = State(sm, "On", on, new Vector2(300, 150));
            sm.defaultState = sOff;
            Go(sOff, sOn, (If, 0, "Active"));
            Go(sOn, sOff, (IfNot, 0, "Active"));
            EditorUtility.SetDirty(ac);
            checkpointController = ac;
        }
    }
}
