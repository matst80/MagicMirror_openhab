using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        DispatcherTimer datetimeUpdateTimer;
        DispatcherTimer dayUpdateTimer;

        HttpClient httpClient = new HttpClient();

        private MqttClient mqttClient;
        private SpeechRecognizer recognizer;

        public MainPage()
        {
            this.InitializeComponent();
            UpdateTime();
            UpdateTemp();
            UpdateMovements();
            datetimeUpdateTimer = new DispatcherTimer();
            datetimeUpdateTimer.Tick += DatetimeUpdateTimer_Tick;
            datetimeUpdateTimer.Interval = TimeSpan.FromMinutes(1);
            datetimeUpdateTimer.Start();

            dayUpdateTimer = new DispatcherTimer();
            dayUpdateTimer.Tick += DayUpdateTimer_Tick;
            dayUpdateTimer.Interval = TimeSpan.FromHours(1);
            dayUpdateTimer.Start();

            mqttClient = new MqttClient("10.10.10.1");
            mqttClient.Connect("mami");
            mqttClient.MqttMsgPublishReceived += MqttClient_MqttMsgPublishReceived;
            //, "/smarthome/mirrormain"
            mqttClient.Subscribe(new[] { "/smarthome/mirror" }, new byte[] { uPLibrary.Networking.M2Mqtt.Messages.MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE });
            mqttClient.Subscribe(new[] { "/smarthome/mirrormain" }, new byte[] { uPLibrary.Networking.M2Mqtt.Messages.MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE });
            mqttClient.Subscribe(new[] { "/smarthome/news" }, new byte[] { uPLibrary.Networking.M2Mqtt.Messages.MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE });
            UpdateSun();


            GetFeed();
            initializeSpeechRecognizer();
        }

        private async void GetFeed()
        {
            var client = new Windows.Web.Syndication.SyndicationClient();
            var feed = await client.RetrieveFeedAsync(new Uri("http://www.aftonbladet.se/nyheter/rss.xml"));
            var lastNews = feed.Items.Take(2);

            tbLastNews.Text = lastNews.FirstOrDefault().Title.Text;
            //tbOldNews.Text = lastNews.LastOrDefault().Title.Text;
            GetCalendarFeed();
        }

        private async void GetCalendarFeed()
        {
            var ret = await ICalParser.GetCalenderEvents("https://calendar.google.com/calendar/ical/hbbe92b9tvlr9rosslm092chn4%40group.calendar.google.com/public/basic.ics");
            var first = ret.FirstOrDefault(d => d.StartDate>=DateTime.Now && d.StartDate.Date == DateTime.Today);
            if (first != null) {
                tbSecMessage.Text = first.StartDateText;
                tbMainMessage.Text = first.Title;
            }

        }

        private async void UpdateMovements()
        {
            var bioState = await httpClient.GetStringAsync("http://10.10.10.1:8080/rest/items/IN_BIO/state");
            var gfState = await httpClient.GetStringAsync("http://10.10.10.1:8080/rest/items/IN_GF/state");
            var tfState = await httpClient.GetStringAsync("http://10.10.10.1:8080/rest/items/IN_TF/state");
            tf.Fill.Opacity = tfState == "ON" ? 1 : 0.4;
            gf.Fill.Opacity = gfState == "ON" ? 1 : 0.4;
            bf.Fill.Opacity = bioState == "ON" ? 1 : 0.4;
        }

        private void DayUpdateTimer_Tick(object sender, object e)
        {
            UpdateSun();
        }

        private void MqttClient_MqttMsgPublishReceived(object sender, uPLibrary.Networking.M2Mqtt.Messages.MqttMsgPublishEventArgs e)
        {
            var message = Encoding.UTF8.GetString(e.Message);
            if (e.Topic == "/smarthome/news")
            {
                if (message == "off")
                    NavBack();
                else
                    ShowNewsPage();
            }
            else if (e.Topic == "/smarthome/calendar")
            {
                if (message == "off")
                    NavBack();
                else
                    ShowSchedulePage();
            }
            else
                UpdateMessage(message, e.Topic);
        }

        private async void UpdateMessage(string message, string topic)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Low, () =>
            {
                if (topic == "/smarthome/mirrormain")
                    tbMainMessage.Text = message;
                else
                    tbSecMessage.Text = message;
            });
        }

        private void DatetimeUpdateTimer_Tick(object sender, object e)
        {
            UpdateTime();
            UpdateTemp();
            GetFeed();
        }

        public void UpdateTime()
        {
            var now = DateTime.Now;
            tbTime.Text = now.ToString("HH:mm");
            var dateString = now.ToString("D");
            tbDate.Text = dateString.Substring(0, dateString.Length - 6);
            rect2Storyboard.Begin();
            UpdateMovements();
        }


        public async void UpdateSun()
        {
            var rise = DateTime.Parse(await httpClient.GetStringAsync("http://10.10.10.1:8080/rest/items/Sunrise_Time/state"));
            var set = DateTime.Parse(await httpClient.GetStringAsync("http://10.10.10.1:8080/rest/items/Sunset_Time/state"));
            tbSun.Text = rise.ToString("HH:mm") + " - " + set.ToString("HH:mm");
        }

        public async void UpdateTemp()
        {

            var stringData = await httpClient.GetStringAsync("http://10.10.10.1:8080/rest/items/TEMPOUT/state");


            tbTemp.Text = stringData + "°";


        }

        private async void initializeSpeechRecognizer()
        {
            // Initialize recognizer
            recognizer = new SpeechRecognizer();
            recognizer.StateChanged += Recognizer_StateChanged;
            recognizer.ContinuousRecognitionSession.ResultGenerated += RecognizerResultGenerated;

            var grammarContentFile = await Windows.ApplicationModel.Package.Current.InstalledLocation.GetFileAsync(@"Assets\grammar.xml");

            var grammarConstraint = new SpeechRecognitionGrammarFileConstraint(grammarContentFile);
            recognizer.Constraints.Add(grammarConstraint);

            var compilationResult = await recognizer.CompileConstraintsAsync();
            if (compilationResult.Status == SpeechRecognitionResultStatus.Success)
            {
                //Debug.WriteLine("Result: " + compilationResult.ToString());

                await recognizer.ContinuousRecognitionSession.StartAsync();
            }
            else {
                //tbMainMessage.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 0, 0));
            }

        }

        private void RecognizerResultGenerated(SpeechContinuousRecognitionSession sender, SpeechContinuousRecognitionResultGeneratedEventArgs args)
        {
            var cmd = args.Result;
            var command = cmd.SemanticInterpretation.Properties["command"].FirstOrDefault();
            var what = cmd.SemanticInterpretation.Properties["direction"].FirstOrDefault();
            //Debug.WriteLine(command + " " + what);
            if (cmd.Confidence > SpeechRecognitionConfidence.Low)
            {
                if (what == "NEWS")
                {
                    ShowNewsPage();
                }
                else if (what == "SCHEDULE")
                {
                    ShowSchedulePage();
                }
                else if (what == "HOME")
                {
                    NavBack();
                }
            }
        }

        private void Recognizer_StateChanged(SpeechRecognizer sender, SpeechRecognizerStateChangedEventArgs args)
        {
            Debug.WriteLine(args.State);
        }

        private void StackPanel_Loaded(object sender, RoutedEventArgs e)
        {
            rect2Storyboard.Begin();
        }

        private async void tbMainMessage_Tapped(object sender, TappedRoutedEventArgs e)
        {

            ShowNewsPage();

        }

        private async void ShowNewsPage()
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Low, () =>
            {
                this.Frame.Navigate(typeof(NewsViewPage));
            });
        }

        private async void ShowSchedulePage()
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Low, () =>
            {
                this.Frame.Navigate(typeof(CalendarViewPage));
            });
        }


        private async void NavBack()
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Low, () =>
            {
                if (this.Frame.CanGoBack)
                    this.Frame.GoBack();
            });
        }
    }
}
