﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discord_UWP.API.Connections
{
    public class Connection
    {
        [JsonProperty("url")]
        public string Url { get; set; }
    }
}
