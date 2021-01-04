<template>
  <div class="paraList">
    <Para v-for="(para, ix) in paras" :key="`para-${ix}`" :paraSegs="para" :dictEntries="dictEntries"
          :paraIx="ix" :episode="episode" :active="activeParaIx == ix" :activeSegIx="activeSegInPara(ix)"
          @jump="onJump" v-on:showEntries="onShowEntries" />
  </div>
</template>

<script>
  import Para from "./Para.vue"

  export default {
    name: 'ParaList',
    data: function () {
      return {
      }
    },
    props: {
      episode: String,
      paras: Array,
      dictEntries: Array,
      activeParaIx: Number,
      activeSegIx: Number,
    },
    computed: {
    },
    methods: {
      activeSegInPara: function (paraIx) {
        if (paraIx != this.activeParaIx) return -1;
        return this.activeSegIx;
      },
      onJump: function (paraIx, segIx) {
        this.$emit('jump', paraIx, segIx);
      },
      onShowEntries: function (segStartSec, ixs) {
        this.$emit("showEntries", segStartSec, ixs);
      },
    },
    components: {
      Para,
    },
  }
</script>

