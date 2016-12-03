using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using uPLibrary.Networking.M2Mqtt;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.SpeechRecognition;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

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
                return StartDate.ToString("HH:mm");
            }
        }
    }

    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class CalendarViewPage : Page
    {

        DispatcherTimer backTimer;
        HttpClient httpClient = new HttpClient();

        public CalendarViewPage()
        {
            this.InitializeComponent();
            backTimer = new DispatcherTimer();
            backTimer.Tick += BackTimer_Tick; ;
            backTimer.Interval = TimeSpan.FromMinutes(5);
            backTimer.Start();
            GetFeed();

        }

        private void BackTimer_Tick(object sender, object e)
        {
            backTimer.Stop();
            this.Frame.GoBack();
        }

        private async void GetFeed()
        {
            var calUrl = "https://calendar.google.com/calendar/ical/hbbe92b9tvlr9rosslm092chn4%40group.calendar.google.com/public/basic.ics";
            var calStream = await httpClient.GetStreamAsync(calUrl);
            var ret = new List<CalendarEvent>();
            var currentEvent = new CalendarEvent();
            CultureInfo us = new CultureInfo("en-US");
            using (var sr = new StreamReader(calStream))
            {
                while (sr.Peek() >= 0)
                {
                    var line = sr.ReadLine();
                    var parts = line.Split(':');
                    var key = parts[0];
                    var value = parts[1];
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
            listView.ItemsSource = ret.Where(d => d.EndDate > DateTime.Now);

        }

    }
}
