<p align="center">
  <img src="Docs/Imagens/eco.png" width="120" alt="Eco, o robô reciclador">
</p>

<h1 align="center">EcoBot: Raízes do Amanhã</h1>

<p align="center">
  <img src="https://img.shields.io/badge/Unity-6.6-000000?logo=unity&logoColor=white" alt="Unity 6.6">
  <img src="https://img.shields.io/badge/C%23-scripts-512BD4?logo=dotnet&logoColor=white" alt="C#">
  <img src="https://img.shields.io/badge/plataformas-Windows%20%7C%20Navegador-0078D6" alt="Windows e navegador">
  <img src="https://img.shields.io/badge/arte-pixel%20art%20original-4cbb6c" alt="Pixel art original">
  <img src="https://img.shields.io/badge/ferramentas-Python-3776AB?logo=python&logoColor=white" alt="Python">
  <img src="https://img.shields.io/badge/status-completo-2e8c57" alt="Status: completo">
</p>

<p align="center">
  Jogo de plataforma 2D feito na <b>Unity 6</b> com <b>C#</b>, desenvolvido como projeto da disciplina de Game Development (UniFECAF).
</p>

<p align="center">
  <a href="https://leonhauck.github.io/Ecobot/"><img src="https://img.shields.io/badge/Jogar_no_navegador-2e8c57?style=for-the-badge" alt="Jogar no navegador"></a>
  <a href="https://github.com/LeonHauck/Ecobot/releases/latest"><img src="https://img.shields.io/badge/Baixar_para_Windows-0078D6?style=for-the-badge" alt="Baixar para Windows"></a>
</p>

> 🌱 A Floresta de Verdália está sendo sufocada pelo lixo e pela fumaça da Fábrica Cinzenta. Você controla **Eco**, um pequeno robô reciclador despertado pela última árvore da floresta. A missão dele é recuperar as Sementes Ancestrais, reciclar as criaturas de lixo e plantar a Muda Ancestral em cada região até chegar à Chaminé Final.

<p align="center">
  <img src="Docs/Imagens/trailer.gif" width="720" alt="Trailer do EcoBot: trechos das 4 fases">
</p>

---

