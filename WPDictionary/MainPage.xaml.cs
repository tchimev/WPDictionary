using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.IO.IsolatedStorage;
using System.IO;

namespace WPDictionary
{
    public partial class MainPage : PhoneApplicationPage
    {
        BingTranslator.TranslatorView _view;
        bool _isNewPageInstance = false;

        public MainPage()
        {
            InitializeComponent();
            _isNewPageInstance = true;
        }

        private void Translate_Click(object sender, EventArgs e)
        {
            _view.TranslateMsg();
        }

        private void Switch_Click(object sender, EventArgs e)
        {
            _view.SwitchLanguages();            
        }

        private void Speak_Click(object sender, EventArgs e)
        {           
            if (this.meSpeak.CurrentState == MediaElementState.Closed && _view.TrySpeakText())
                using (var isoStream = new IsolatedStorageFileStream(BingTranslator.Translator.SpeakFileName, FileMode.Open, IsolatedStorageFile.GetUserStoreForApplication()))
                {
                    this.meSpeak.SetSource(isoStream);
                    this.meSpeak.Position = System.TimeSpan.FromSeconds(0);
                    this.meSpeak.Play();
                }
        }

        private void mElementSpeak_MediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            _view.Error = "Speak failed: " + e.ErrorException.Message;
        }

        private void meSpeak_MediaEnded(object sender, RoutedEventArgs e)
        {
            this.meSpeak.Source = null;
        }

        private void PhoneApplicationPage_BackKeyPress(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_view != null)
                _view.Unload();
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            if (_isNewPageInstance)
            {
                if (_view == null)
                    _view = new BingTranslator.TranslatorView();

                this.LayoutRoot.DataContext = _view;
            }
            _isNewPageInstance = false;
        }
    }
}