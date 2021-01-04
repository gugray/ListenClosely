<template>
  <div :class="`para ${dynClass}`" ref="paraDiv">
    <div class="paraLeft">
      <span class="paraMarker">&nbsp;</span>
      <span class="segmMarker">&nbsp;</span>
    </div>
    <div v-for="(segm, ix) in paraSegs.slice().reverse()" :key="`segmStripe-${ix}`" :class="`segmStripe segmStripe${ix}`"></div>
    <p>
      <Segment v-for="(segm, ix) in paraSegs" :key="`segm-${ix}`" :segm="segm" :dictEntries="dictEntries"
               :episode="episode" :paraIx="paraIx" :segmIx="ix" :active="activeSegIx == ix"
               v-on:showEntries="onShowEntries" />
    </p>
    <div v-for="(segm, ix) in paraSegs.slice().reverse()" :key="`segmHandle-${ix}`" :class="`segmHandle segmHandle${ix}`"
         @mouseover="onOverHandle(ix)" @mouseleave="onLeaveHandle(ix)" @click="onClickHandle(ix)"></div>
  </div>
</template>

<script>
  import Segment from "./Segment.vue"

  export default {
    name: 'Para',
    data: function () {
      return {
      }
    },
    props: {
      episode: String,
      paraIx: Number,
      paraSegs: Array,
      dictEntries: Array,
      active: Boolean,
      activeSegIx: Number,
    },
    computed: {
      dynClass: function () {
        var cls = "";
        if (this.active) cls += " active";
        return cls;
      },
      space: function () {
        return " ";
      },
    },
    mounted: function () {
      var elmPara = this.$refs.paraDiv;
      var myRect = elmPara.getBoundingClientRect();
      for (var ix = 0; ix < this.paraSegs.length; ++ix) {
        var revIx = this.paraSegs.length - ix - 1;
        // Get height and relative top of segment
        var elmSegment = elmPara.querySelector(".segment.ix" + ix);
        var segmRect = elmSegment.getBoundingClientRect();
        var relTop = segmRect.top - myRect.top;
        // Position corresponding stripe and handle
        var elmStripe = elmPara.querySelector(".segmStripe" + revIx);
        elmStripe.style.top = relTop + "px";
        elmStripe.style.height = segmRect.height + "px";
        var elmHandle = elmPara.querySelector(".segmHandle" + revIx);
        elmHandle.style.top = relTop + "px";
        elmHandle.style.height = segmRect.height + "px";
      }
    },
    watch: {
      active: function (newVal) {
        // When we become active
        if (newVal == false) return;
        // Scroll position, and our screen offset / bottom
        var scrollPos = document.documentElement.scrollTop;
        var elm = this.$refs.paraDiv;
        var ofs = document.getElementsByClassName("topFixed")[0].offsetHeight;
        // We want this many extra pixels visible
        var extra = 10;
        // Bottom not fully visible
        var diffBottom = elm.offsetTop + elm.offsetHeight + ofs - scrollPos - document.documentElement.offsetHeight + extra;
        if (diffBottom > 0) {
          var opt = {
            top: document.documentElement.scrollTop + diffBottom,
            behavior: "smooth",
          };
          document.documentElement.scroll(opt);
        }
        // Top not fully visible
        var diffTop = elm.offsetTop - scrollPos - extra;
        if (diffTop < 0) {
          var opt = {
            top: document.documentElement.scrollTop + diffTop,
            behavior: "smooth",
          }
          document.documentElement.scroll(opt);
        }
      },
    },
    methods: {
      onOverHandle: function (ix) {
        var elmPara = this.$refs.paraDiv;
        //elmPara.querySelector(".segmStripe" + ix).classList.add("visible");
        elmPara.querySelector(".segmHandle" + ix).classList.add("indicated");
        var segIx = this.paraSegs.length - ix - 1;
        elmPara.querySelector(".segment.ix" + segIx).classList.add("indicated");
      },
      onLeaveHandle: function (ix) {
        var elmPara = this.$refs.paraDiv;
        elmPara.querySelector(".segmStripe" + ix).classList.remove("visible");
        elmPara.querySelector(".segmHandle" + ix).classList.remove("indicated");
        var segIx = this.paraSegs.length - ix - 1;
        elmPara.querySelector(".segment.ix" + segIx).classList.remove("indicated");
      },
      onClickHandle: function (ix) {
        this.$emit('jump', this.paraIx, this.paraSegs.length - ix - 1);
      },
      onShowEntries: function (segStartSec, ixs) {
        this.$emit("showEntries", segStartSec, ixs);
      },
    },
    components: {
      Segment,
    },
  }
</script>