## 📑 Sumário
- 🎯 [Jogar agora](#-jogar-agora)
- 🎬 [Trailer](#-trailer)
- 🎮 [Como jogar](#-como-jogar)
- 🧩 [Mecânicas](#-mecânicas)
- 🌎 [Fases](#-fases)
- 🚀 [Como abrir e executar](#-como-abrir-e-executar)
- 📁 [Estrutura do projeto](#-estrutura-do-projeto)
- 🧠 [Arquitetura do código](#-arquitetura-do-código)
- 📝 [Editor de fases em texto (ASCII)](#-editor-de-fases-em-texto-ascii)
- 🎨 [Arte, áudio e créditos](#-arte-áudio-e-créditos)
- 📸 [Prints](#-prints)
- 📚 [Documentação](#-documentação)


---

## 🎯 Jogar agora

| Onde | Como |
|---|---|
| 🌐 **Navegador** | Acesse **[leonhauck.github.io/Ecobot](https://leonhauck.github.io/Ecobot/)** e jogue na hora, sem instalar nada (computador com teclado ou gamepad). |
| 💾 **Windows** | Baixe o `EcoBot_Windows.zip` na **[página de Releases](https://github.com/LeonHauck/Ecobot/releases/latest)**, extraia e abra o `EcoBot.exe`. |

---

## 🎬 Trailer

https://github.com/user-attachments/assets/d176d3eb-cc72-47f9-b1bf-d73c2cd30b7f


---

## 🎮 Como jogar

| Ação | Teclado | Gamepad |
|---|---|---|
| Andar | `A` / `D` ou `←` / `→` | Analógico / D-pad |
| Correr | Segurar `Shift` | Segurar `X` |
| Pular | `Espaço` (segure para pular mais alto) | `A` |
| Escalar cipós e escadas | `W` / `S` ou `↑` / `↓` | Analógico |
| Descer de uma plataforma | `↓` + `Espaço` | Baixo + `A` |
| Pausar | `Esc` ou `P` | `Start` |
| Tirar print | `F12` | — |
| Ligar/desligar o som | `M` | — |

**Objetivo:** chegar à Muda Ancestral no fim de cada fase. Pelo caminho:

| Item / objeto | Efeito |
|---|---|
| 🌰 Semente dourada | +100 pontos (pegar todas da fase dá +1000 de bônus) |
| 🔋 Bateria | Recupera 1 de energia (se já estiver cheia, vira pontos) |
| 🍃 Folha dourada | +1 vida |
| 🔌 Estação de recarga | Checkpoint: salva o ponto de renascimento e recarrega a energia |
| 🦶 Pisar em inimigo | +200 pontos |
| ⏱️ Terminar rápido | Bônus de tempo (10 pontos por segundo abaixo do tempo-alvo) |

O Eco tem **3 de energia** por vida. Inimigos e espinhos tiram 1 de energia, com recuo e alguns segundos de invencibilidade. Cair num abismo ou no lodo tóxico custa uma vida inteira. Sem vidas, aparece a tela de Game Over com a opção de tentar a fase de novo.

---

## 🧩 Mecânicas

**🏃 Movimento** ([PlayerController.cs](Assets/Scripts/Player/PlayerController.cs))
- **Andar e correr** com aceleração e desaceleração (o personagem não para nem arranca de forma brusca).
- **Pulo responsivo:**
  - *coyote time*: ainda dá para pular um instante depois de sair da borda;
  - *jump buffer*: o pulo apertado um pouco antes de tocar o chão também vale;
  - *altura variável*: soltar o botão cedo corta o pulo;
  - gravidade maior na descida, para o pulo ficar mais firme.
- **Escalar** cipós e escadas, que centralizam o personagem na coluna. É possível agarrar no ar e pular de um cipó para outro.
- **Plataformas de mão única**: dá para atravessar por baixo e descer com `↓ + Espaço`.
- **Plataformas móveis** levam o jogador junto, e **plataformas rachadas** desabam logo depois de pisadas.
- **Squash & stretch** (o sprite estica no pulo e achata na aterrissagem) e partículas de poeira.

**👾 Inimigos** ([Assets/Scripts/Enemies](Assets/Scripts/Enemies))

<p align="center"><img src="Docs/Imagens/personagens.png" alt="Eco, Gosma de Lixo, Ouriço de Sucata, Drone de Fumaça e Semente"><br><sub>Eco · Gosma de Lixo · Ouriço de Sucata · Drone de Fumaça · Semente</sub></p>

| Inimigo | Comportamento | Pode pisar? |
|---|---|---|
| Gosma de Lixo | Patrulha e vira ao encontrar parede ou borda | Sim |
| Ouriço de Sucata | Patrulha mais rápido e tem espinhos nas costas | **Não**, é preciso pular por cima |
| Drone de Fumaça | Voa em vai-e-vem balançando | Sim, e serve de "trampolim" |

**✨ Feedback ao jogador**
- **Visual:** partículas ao coletar, pisar, levar dano e terminar a fase; textos flutuantes ("+100", "+1 VIDA"); personagem piscando durante a invencibilidade; tela tremendo ao levar dano; ícones do HUD que "pulsam" quando o valor muda.
- **Sonoro:** efeitos para pulo, aterrissagem, coleta, energia, vida extra, dano, pisão, checkpoint, plataforma desabando, fase concluída, game over e navegação nos menus. Cada fase tem uma trilha própria, além do menu e da tela final.

**🖥️ HUD** ([HUD.cs](Assets/Scripts/UI/HUD.cs)): vidas, células de energia, sementes da fase (coletadas/total), pontuação, tempo, nome da fase, dicas das placas e painéis de pausa, fim de fase e game over.

---

## 🌎 Fases

| # | Fase | Novidades | Elementos |
|---|---|---|---|
| 1 | **Floresta Despertar** | Tutorial com placas: andar, pular, correr, escalar, pisar em inimigos, espinhos | 25 sementes, 3 gosmas, 1 checkpoint |
| 2 | **Caverna das Raízes** | Ouriços (não dá para pisar), plataformas que desabam, cipós sobre o abismo, corredor baixo | 20 sementes, 5 ouriços, 7 plataformas rachadas |
| 3 | **Fábrica Cinzenta** | Lodo tóxico, plataformas móveis horizontais e verticais, drones voadores, descida de plataformas | 17 sementes, 6 plataformas móveis, 3 drones |
| 4 | **Chaminé Final** | Subida vertical que combina tudo o que foi aprendido | 16 sementes, 4 drones, 4 ouriços, 3 checkpoints |

A dificuldade cresce porque cada fase apresenta um desafio novo em um lugar seguro e depois o combina com os anteriores. Mapas completos:

<details>
<summary>Ver mapas das 4 fases</summary>

**Fase 1: Floresta Despertar**
![Fase 1](Docs/Mapas/Level1_mapa.png)

**Fase 2: Caverna das Raízes**
![Fase 2](Docs/Mapas/Level2_mapa.png)

**Fase 3: Fábrica Cinzenta**
![Fase 3](Docs/Mapas/Level3_mapa.png)

**Fase 4: Chaminé Final**
<img src="Docs/Mapas/Level4_mapa.png" height="600">

</details>

---

## 🚀 Como abrir e executar

### ▶️ Jogar o executável
1. Baixe o `EcoBot_Windows.zip` na [página de Releases](https://github.com/LeonHauck/Ecobot/releases/latest) e extraia.
2. Execute `EcoBot.exe` (Windows 10/11, 64 bits). Se o Windows mostrar "O Windows protegeu o computador", clique em **Mais informações → Executar assim mesmo** (o jogo não tem assinatura digital).

Ou jogue direto no navegador: **[leonhauck.github.io/Ecobot](https://leonhauck.github.io/Ecobot/)**.

### 🛠️ Abrir o projeto na Unity
1. Instale o **Unity Hub** e uma versão **Unity 6 (6000.x LTS)** com o módulo *Windows Build Support*.
2. No Unity Hub: **Add → Add project from disk** e selecione esta pasta. Se a versão for diferente, aceite abrir com a versão instalada.
3. Com o projeto aberto, use o menu **EcoBot → 1. Gerar projeto completo**. Ele cria as animações, prefabs, tiles e as 6 cenas (menu, 4 fases e vitória).
4. Abra `Assets/Scenes/MainMenu.unity` e aperte **Play**. Qualquer fase também pode ser testada direto: abra a cena `Level1..4` e aperte Play.

### 📦 Gerar o executável
- Menu **EcoBot → 2. Exportar executável (Windows)**. O jogo é gerado em `Builds/Windows/EcoBot.exe`.
- Menu **EcoBot → 3. Exportar para navegador (WebGL)**. A versão web é gerada em `Builds/WebGL` (é o que está publicado no GitHub Pages).
- Ou pelo caminho padrão: *File → Build Profiles* (as cenas já estão na lista).
- Pela linha de comando (sem abrir o editor):
  ```
  Unity.exe -batchmode -quit -projectPath . -executeMethod EcoBot.EditorTools.EcoBotBuilder.BatchBuildAll
  ```

---

## 📁 Estrutura do projeto

```
Assets/
├── Art/Sprites/        Sprites pixel-art (personagem, inimigos, itens, tiles, fundos, UI, partículas)
├── Audio/SFX/          Efeitos sonoros (.wav)
├── Audio/Music/        Trilhas de cada fase (.wav)
├── LevelData/          Mapas das fases em texto (Level1..4.txt)
├── Scripts/
│   ├── Core/           GameManager, AudioManager, GameInput, Effects, SceneFader, LevelInfo, Bootstrap
│   ├── Player/         PlayerController (movimento), PlayerHealth (energia/vidas)
│   ├── Enemies/        Enemy (dano/pisão), PatrolMover, FlyingMover
│   ├── Level/          Collectible, Checkpoint, Hazard, MovingPlatform, FallingPlatform, Climbable, LevelGoal...
│   ├── Camera/         CameraFollow (suavização, limites, tremor), ParallaxLayer
│   └── UI/             HUD, MainMenu, VictoryScreen, FloatingText, UIButtonSfx
├── Editor/             Construtor do projeto (menu "EcoBot") e configurações de importação
│
│   (criados pelo menu EcoBot → 1)
├── Animations/         AnimationClips e Animator Controllers
├── Prefabs/            Jogador, inimigos, itens, objetos de fase, partículas
├── Tiles/              Tiles do Tilemap
├── Resources/          Systems.prefab (gerenciadores carregados automaticamente)
└── Scenes/             MainMenu, Level1..4, Victory
Tools/                  Scripts Python que geram os sprites, os sons e as imagens dos mapas
Docs/                   Mapas das fases, rascunho do relatório e roteiro do vídeo
```

---

## 🧠 Arquitetura do código

- **Game loop da Unity:** a leitura de controles e as animações ficam no `Update`; a física (velocidade, gravidade, detecção de chão) fica no `FixedUpdate`; a câmera e o parallax ficam no `LateUpdate`, depois que tudo já se moveu.
- **Gerenciadores persistentes:** `Bootstrap` cria o prefab `Systems` antes da primeira cena (`RuntimeInitializeOnLoadMethod`). Ele contém `GameManager`, `AudioManager`, `Effects` e `SceneFader`, que sobrevivem à troca de cenas (`DontDestroyOnLoad`).
- **Eventos (C# `event`):** o `GameManager` avisa quando os números mudam, quando a fase começa ou termina, no game over e na pausa. O `HUD` apenas escuta esses eventos, sem lógica de jogo dentro da interface.
- **Componentização:** o inimigo é a soma de `Enemy` (dano e pisão) com um "motor" (`PatrolMover` ou `FlyingMover`). Para criar um inimigo novo basta trocar o motor.
- **Animator (máquina de estados)** do Eco: `Idle ⇄ Walk ⇄ Run`, `Jump`, `Fall`, `Climb`, `Hurt` e `Win`, controlados pelos parâmetros `Speed`, `Grounded`, `VelY`, `Climbing`, `ClimbMove` e pelos gatilhos `Hurt` e `Win`.
- **Input unificado** (`GameInput`): funciona com o Input Manager clássico e com o novo Input System, no teclado e no gamepad.

---

## 📝 Editor de fases em texto (ASCII)

As fases são desenhadas em arquivos de texto ([Assets/LevelData](Assets/LevelData)). O construtor lê cada caractere e monta a cena: tiles no Tilemap, colisores unificados (`CompositeCollider2D`), prefabs e decoração automática.

| Caractere | Elemento | Caractere | Elemento |
|---|---|---|---|
| `#` | Terreno sólido | `P` | Início do jogador |
| `=` | Plataforma de mão única | `G` | Muda Ancestral (fim) |
| `H` | Cipó / escada | `C` | Estação de recarga (checkpoint) |
| `~` | Lodo tóxico | `o` | Semente |
| `^` | Espinhos | `b` | 🔋 Bateria |
| `s` | Gosma de Lixo | `L` | 🍃 Folha dourada |
| `h` | Ouriço de Sucata | `F` | Plataforma rachada |
| `d` | Drone (vai até o `:` na linha) | `M` | Plataforma móvel horizontal (até o `:`) |
| `1`–`9` | Placa de dica | `V` | Plataforma móvel vertical (até o `:` acima) |

Para mudar uma fase: edite o `.txt` e rode **EcoBot → 1** de novo. Para ver o mapa como imagem: `python Tools/render_level_preview.py`.

---

## 🎨 Arte, áudio e créditos

- **Sprites (pixel art 16×16):** originais, desenhados por código em [Tools/generate_sprites.py](Tools/generate_sprites.py) (personagem, 3 inimigos, itens, 4 conjuntos de tiles, decoração, fundos com parallax, UI e partículas).
- **Áudio:** original, sintetizado em [Tools/generate_audio.py](Tools/generate_audio.py) (15 efeitos e 6 músicas no estilo chiptune).
- **Fonte:** LegacyRuntime (fonte padrão da Unity).
- **Engine:** Unity 6, C#.

Para regenerar a arte e o som: `python Tools/generate_sprites.py` e `python Tools/generate_audio.py` (Python 3, sem bibliotecas extras).

---

## 📸 Prints

| | |
|---|---|
| ![Menu](Docs/Prints/01_menu.png) | ![História](Docs/Prints/02_historia.png) |
| Menu principal | Tela de história |
| ![Fase 1](Docs/Prints/04_fase1_inicio.png) | ![Fase 1 - cipó](Docs/Prints/05_fase1_cipo.png) |
| Fase 1: Floresta Despertar | Checkpoint, cipó e Gosma de Lixo |
| ![Fase 2](Docs/Prints/06_fase2_caverna.png) | ![Fase 3](Docs/Prints/07_fase3_fabrica.png) |
| Fase 2: Caverna das Raízes | Fase 3: Fábrica Cinzenta |
| ![Fase 4](Docs/Prints/08_fase4_chamine.png) | ![Pausa](Docs/Prints/09_pausa.png) |
| Fase 4: Chaminé Final | Menu de pausa |
| ![Fase concluída](Docs/Prints/10_fase_concluida.png) | ![Vitória](Docs/Prints/11_vitoria.png) |
| Painel de fase concluída | Tela final |

> Os prints são gerados automaticamente: `EcoBot.exe -tour <pasta>` percorre o jogo e salva as imagens. Durante o jogo, **F12** salva um print na pasta `Prints` ao lado do executável.

---

## 📚 Documentação

- 📄 [Relatório teórico (PDF)](Docs/Relatorio_Teorico_EcoBot.pdf)
- 🗺️ [Mapas das fases](Docs/Mapas)
