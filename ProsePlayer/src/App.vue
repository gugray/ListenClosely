<template>
  <div id="app">
    <vue-headful :title="pageTitle" />
    <template v-if="mode == null">
      <div class="noApp">
      </div>
    </template>
    <template v-if="mode == 'error'">
      <div class="noApp">
        Something went wrong ;-[
      </div>
    </template>
    <template v-if="mode == 'listen'">
      <div class="topFixed">
        <ControlPanel :loading="audioLoading" :playing="playing" v-on:action="onControlPanel"
                      :currPos="currPos" :totalSec="totalSec" :title="title" />
      </div>
      <div class="centrer">
        <ParaList :paras="paras" :dictEntries="content.dictEntries" :episode="episode"
                  :activeParaIx="active.paraIx" :activeSegIx="active.segIx"
                  @jump="onJump" v-on:showEntries="onShowEntries" />
        <div class="anno-pad">&nbsp;</div>
      </div>
      <div class="fixed-centrer-outer">
        <div class="fixed-centrer">
          <div class="annotation">
            <InfoPanel :entryIxs="entryIxsToShow" :content="content" :pinnedEntries="pinnedEntries" :mode="mode" :episode="episode"
                       v-on:pin="onEntryPin" v-on:unpin="onEntryUnpin" />
          </div>
        </div>
      </div>
    </template>
  </div>
</template>

<script>
  import ControlPanel from "./components/ControlPanel.vue"
  import ParaList from "./components/ParaList.vue"
  import InfoPanel from "./components/InfoPanel.vue"
  var player = require("./player.js").default;
  var axios = require('axios');
  const dataKey = "ppdata";

  export default {
    name: 'app',
    data: function () {
      return {
        episode: null,
        audioLoading: false,
        mode: null,
        content: null,
        paras: null,
        startSecForEntries: -1,
        entryIxsToShow: [],
        pinnedEntries: [],
        playing: false,
        currPos: 0,
        totalSec: 0,
        blockTicksTill: -1,
        active: {
          paraIx: 0,
          segIx: 0,
        },
      }
    },
    components: {
      ControlPanel,
      ParaList,
      InfoPanel,
    },
    computed: {
      title: function () {
        if (this.content && this.content.title) return this.content.title;
        return "";
      },
      pageTitle: function () {
        var title = "";
        if (this.content && this.content.title) title = this.content.title + " | ";
        title += "Prose Player";
        return title;
      },
    },
    methods: {
      setActive: function (paraIx, segIx) {
        this.active = { paraIx: paraIx, segIx: segIx };
        this.entryIxsToShow = [];
        this.pinnedEntries = getPinnedEntries(this.episode, this.paras[paraIx][segIx].startSec);
      },
      onTick: function () {
        var currPos = player.getPos();
        this.playing = player.isPlaying();
        if (this.currPos != Math.floor(currPos)) this.currPos = Math.floor(currPos);
        // Ticks blocked: we're done here
        var timeNow = new Date().getTime();
        if (timeNow < this.blockTicksTill) return;
        // Player is not even playing: we're done here
        if (!player.isPlaying()) return;
        // Find segment
        var currSeg = this.paras[this.active.paraIx][this.active.segIx];
        var nextSeg = null;
        if (this.active.segIx + 1 < this.paras[this.active.paraIx].length) nextSeg = this.paras[this.active.paraIx][this.active.segIx + 1];
        else if (this.active.paraIx + 1 < this.paras.length) nextSeg = this.paras[this.active.paraIx + 1][0];
        var inCurrSeg = true;
        if (currPos < currSeg.startSec) inCurrSeg = false;
        if (nextSeg != null && currPos >= nextSeg.startSec) inCurrSeg = false;
        if (inCurrSeg) return;
        // Just entered next segment?
        if (nextSeg != null && currPos >= nextSeg.startSec && currPos <= nextSeg.startSec + nextSeg.lengthSec) {
          // OK, update active segment
          if (this.active.segIx + 1 < this.paras[this.active.paraIx].length)
            this.setActive(this.active.paraIx, this.active.segIx + 1);
          else this.setActive(this.active.paraIx + 1, 0);
          return;
        }
        // Find wherever we are
        var newParaIx = 0;
        var newSegIx = 0;
        for (var pix = 0; pix < this.paras.length; ++pix) {
          for (var six = 0; six < this.paras[pix].length; ++six) {
            var thisSegm = this.paras[pix][six];
            if (currPos >= thisSegm.startSec) { newParaIx = pix; newSegIx = six; }
          }
        }
        this.setActive(newParaIx, newSegIx);
      },
      onPlayPause: function () {
        // If now resuming play, block autopause for a bit
        // Otherwise, we cannot continue by resuming at end of segment
        if (!player.isPlaying()) {
          this.blockTicksTill = new Date().getTime() + 400;
          player.play();
        }
        else player.pause();
      },
      onJump: function (paraIx, segIx) {
        var startPos = this.paras[paraIx][segIx].startSec;
        this.setActive(paraIx, segIx);
        this.blockTicksTill = new Date().getTime() + 400;
        player.playFrom(startPos);
      },
      onRepeat: function () {
        this.onJump(this.active.paraIx, this.active.segIx);
        var pos = this.paras[this.active.paraIx][this.active.segIx].startSec;
        if (window._paq)
          window._paq.push(['trackEvent', 'Navigate-Repeat', this.episode, "", fmtPos(pos)]);
      },
      onNext: function () {
        var newParaIx = this.active.paraIx;
        var newSegIx = this.active.segIx;
        if (newSegIx + 1 < this.paras[this.active.paraIx].length)++newSegIx;
        else if (newParaIx + 1 < this.paras.length - 1) { ++newParaIx; newSegIx = 0; }
        else return;
        this.onJump(newParaIx, newSegIx);
        var pos = this.paras[newParaIx][newSegIx].startSec;
        if (window._paq)
          window._paq.push(['trackEvent', 'Navigate-Next', this.episode, "", fmtPos(pos)]);
      },
      onPrev: function () {
        var newParaIx = this.active.paraIx;
        var newSegIx = this.active.segIx;
        if (newSegIx > 0)--newSegIx;
        else if (newParaIx > 0) { --newParaIx; newSegIx = this.paras[newParaIx].length - 1; }
        else return;
        this.onJump(newParaIx, newSegIx);
        var pos = this.paras[newParaIx][newSegIx].startSec;
        if (window._paq)
          window._paq.push(['trackEvent', 'Navigate-Prev', this.episode, "", fmtPos(pos)]);
      },
      onControlPanel: function (action) {
        if (action == "back") this.onPrev();
        else if (action == "next") this.onNext();
        else if (action == "repeat") this.onRepeat();
        else if (action == "playPause") this.onPlayPause();
      },
      onShowEntries: function (segStartSec, ixs) {
        this.startSecForEntries = segStartSec;
        this.entryIxsToShow = ixs;
      },
      onKeyDown: function (e) {
        var cmd = null;
        if (!e.ctrlKey && !e.altKey && !e.metaKey && !e.shiftKey) {
          if (e.key == "Enter") cmd = "Space";
          if (this.mode != "edit") {
            if (e.key == "ArrowDown") cmd = "Down";
            else if (e.key == "ArrowUp") cmd = "Up";
            else if (e.key == "ArrowLeft") cmd = "Left";
            else if (e.key == " ") cmd = "Space";
          }
        }
        if (cmd == null) return;
        e.preventDefault();
        if (cmd == "Space") this.onPlayPause();
        else if (cmd == "Left") this.onRepeat();
        else if (cmd == "Down") this.onNext();
        else if (cmd == "Up") this.onPrev();
      },
      onEntryUnpin: function (entry) {
        var startSec = this.paras[this.active.paraIx][this.active.segIx].startSec;
        pinUnpinEntry(this.episode, this.startSecForEntries, entry, "unpin");
        this.pinnedEntries = getPinnedEntries(this.episode, startSec);
      },
      onEntryPin: function (entry) {
        var startSec = this.paras[this.active.paraIx][this.active.segIx].startSec;
        pinUnpinEntry(this.episode, this.startSecForEntries, entry, "pin");
        this.pinnedEntries = getPinnedEntries(this.episode, startSec);
      },
    },
    mounted: function () {
      var episode = null;
      var mode = "listen";
      var app = this;

      // Query param stuff (for via static)
      const urlParams = new URLSearchParams(window.location.search);
      episode = urlParams.get('ep');
      if (!episode) {
        app.mode = "error";
        return;
      }
      // Set up, get episode data
      app.episode = episode;
      if (mode == "listen" || mode == "segment" || mode == "edit") {
        // Original is for segments via API. For now, we're back to static
        //axios.get("/api/" + episode + "/segs")
        axios.get("./media/" + episode + "-segs.json")
          .then(function (response) {
            app.content = response.data;
            app.paras = calcParas(app.content.segments);
            app.mode = mode;
            app.audioLoading = true;
            player.onLoad(() => {
              app.audioLoading = false;
              setInterval(app.onTick, 10);
              app.totalSec = player.duration();
            });
            player.initAudio(episode);
            if (window._paq)
              window._paq.push(['trackEvent', 'Load-Episode', episode]);
          })
          .catch(function (error) {
            // eslint-disable-next-line
            console.log(error);
            app.mode = "error";
          });
        // Indent
      }
      // Keyboard stuff
      window.addEventListener("keydown", this.onKeyDown);
    }
  }

  function calcParas(segments) {
    var res = [];
    var paraIx = 0;
    var segsHere = [];
    for (var i = 0; i < segments.length; ++i) {
      var segm = segments[i];
      if (segm.paraIx != paraIx) {
        res.push(segsHere);
        segsHere = [];
        paraIx = segm.paraIx;
      }
      segsHere.push(segm);
    }
    if (segsHere.length != 0) res.push(segsHere);
    return res;
  }

  function fmtPos(pos) {
    return (Math.round(pos * 100) / 100).toFixed(2);
  }

  var skeletonData = {
    episodes: [{
      "name": "E179X",
      "segments": [
        {
          "startSec": 9.25,
          "pinnedEntries": []
        }
      ]
    }]
  };

  function getPinnedEntries(episode, startSec) {
    var res = [];
    var appData = getEnsureData();
    var episodeData = null;
    for (var i = 0; i < appData.episodes.length; ++i) {
      if (appData.episodes[i].name == episode) {
        episodeData = appData.episodes[i];
        break;
      }
    }
    var segment = null;
    if (episodeData != null) {
      for (var j = 0; j < episodeData.segments.length; ++j) {
        if (episodeData.segments[j].startSec == startSec) {
          segment = episodeData.segments[j];
          break;
        }
      }
    }
    if (!segment) return res;
    for (var k = 0; k < segment.pinnedEntries.length; ++k) {
      res.push(JSON.parse(segment.pinnedEntries[k]));
    }
    return res;
  }

  function getEnsureData() {
    var appDataJson = window.localStorage.getItem(dataKey);
    if (!appDataJson) window.localStorage.setItem(dataKey, JSON.stringify(skeletonData));
    appDataJson = window.localStorage.getItem(dataKey);
    return JSON.parse(appDataJson);
  }

  function saveData(appData) {
    window.localStorage.setItem(dataKey, JSON.stringify(appData));
  }

  function pinUnpinEntry(episode, startSec, entry, action) {
    var entryJson = JSON.stringify(entry);
    var appData = getEnsureData();
    var episodeData = null;
    for (var i = 0; i < appData.episodes.length; ++i) {
      if (appData.episodes[i].name == episode) {
        episodeData = appData.episodes[i];
        break;
      }
    }
    if (episodeData == null) {
      episodeData = {
        "name": episode,
        "segments": []
      };
      appData.episodes.push(episodeData);
    }
    var segment = null;
    for (var j = 0; j < episodeData.segments.length; ++j) {
      if (episodeData.segments[j].startSec == startSec) {
        segment = episodeData.segments[j];
        break;
      }
    }
    if (segment == null) {
      segment = {
        "startSec": startSec,
        "pinnedEntries": []
      };
      episodeData.segments.push(segment);
    }
    if (action == "pin") segment.pinnedEntries.push(entryJson);
    else if (action == "unpin") {
      var ix = -1;
      for (var k = 0; k < segment.pinnedEntries.length; ++k) {
        if (segment.pinnedEntries[k] == entryJson) {
          ix = k;
          break;
        }
      }
      if (ix != -1) segment.pinnedEntries.splice(ix, 1);
    }
    saveData(appData);
  }

</script>

<style lang="less">
  @import './style.less';
</style>

<style>
  @import 'https://fonts.googleapis.com/css?family=Source+Sans+Pro:400,400i,700,700i';
  @import 'https://fonts.googleapis.com/css?family=Space+Mono:700&display=swap';
</style>
