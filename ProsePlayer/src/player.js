import { Howl } from 'howler';

var player = function () {

  var _onLoad = null;
  var _sound = null;
  var _playing = false;

  function initAudio(ep) {
    var audioUrlBase = "./media/" + ep;
    var audioUrls = [
      audioUrlBase + ".webm",
      audioUrlBase + ".m4a",
    ];
    _sound = new Howl({
      src: audioUrls,
      volume: 1.0,
      html5: true,
      autoplay: false,
      onload: function () {
        if (_onLoad) _onLoad();
      },
      onplay: function () {
        _playing = true;
      },
      onend: function () {
        _playing = false;
      },
      onpause: function () {
        _playing = false;
      },
      onstop: function () {
        _playing = false;
      }
    });
  }

  function pause() {
    if (_playing) _sound.pause();
  }

  function play() {
    if (!_playing) _sound.play();
  }

  function playFrom(pos) {
    _sound.seek(pos);
    if (!_playing) _sound.play();
  }

  function getPos() {
    var currSec = undefined;
    try {
      currSec = _sound.seek();
    }
    catch {
      // NOP
    }
    if (typeof currSec == 'number') return currSec;
    else return 0;
  }


  return {
    onLoad: function (handler) { _onLoad = handler; },
    initAudio: initAudio,
    pause: pause,
    play: play,
    playFrom: playFrom,
    getPos: getPos,
    isPlaying: function () { return _playing; },
    duration: function () { return _sound.duration() || 0; },
  };

}();

export default player;
