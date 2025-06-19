using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Pathlink.Core
{
    public class NOAAMagneticDeclinationResponse
    {
        [JsonProperty("result")]
        public List<DeclinationResult> Results { get; set; }

        [JsonProperty("model")]
        public string Model { get; set; }

        [JsonProperty("units")]
        public Units Unit { get; set; }

        [JsonProperty("version")]
        public string Version { get; set; }
    }

    public class DeclinationResult
    {
        [JsonProperty("date")]
        public double Date { get; set; }

        [JsonProperty("elevation")]
        public int Elevation { get; set; }

        [JsonProperty("declination")]
        public double Declination { get; set; }

        [JsonProperty("latitude")]
        public double Latitude { get; set; }

        [JsonProperty("declination_sv")]
        public double DeclinationSv { get; set; }

        [JsonProperty("declination_uncertainty")]
        public double DeclinationUncertainty { get; set; }

        [JsonProperty("longitude")]
        public double Longitude { get; set; }
    }

    public class Units
    {
        [JsonProperty("elevation")]
        public string Elevation { get; set; }

        [JsonProperty("declination")]
        public string Declination { get; set; }

        [JsonProperty("declination_sv")]
        public string DeclinationSv { get; set; }

        [JsonProperty("latitude")]
        public string Latitude { get; set; }

        [JsonProperty("declination_uncertainty")]
        public string DeclinationUncertainty { get; set; }

        [JsonProperty("longitude")]
        public string Longitude { get; set; }
    }


}
