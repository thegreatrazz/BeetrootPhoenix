using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Content Dialog item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Phoenix.UWP
{
    public sealed partial class RequestSettings : ContentDialog
    {
        readonly ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

        public bool AcceptRequests { get; set; }

        public bool ModerateRequests { get; set; }

        public RequestSettings()
        {
            this.InitializeComponent();
            this.AcceptRequests = (bool)localSettings.Values["AcceptRequests"];
            this.ModerateRequests = (bool)localSettings.Values["ModerateRequests"];
        }

        private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            localSettings.Values["AcceptRequests"] = AcceptRequests;
            localSettings.Values["ModerateRequests"] = ModerateRequests;
        }
    }
}
