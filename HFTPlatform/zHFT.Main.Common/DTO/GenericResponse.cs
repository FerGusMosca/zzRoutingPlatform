using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace zHFT.Main.Common.DTO
{
    public class GenericResponse
    {
        [JsonIgnore]
        public HttpResponseMessage resp { get; set; }

        public string respContent { get; set; }

        public bool success { get; set; }

        public GenericError error { get; set; }
    }
}
