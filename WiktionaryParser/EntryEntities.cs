using Newtonsoft.Json;
using System.Collections.Generic;

namespace WiktionaryParser
{
    class Meaning
    {
        [JsonProperty("srcDef")]
        public string SrcDef;
        [JsonProperty("otherLangs")]
        public List<string> OtherLangs = new List<string>();
    }

    class Entry
    {
        [JsonProperty("head")]
        public string Head;
        [JsonProperty("pos")]
        public string PoS;
        [JsonProperty("meanings")]
        public List<Meaning> Meanings = new List<Meaning>();
    }
}
