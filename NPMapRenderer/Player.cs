using System.Collections.Generic;
using Newtonsoft.Json;

namespace NPMapRenderer
{
    public class Player
    {
        [JsonProperty("uid")] 
        public int Id { get; set; }
        public string Alias { get; set; }
        public Dictionary<string, Tech> Tech { get; set; } = new Dictionary<string, Tech>(); 
    }

    public class Tech
    {
        public double Value { get; set; }
        public int Level { get; set; }
    }
}