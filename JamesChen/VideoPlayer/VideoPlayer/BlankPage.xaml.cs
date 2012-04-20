using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

using System.Threading.Tasks;
using Windows.Graphics.Display;
using Windows.System;
using Windows.UI;
using Windows.Media.PlayTo;
using Windows.Storage.Pickers;
using Windows.Storage;

using System.Collections.ObjectModel;
// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace VideoPlayer
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class BlankPage : Page
    {
        public BlankPage()
        {
            this.InitializeComponent();

            OpenFileBtn.Click += new RoutedEventHandler(OpenFileButton_Click);
        }

        void OpenFileButton_Click(object sender, RoutedEventArgs e)
        {
            PickFileAsync();
        }

        async Task PickFileAsync()
        {

            FileOpenPicker fop = new FileOpenPicker();
            
            fop.FileTypeFilter.Add(".wmv");
            fop.FileTypeFilter.Add(".mp4");
            fop.SuggestedStartLocation = PickerLocationId.VideosLibrary;

            myMediaElement.AutoPlay = false;
            var file = await fop.PickSingleFileAsync();
            if (file != null)
            {
                await SetMediaElementSourceAsync(file);
            }
        }

        async Task SetMediaElementSourceAsync(StorageFile file)
        {
            var stream = await file.OpenAsync(Windows.Storage.FileAccessMode.Read);
            myMediaElement.SetSource(stream, file.ContentType);
        }

        void StartButton_Click(object sender, RoutedEventArgs s)
        {
            myMediaElement.Play();
        }

        void StopButton_Click(object sender, RoutedEventArgs s)
        {
            myMediaElement.Stop();
        }

        void PauseButton_Click(object sender, RoutedEventArgs s)
        {
            myMediaElement.Pause();
        }

        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.  The Parameter
        /// property is typically used to configure the page.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
        }
    }
}
