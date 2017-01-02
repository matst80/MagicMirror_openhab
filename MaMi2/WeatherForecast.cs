using System.Collections.Generic;

namespace MaMi2
{
    public class MainForecast {
        public string icon { get; set; }
    }


    public class WeatherForecast
    {
        public List<MainForecast> weather { get; set; }
    }
}