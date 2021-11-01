<template>
  <div class="entry" :class="getClass">
    <div class="head">
      <div class="headword">  {{ displayHead }}</div>
      <div class="pin" v-on:click="onPinClicked"><ImgPushPin /></div>
    </div>
    <ul class="senses">
      <template v-for="(sense, ix) in entry.senses">
        <li class="sense" :key="`sense-${ix}`">
          {{ sense.srcDef }}
        </li>
      </template>
    </ul>
  </div>
</template>

<script>
  import ImgPushPin from "../image_comps/ImgPushPin.vue"

  export default {
    name: 'Entry',
    data: function () {
      return {
      }
    },
    props: {
      entry: { type: Object },
      pinned: Boolean,
    },
    computed: {
      displayHead: function () {
        if (this.entry.displayHead && this.entry.displayHead != "") return this.entry.displayHead;
        return this.entry.head;
      },
      getClass: function () {
        return this.pinned ? "pinned" : "";
      },
      hanziAlt: function () {
        if (this.entry.hanzi_trad == this.entry.hanzi) return "";
        else return " • " + this.entry.hanzi_trad;
      }
    },
    methods: {
      onPinClicked: function () {
        this.$emit('pinClicked', this.entry);
      },
    },
    components: {
      ImgPushPin,
    },
  }
</script>

