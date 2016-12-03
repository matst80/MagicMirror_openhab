using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace MaMi2
{

    public class CalendarEvent
    {
        public string Title { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string StartDateText
        {
            get
            {
                return StartDate.ToString("dd MMM HH:mm");
            }
        }

        public string EndDateText
        {
            get
            {
                return EndDate.ToString("HH:mm");
            }
        }
    }

    public class ICalParser
    {
        public static async Task<List<CalendarEvent>> GetCalenderEvents(string calUrl) {
            
            var httpClient = new HttpClient();
            var calStream = await httpClient.GetStreamAsync(calUrl);
            var ret = new List<CalendarEvent>();
            var currentEvent = new CalendarEvent();
            CultureInfo us = new CultureInfo("en-US");
            var hasStarted = false;
            using (var sr = new StreamReader(calStream))
            {
                while (sr.Peek() >= 0)
                {
                    var line = sr.ReadLine();
                    if (line == "BEGIN:VEVENT")
                        hasStarted = true;
                    var parts = line.Split(':');
                    var key = parts[0];
                    var value = parts[1];
                    if (hasStarted)
                    {
                        if (key == "DTSTART")
                        {
                            currentEvent.StartDate = DateTime.ParseExact(value, "yyyyMMddTHHmmssZ", us);
                        }
                        else if (key == "DTEND")
                        {
                            currentEvent.EndDate = DateTime.ParseExact(value, "yyyyMMddTHHmmssZ", us);
                        }
                        else if (key == "SUMMARY")
                        {
                            currentEvent.Title = value;
                        }
                        else if (line == "END:VEVENT")
                        {
                            ret.Add(currentEvent);
                            currentEvent = new CalendarEvent();
                        }
                    }
                }
            }
            return ret;
        }
    }
}
