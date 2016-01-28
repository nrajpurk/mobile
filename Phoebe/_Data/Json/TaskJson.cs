using System;
using Newtonsoft.Json;

namespace Toggl.Phoebe._Data.Json
{
    public class TaskJson : CommonJson
    {
        [JsonProperty ("name")]
        public string Name { get; set; }

        [JsonProperty ("active")]
        public bool IsActive { get; set; }

        [JsonProperty ("estimated_seconds")]
        public long Estimate { get; set; }

        [JsonProperty ("wid")]
        public long WorkspaceRemoteId { get; set; }

        [JsonProperty ("pid")]
        public long ProjectRemoteId { get; set; }
    }
}
