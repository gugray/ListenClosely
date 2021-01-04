<template>
  <span :class="`segment ${dynClass} ix${segmIx}`" ref="segmentDiv" >
    <template v-for="(word, ix) in segm.words">
      <span class="word" :key="`wd-${ix}`" v-on:click="onWordClick(ix)">
        <span v-if="spaceBefore(ix)">{{ " " }}</span><span>{{word.lead}}</span><span>{{word.text}}</span><span>{{word.trail}}</span>
      </span>
    </template>
  </span>
</template>

<script>

  export default {
    name: 'Segment',
    data: function () {
      return {
      }
    },
    components: {
    },
    props: {
      episode: String,
      paraIx: Number,
      segmIx: Number,
      segm: Object,
      dictEntries: Array,
      active: Boolean,
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
    watch: {
      active: function (newVal) {
        //// When we become active
        //if (newVal != false) {
        //  // Scroll active segment into view
        //  var elm = this.$refs.segmentDiv;
        //  var scrollElm = elm.parentNode.parentNode.parentNode;
        //  var offsetTop = elm.offsetTop;
        //  var scrollTop = scrollElm.scrollTop;
        //  var opt = {
        //    top: offsetTop - 300,
        //  };
        //  if (Math.abs(offsetTop - 300 - scrollTop) < 600) opt.behavior = "smooth";
        //  scrollElm.scroll(opt);
        //}
      },
    },
    mounted: function () {
    },
    methods: {
      spaceBefore: function (ix) {
        if (ix == 0) return this.segmIx != 0;
        if (this.segm.words[ix].glueLeft) return false;
        return true;
      },
      onWordClick: function (ix) {
        var ixs = this.segm.words[ix].entries;
        if (ixs && ixs.length > 0) this.$emit("showEntries", this.segm.startSec, ixs);
        else this.$emit("showEntries", this.segm.startSec, null);
        if (window._paq)
          window._paq.push(['trackEvent', 'Show-Entries', this.episode, this.segm.words[ix].text]);
      },
    },
  }
</script>

