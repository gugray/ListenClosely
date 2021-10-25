<template>
  <div class="controlPanel">
    <div :class="loadingClass">Loading...</div>
    <i class="fa smaller fa-step-backward" @click="onClick('back')"></i>
    <i class="fa" :class="playPauseClass"  @click="onClick('playPause')"></i>
    <i class="fa smaller fa-undo"  @click="onClick('repeat')"></i>
    <i class="fa smaller fa-step-forward"  @click="onClick('next')"></i>
    <div class="time">{{ timeStr }}</div>
    <div class="title">{{ title }}</div>
  </div>
</template>

<script>
  var fmtTime = require("../fmtTime.js").default;

  export default {
    name: 'ControlPanel',
    data: function () {
      return {
      }
    },
    props: {
      title: String,
      loading: Boolean,
      playing: Boolean,
      currPos: Number,
      totalSec: Number,
    },
    computed: {
      loadingClass: function () {
        if (this.loading) return "loading visible";
        else return "loading";
      },
      playPauseClass: function () {
        if (!this.playing) return "fa-play-circle-o";
        else return "fa-pause-circle-o active";
      },
      timeStr: function () {
        return fmtTime(this.currPos, false, 0) + " / " + fmtTime(this.totalSec, false, 0);
      },
    },
    methods: {
      onClick: function (action) {
        this.$emit('action', action);
      },
    },
    components: {
    }
  }
</script>

