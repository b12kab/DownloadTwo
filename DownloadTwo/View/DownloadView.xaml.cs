using DownloadTwo.ViewModel;
using System;
using System.Collections.Generic;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace DownloadTwo.View
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class DownloadView : ContentPage
    {
        public DownloadView()
        {
            InitializeComponent();
            this.BindingContext = new DownloadViewModel(Navigation);
        }

        protected override void OnBindingContextChanged()
        {
            DevicePlatform os = DeviceInfo.Platform;
            Version version = DeviceInfo.Version;

            // If Android and API 30 or above, add the pending switch to the options
            if (os == DevicePlatform.Android && version.Major > 10)
            {
                switchStack.IsEnabled = true;
                switchStack.Padding = 8;
            }

            base.OnBindingContextChanged();
        }
    }
}
