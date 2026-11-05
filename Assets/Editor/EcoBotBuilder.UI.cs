using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace EcoBot.EditorTools
{
    public static partial class EcoBotBuilder
    {
        
        const string AuthorName = "Leon Hauck";

        static readonly Color TextColor = new Color(1f, 0.98f, 0.92f);
        static readonly Color Accent = new Color(0.55f, 0.95f, 0.45f);
        static readonly Color Gold = new Color(1f, 0.85f, 0.35f);

        const string StoryText =
            "Há muito tempo, a Floresta de Verdália era cheia de vida. Até que a Fábrica Cinzenta " +
            "começou a despejar lixo e fumaça, e as Sementes Ancestrais se espalharam pelo caminho.\n\n" +
            "Você é ECO, um pequeno robô reciclador despertado pela última árvore da floresta. " +
            "Sua missão: recuperar as sementes, reciclar as criaturas de lixo e plantar a Muda Ancestral " +
            "em cada região até alcançar a Chaminé Final.\n\nSó assim Verdália poderá respirar de novo.";

        const string HowToText =
            "<color=#8cf07a>CONTROLES</color>\n" +
            "A / D  ou  ← / →        Andar\n" +
            "SHIFT (segurar)           Correr\n" +
            "ESPAÇO                       Pular (segure para ir mais alto)\n" +
            "W / S  ou  ↑ / ↓          Escalar cipós e escadas\n" +
            "↓ + ESPAÇO                Descer de plataformas\n" +
            "ESC ou P                     Pausar          M   Som liga/desliga\n" +
            "Gamepad: analógico, A pula, X corre, Start pausa\n\n" +
            "<color=#8cf07a>OBJETIVO</color>\n" +
            "Colete sementes (+100), pule nos inimigos (+200) e chegue à Muda Ancestral.\n" +
            "Baterias recuperam energia e folhas douradas dão +1 vida.\n" +
            "Estações de recarga salvam seu progresso e recarregam a energia.";

        static string CreditsText =>
            "<color=#8cf07a>EcoBot: Raízes do Amanhã</color>\n" +
            "Projeto da disciplina de Game Development — UniFECAF\n\n" +
            $"Desenvolvimento, programação e design de fases\n{AuthorName}\n\n" +
            "Arte pixel art e áudio (efeitos e trilhas chiptune)\n" +
            "Gerados proceduralmente por scripts próprios (pasta Tools/)\n\n" +
            "Feito com Unity 6 e C#";

        const string EndingText =
            "A Muda Purificadora foi instalada na Chaminé Final! A fumaça virou ar limpo " +
            "e as sementes recuperadas já brotam por toda Verdália.\nObrigado, Eco!";

        // ================================================================ blocos de UI
        static Canvas NewCanvas(string name, int order)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = order;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        static RectTransform NewRect(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        /// <summary>Posiciona um RectTransform a partir de uma âncora (0..1) e tamanho em pixels.</summary>
        static void Pos(RectTransform rt, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = anchor;
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
        }

        static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        static Image NewImage(Transform parent, string name, Sprite sprite, Color color, bool sliced = false)
        {
            var rt = NewRect(parent, name);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.raycastTarget = false;
            if (sliced)
            {
                img.type = Image.Type.Sliced;
                img.pixelsPerUnitMultiplier = 2f;
            }
            else if (sprite != null)
            {
                img.preserveAspect = true;
            }
            return img;
        }

        static Text NewText(Transform parent, string name, string content, int size, TextAnchor align, Color color)
        {
            var rt = NewRect(parent, name);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = UIFont;
            t.text = content;
            t.fontSize = size;
            t.fontStyle = FontStyle.Bold;
            t.alignment = align;
            t.color = color;
            t.supportRichText = true;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            var outline = rt.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.06f, 0.08f, 0.12f, 0.9f);
            outline.effectDistance = new Vector2(2f, -2f);
            return t;
        }

        static Button NewButton(Transform parent, string name, string label, Vector2 position, Vector2? size = null)
        {
            var img = NewImage(parent, name, S("ui_button"), Color.white, sliced: true);
            img.raycastTarget = true;
            Pos(img.rectTransform, new Vector2(0.5f, 0.5f), position, size ?? new Vector2(320, 56));
            img.rectTransform.pivot = new Vector2(0.5f, 0.5f);

            var button = img.gameObject.AddComponent<Button>();
            var colors = button.colors;
            colors.normalColor = new Color(0.78f, 0.88f, 0.82f);
            colors.highlightedColor = new Color(1f, 0.95f, 0.6f);
            colors.selectedColor = new Color(1f, 0.85f, 0.35f);
            colors.pressedColor = new Color(0.6f, 0.75f, 0.6f);
            colors.fadeDuration = 0.05f;
            button.colors = colors;
            button.targetGraphic = img;
            img.gameObject.AddComponent<UIButtonSfx>();

            var text = NewText(img.transform, "Texto", label, 26, TextAnchor.MiddleCenter, new Color(0.1f, 0.15f, 0.2f));
            Object.DestroyImmediate(text.GetComponent<Outline>());
            Stretch(text.rectTransform);
            return button;
        }

        static RectTransform NewPanel(Transform parent, string name, Vector2 size, Vector2 position = default)
        {
            var img = NewImage(parent, name, S("ui_panel"), Color.white, sliced: true);
            img.raycastTarget = true;
            Pos(img.rectTransform, new Vector2(0.5f, 0.5f), position, size);
            img.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            return img.rectTransform;
        }

        static Text PanelText(RectTransform panel, string name, string content, int size, float y, float height, Color color, TextAnchor align = TextAnchor.UpperCenter)
        {
            var t = NewText(panel, name, content, size, align, color);
            t.rectTransform.anchorMin = new Vector2(0f, 1f);
            t.rectTransform.anchorMax = new Vector2(1f, 1f);
            t.rectTransform.pivot = new Vector2(0.5f, 1f);
            t.rectTransform.anchoredPosition = new Vector2(0f, y);
            t.rectTransform.sizeDelta = new Vector2(-60f, height);
            return t;
        }

        static void NewEventSystem()
        {
            var go = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            var type = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (type != null) { go.AddComponent(type); return; }
#endif
            go.AddComponent<StandaloneInputModule>();
        }

        // ================================================================ HUD das fases
        static void BuildHUD()
        {
            var canvas = NewCanvas("HUD", 10);
            var root = canvas.transform;
            var topLeft = new Vector2(0f, 1f);
            var topRight = new Vector2(1f, 1f);
            var topCenter = new Vector2(0.5f, 1f);

            // vidas e energia (canto superior esquerdo)
            var lifeIcon = NewImage(root, "IconeVida", S("icon_life"), Color.white);
            Pos(lifeIcon.rectTransform, topLeft, new Vector2(18, -14), new Vector2(52, 52));
            var lives = NewText(root, "Vidas", "x 3", 30, TextAnchor.MiddleLeft, TextColor);
            Pos(lives.rectTransform, topLeft, new Vector2(74, -14), new Vector2(90, 52));
            lives.rectTransform.pivot = new Vector2(0f, 1f);
            var energy = new Image[3];
            for (int i = 0; i < 3; i++)
            {
                energy[i] = NewImage(root, "Energia" + (i + 1), S("icon_energy_full"), Color.white);
                Pos(energy[i].rectTransform, topLeft, new Vector2(20 + i * 42, -70), new Vector2(40, 40));
            }

            // nome da fase e tempo (centro)
            var levelName = NewText(root, "NomeDaFase", "", 24, TextAnchor.UpperCenter, Accent);
            Pos(levelName.rectTransform, topCenter, new Vector2(0, -12), new Vector2(500, 32));
            var time = NewText(root, "Tempo", "0:00", 26, TextAnchor.UpperCenter, TextColor);
            Pos(time.rectTransform, topCenter, new Vector2(0, -44), new Vector2(200, 32));

            // sementes e pontos (canto superior direito)
            var seedIcon = NewImage(root, "IconeSemente", S("icon_seed"), Color.white);
            Pos(seedIcon.rectTransform, topRight, new Vector2(-150, -14), new Vector2(48, 48));
            seedIcon.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            seedIcon.rectTransform.anchoredPosition = new Vector2(-170, -38);
            var seeds = NewText(root, "Sementes", "0/0", 30, TextAnchor.MiddleLeft, Gold);
            Pos(seeds.rectTransform, topRight, new Vector2(-20, -14), new Vector2(120, 48));
            var score = NewText(root, "Pontos", "000000", 30, TextAnchor.MiddleRight, TextColor);
            Pos(score.rectTransform, topRight, new Vector2(-20, -62), new Vector2(240, 40));

            // título da fase (centro da tela)
            var title = NewRect(root, "TituloDaFase");
            Pos(title, new Vector2(0.5f, 0.5f), new Vector2(0, 90), new Vector2(900, 220));
            title.pivot = new Vector2(0.5f, 0.5f);
            var titleGroup = title.gameObject.AddComponent<CanvasGroup>();
            titleGroup.blocksRaycasts = false;
            var titleText = NewText(title, "Titulo", "FASE 1", 64, TextAnchor.UpperCenter, Gold);
            Stretch(titleText.rectTransform);
            var subtitle = NewText(title, "Subtitulo", "", 34, TextAnchor.UpperCenter, TextColor);
            Stretch(subtitle.rectTransform);
            subtitle.rectTransform.offsetMax = new Vector2(0, -80);

            // dicas das placas (parte de baixo)
            var hint = NewPanel(root, "Dica", new Vector2(860, 84));
            hint.anchorMin = hint.anchorMax = new Vector2(0.5f, 0f);
            hint.anchoredPosition = new Vector2(0, 70);
            var hintGroup = hint.gameObject.AddComponent<CanvasGroup>();
            hintGroup.blocksRaycasts = false;
            var hintText = NewText(hint, "Texto", "", 26, TextAnchor.MiddleCenter, TextColor);
            Stretch(hintText.rectTransform);
            hintText.rectTransform.offsetMin = new Vector2(24, 8);
            hintText.rectTransform.offsetMax = new Vector2(-24, -8);

            // painel: fase concluída
            var complete = NewPanel(root, "PainelFaseConcluida", new Vector2(560, 440));
            PanelText(complete, "Titulo", "FASE CONCLUÍDA!", 44, -28, 60, Accent);
            var completeText = PanelText(complete, "Resultado", "", 28, -100, 240, TextColor);
            var continueBtn = NewButton(complete, "Continuar", "Continuar", new Vector2(0, -170));

            // painel: game over
            var over = NewPanel(root, "PainelGameOver", new Vector2(560, 400));
            PanelText(over, "Titulo", "GAME OVER", 52, -30, 70, new Color(1f, 0.45f, 0.4f));
            var overText = PanelText(over, "Texto", "", 28, -110, 100, TextColor);
            var retry = NewButton(over, "TentarDeNovo", "Tentar de novo", new Vector2(0, -60));
            var overMenu = NewButton(over, "MenuPrincipal", "Menu principal", new Vector2(0, -130));

            // painel: pausa
            var pause = NewPanel(root, "PainelPausa", new Vector2(560, 470));
            PanelText(pause, "Titulo", "PAUSADO", 52, -28, 70, Gold);
            PanelText(pause, "Dica", "ESC ou P para continuar  •  M liga/desliga o som", 20, -100, 30, TextColor);
            var resume = NewButton(pause, "Continuar", "Continuar", new Vector2(0, 20));
            var restart = NewButton(pause, "Reiniciar", "Reiniciar fase", new Vector2(0, -50));
            var pauseMenu = NewButton(pause, "Menu", "Menu principal", new Vector2(0, -120));

            var hud = canvas.gameObject.AddComponent<HUD>();
            Set(hud, "livesText", lives);
            Set(hud, "scoreText", score);
            Set(hud, "seedsText", seeds);
            Set(hud, "timeText", time);
            Set(hud, "levelNameText", levelName);
            Set(hud, "energyIcons", energy);
            Set(hud, "energyFull", S("icon_energy_full"));
            Set(hud, "energyEmpty", S("icon_energy_empty"));
            Set(hud, "seedIcon", seedIcon.rectTransform);
            Set(hud, "titleGroup", titleGroup);
            Set(hud, "titleText", titleText);
            Set(hud, "subtitleText", subtitle);
            Set(hud, "hintGroup", hintGroup);
            Set(hud, "hintText", hintText);
            Set(hud, "levelCompletePanel", complete.gameObject);
            Set(hud, "levelCompleteText", completeText);
            Set(hud, "continueButton", continueBtn);
            Set(hud, "gameOverPanel", over.gameObject);
            Set(hud, "gameOverText", overText);
            Set(hud, "retryButton", retry);
            Set(hud, "gameOverMenuButton", overMenu);
            Set(hud, "pausePanel", pause.gameObject);
            Set(hud, "resumeButton", resume);
            Set(hud, "restartButton", restart);
            Set(hud, "pauseMenuButton", pauseMenu);

            NewEventSystem();
        }

        // ================================================================ menu principal
        static void BuildMenuScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cam = CreateCamera(Forest.cameraColor);
            cam.orthographicSize = 5f;
            BuildDecorativeStage(Forest);

            // Eco e a Muda Ancestral no cenário
            var eco = new GameObject("Eco");
            eco.transform.position = new Vector3(-5.5f, -2f, 0f);
            eco.transform.localScale = new Vector3(2f, 2f, 1f);
            var ecoSr = eco.AddComponent<SpriteRenderer>();
            ecoSr.sprite = S("eco_idle_0");
            ecoSr.sortingOrder = 10;
            eco.AddComponent<Animator>().runtimeAnimatorController = playerController;
            var tree = new GameObject("Muda");
            tree.transform.position = new Vector3(6f, -1.5f, 0f);
            tree.transform.localScale = new Vector3(1.5f, 1.5f, 1f);
            var treeSr = tree.AddComponent<SpriteRenderer>();
            treeSr.sprite = S("goal_0");
            treeSr.sortingOrder = 4;
            tree.AddComponent<Animator>().runtimeAnimatorController = goalController;

            var canvas = NewCanvas("Menu", 10);
            var root = canvas.transform;

            var title = NewText(root, "Titulo", "ECOBOT", 110, TextAnchor.MiddleCenter, Accent);
            Pos(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -40), new Vector2(900, 130));
            title.GetComponent<Outline>().effectDistance = new Vector2(5f, -5f);
            var subtitle = NewText(root, "Subtitulo", "Raízes do Amanhã", 40, TextAnchor.MiddleCenter, Gold);
            Pos(subtitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -165), new Vector2(900, 50));

            // painel principal
            var main = NewRect(root, "PainelPrincipal");
            Stretch(main);
            var play = NewButton(main, "Jogar", "Jogar", new Vector2(0, 20));
            var howTo = NewButton(main, "ComoJogar", "Como jogar", new Vector2(0, -50));
            var credits = NewButton(main, "Creditos", "Créditos", new Vector2(0, -120));
            var quit = NewButton(main, "Sair", "Sair", new Vector2(0, -190));
            var best = NewText(main, "Recorde", "", 24, TextAnchor.MiddleCenter, Gold);
            Pos(best.rectTransform, new Vector2(0.5f, 0f), new Vector2(0, 16), new Vector2(600, 34));

            // história
            var story = NewPanel(root, "PainelHistoria", new Vector2(900, 440), new Vector2(0, -100));
            PanelText(story, "Titulo", "A HISTÓRIA", 40, -24, 50, Gold);
            PanelText(story, "Texto", StoryText, 24, -80, 280, TextColor);
            var start = NewButton(story, "Comecar", "Começar aventura!", new Vector2(0, -170), new Vector2(380, 56));

            // como jogar
            var howToPanel = NewPanel(root, "PainelComoJogar", new Vector2(900, 440), new Vector2(0, -100));
            PanelText(howToPanel, "Titulo", "COMO JOGAR", 40, -24, 50, Gold);
            PanelText(howToPanel, "Texto", HowToText, 21, -76, 300, TextColor, TextAnchor.UpperLeft);
            var backHowTo = NewButton(howToPanel, "Voltar", "Voltar", new Vector2(0, -180));

            // créditos
            var creditsPanel = NewPanel(root, "PainelCreditos", new Vector2(900, 440), new Vector2(0, -100));
            PanelText(creditsPanel, "Titulo", "CRÉDITOS", 40, -24, 50, Gold);
            PanelText(creditsPanel, "Texto", CreditsText, 24, -80, 280, TextColor);
            var backCredits = NewButton(creditsPanel, "Voltar", "Voltar", new Vector2(0, -180));

            var menu = canvas.gameObject.AddComponent<MainMenu>();
            Set(menu, "mainPanel", main.gameObject);
            Set(menu, "storyPanel", story.gameObject);
            Set(menu, "howToPanel", howToPanel.gameObject);
            Set(menu, "creditsPanel", creditsPanel.gameObject);
            Set(menu, "playButton", play);
            Set(menu, "howToButton", howTo);
            Set(menu, "creditsButton", credits);
            Set(menu, "quitButton", quit);
            Set(menu, "startGameButton", start);
            Set(menu, "backButtons", new Object[] { backHowTo, backCredits });
            Set(menu, "bestScoreText", best);

            NewEventSystem();
            EditorSceneManager.SaveScene(scene, ScenePath("MainMenu"));
        }

        // ================================================================ tela de vitória
        static void BuildVictoryScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cam = CreateCamera(Forest.cameraColor);
            cam.orthographicSize = 5f;
            BuildDecorativeStage(Forest);

            var eco = new GameObject("Eco");
            eco.transform.position = new Vector3(-1.6f, -2f, 0f);
            eco.transform.localScale = new Vector3(2f, 2f, 1f);
            var ecoSr = eco.AddComponent<SpriteRenderer>();
            ecoSr.sprite = S("eco_win_0");
            ecoSr.sortingOrder = 10;
            var ecoAnimator = eco.AddComponent<Animator>();
            ecoAnimator.runtimeAnimatorController = playerController;
            var tree = new GameObject("MudaPurificadora");
            tree.transform.position = new Vector3(1.8f, -1.5f, 0f);
            tree.transform.localScale = new Vector3(1.5f, 1.5f, 1f);
            var treeSr = tree.AddComponent<SpriteRenderer>();
            treeSr.sprite = S("goal_0");
            treeSr.sortingOrder = 4;
            tree.AddComponent<Animator>().runtimeAnimatorController = goalController;

            var canvas = NewCanvas("Vitoria", 10);
            var root = canvas.transform;
            var title = NewText(root, "Titulo", "VERDÁLIA ESTÁ SALVA!", 64, TextAnchor.MiddleCenter, Accent);
            Pos(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -30), new Vector2(1100, 80));
            var ending = NewText(root, "Final", EndingText, 26, TextAnchor.UpperCenter, TextColor);
            Pos(ending.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -120), new Vector2(1000, 110));
            var result = NewText(root, "Resultado", "", 30, TextAnchor.UpperCenter, Gold);
            Pos(result.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -235), new Vector2(900, 120));
            var again = NewButton(root, "JogarNovamente", "Jogar novamente", new Vector2(-180, -250));
            var menuBtn = NewButton(root, "Menu", "Menu principal", new Vector2(180, -250));

            var victory = canvas.gameObject.AddComponent<VictoryScreen>();
            Set(victory, "resultText", result);
            Set(victory, "playAgainButton", again);
            Set(victory, "menuButton", menuBtn);
            Set(victory, "ecoAnimator", ecoAnimator);

            NewEventSystem();
            EditorSceneManager.SaveScene(scene, ScenePath("Victory"));
        }
    }
}
