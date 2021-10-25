<template>
  <div class="infoPanel">
    <div v-if="showInstructions" class="ifu">
      <h2>Short help</h2>
      <p>
        Click left of the text to play from a given sentence. Click on a word in the text to show dictionary entries.
      </p>
    </div>
    <div v-if="showNoEntries" class="ifu">
      <h2>No entries</h2>
      <p>
        Sorry; we didn't find any dictionary matches for this word.
      </p>
    </div>
    <div class="pinnedEntries" v-if="pinnedEntries && pinnedEntries.length > 0">
      <Entry v-for="(entry, ix) in pinnedEntries" :key="`entry-${ix}`" :entry="entry" :pinned="true"
             v-on:pinClicked="onUnpin" />
    </div>
    <div>
      <Entry v-for="(entryIx, ix) in entryIxs" :key="`entry-${ix}`" :entry="getEntry(entryIx)" :pinned="false"
             v-on:pinClicked="onPin" />
    </div>
  </div>
</template>

<script>
  import Entry from "./Entry.vue"

  export default {
    name: 'InfoPanel',
    data: function () {
      return {
      }
    },
    props: {
      content: Object,
      entryIxs: Array,
      pinnedEntries: Array,
      mode: String,
      episode: String,
    },
    computed: {
      showInstructions: function () {
        if (this.pinnedEntries && this.pinnedEntries.length != 0) return false;
        if (this.entryIxs && this.entryIxs.length != 0) return false;
        if (this.entryIxs !== null) return true;
        return false;
      },
      showNoEntries: function () {
        return this.entryIxs == null && (!this.pinnedEntries || this.pinnedEntries.length == 0);
      },
    },
    methods: {
      getEntry: function (entryIx) {
        return this.content.dictEntries[entryIx];
      },
      onUnpin: function (entry) {
        this.$emit("unpin", entry);
      },
      onPin: function (entry) {
        this.$emit("pin", entry);
        if (window._paq)
          window._paq.push(['trackEvent', 'Pin-Entry', this.episode, entry.head]);
      },
    },
    components: {
      Entry,
    },
  }
</script>

