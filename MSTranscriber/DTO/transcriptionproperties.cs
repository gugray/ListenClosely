//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

namespace MSTranscriber
{
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;

    public class TranscriptionProperties
    {
        [JsonProperty(PropertyName = "diarizationEnabled")]
        public bool IsDiarizationEnabled { get; set; }

        [JsonProperty(PropertyName = "wordLevelTimestampsEnabled")]
        public bool IsWordLevelTimestampsEnabled { get; set; }

        public string Email { get; set; }

        public IEnumerable<int> Channels { get; set; }

        public PunctuationMode PunctuationMode { get; set; }

        public ProfanityFilterMode ProfanityFilterMode { get; set; }

        [JsonConverter(typeof(TimeSpanConverter))]
        public TimeSpan Duration { get; set; }

        public Uri DestinationContainerUrl { get; set; }

        [JsonConverter(typeof(TimeSpanConverter))]
        public TimeSpan TimeToLive { get; set; }

        public EntityError Error { get; set; }
    }
}