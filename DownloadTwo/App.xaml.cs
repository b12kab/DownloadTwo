using System;
using DownloadTwo.View;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace DownloadTwo
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            MainPage = new DownloadView();
        }

        protected override void OnStart()
        {
        }

        protected override void OnSleep()
        {
        }

        protected override void OnResume()
        {
        }
    }
}
