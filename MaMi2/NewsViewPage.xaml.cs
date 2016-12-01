using System;
using System.Collections.Generic;
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
    public sealed partial class NewsViewPage : Page
    {


        //       HttpClient httpClient = new HttpClient();

        DispatcherTimer backTimer;

        public NewsViewPage()
        {
            this.InitializeComponent();
            backTimer = new DispatcherTimer();
            backTimer.Tick += BackTimer_Tick; ;
            //backTimer.Interval = TimeSpan.FromSeconds(5);
            backTimer.Interval = TimeSpan.FromMinutes(3);
            backTimer.Start();
            GetFeed();
            
        }

        private void BackTimer_Tick(object sender, object e)
        {
            backTimer.Stop();
            this.Frame.GoBack();
        }

        private async void GetFeed() {
            var client = new Windows.Web.Syndication.SyndicationClient();
            var feed = await client.RetrieveFeedAsync(new Uri("http://www.aftonbladet.se/nyheter/rss.xml"));
            var lastNews = feed.Items.Take(10).ToList();
            listView.ItemsSource = lastNews;
            
        }
        
    }
}
