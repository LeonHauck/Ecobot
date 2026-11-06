// Só entra na versão para navegador (WebGL).
// Navegadores bloqueiam o som até a primeira interação do jogador. A Unity libera o som
// ao apertar uma tecla, mas não em todos os cliques: aqui qualquer clique, toque ou
// tecla também libera, para a música do menu tocar logo no primeiro clique.
mergeInto(LibraryManager.library, {
  EcoBot_InstallAudioUnlock__deps: ['$WEBAudio'],
  EcoBot_InstallAudioUnlock: function () {
    var resume = function () {
      try {
        var ctx = WEBAudio && WEBAudio.audioContext;
        if (ctx && ctx.state !== 'running') ctx.resume();
      } catch (e) { }
    };
    ['pointerdown', 'mousedown', 'touchstart', 'keydown', 'click'].forEach(function (name) {
      document.addEventListener(name, resume, true);
    });
  }
});
