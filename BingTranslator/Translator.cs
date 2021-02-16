using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Collections.Generic;
using System.ComponentModel;
using Microsoft.Phone.Net.NetworkInformation;
using System.IO;
using System.IO.IsolatedStorage;
using System.Runtime.Serialization;

namespace BingTranslator
{
    public class Translator : IDisposable, INotifyPropertyChanged
    {
        #region Properties

        const string _bingAppID = "AF300408F05D5CCE83DD0180B4DFB7938D60877B";
        const int _minRatingRead = 5;
        const int _minRatingWrite = 4;
        const int _tokenExpireSec = 3600;
        const string _textFormat = "text/plain";
        const string _textCategory = "general";
        const string _audioFormat = "audio/wav";

        public const string SpeakFileName = "speak.wav";

        string _tokenID;
        List<string> _langCodes;
        List<string> _langSpeakCodes;

        ServiceReference1.LanguageServiceClient srvc;

        public Languages SupportedLanguages { get; set; }

        string _text = string.Empty;
        public string Text
        {
            get { return _text; }
            set 
            {
                _text = value;
                OnPropertyChanged("Text");
            } 
        }

        Language _fromLang;
        public Language FromLang
        {
            get { return _fromLang; }
            set
            {
                _fromLang = value;
                OnPropertyChanged("FromLang");
            }
        }

        Language _toLang;
        public Language ToLang
        {
            get { return _toLang; }
            set
            {
                _toLang = value;
                OnPropertyChanged("ToLang");
            }
        }

        public bool CanSpeakToLang { get { return (this._langSpeakCodes != null
                                                    && this.ToLang != null
                                                    && this._langSpeakCodes.Contains(this.ToLang.ShortAbrv)); } }

        public bool HasConnection { get { return NetworkInterface.NetworkInterfaceType != NetworkInterfaceType.None; } }

        Uri _speakUrl;
        public Uri SpeakFileUrl
        {
            get { return _speakUrl; }
            set
            {
                _speakUrl = value;
                OnPropertyChanged("SpeakFileUrl");
            }
        }

        bool _canSpeak = false;
        public bool CanSpeak
        {
            get { return _canSpeak; }
            private set { _canSpeak = value; }
        }

        #endregion

        public event EventHandler<EventArgs> LanguagesLoaded;
        void OnLanguagesLoaded()
        {
            EventHandler<EventArgs> handler = LanguagesLoaded;
            if (handler != null)
                handler(this, new EventArgs());
        }

        public class TranslatedEventArgs : EventArgs
        {
            public TranslatedEventArgs(string res)
            {
                this.Result = res;
            }

            public string Result { get; private set; }
        }
        public event EventHandler<TranslatedEventArgs> Translated;
        void OnTranslated(object sender, TranslatedEventArgs args)
        {
            EventHandler<TranslatedEventArgs> handler = Translated;
            if (handler != null)
                handler(sender, args);
        }

        public Translator()
        {
            if (!this.HasConnection)
                throw new NotSupportedException("Please connect to a network and try again.");

            srvc = new ServiceReference1.LanguageServiceClient();
            this.SupportedLanguages = new Languages();
            srvc.GetAppIdTokenCompleted += new EventHandler<ServiceReference1.GetAppIdTokenCompletedEventArgs>(srvc_GetAppIdTokenCompleted);
            srvc.GetAppIdTokenAsync(_bingAppID, _minRatingRead, _minRatingWrite, _tokenExpireSec);

            srvc.GetLanguagesForTranslateCompleted += new EventHandler<ServiceReference1.GetLanguagesForTranslateCompletedEventArgs>(srvc_GetLanguagesForTranslateCompleted);
            srvc.GetLanguageNamesCompleted += new EventHandler<ServiceReference1.GetLanguageNamesCompletedEventArgs>(srvc_GetLanguageNamesCompleted);
            srvc.TranslateCompleted += new EventHandler<ServiceReference1.TranslateCompletedEventArgs>(srvc_TranslateCompleted);
            srvc.GetLanguagesForSpeakCompleted += new EventHandler<ServiceReference1.GetLanguagesForSpeakCompletedEventArgs>(srvc_GetLanguagesForSpeakCompleted);
            srvc.SpeakCompleted += new EventHandler<ServiceReference1.SpeakCompletedEventArgs>(srvc_SpeakCompleted);
        }

        void srvc_SpeakCompleted(object sender, ServiceReference1.SpeakCompletedEventArgs e)
        {
            if (e.Error == null)
            {
                this.SpeakFileUrl = new Uri(e.Result);

                WebClient client = new WebClient();

                client.OpenReadCompleted += ((s, args) =>
                                            {
                                                using (var isoStream = new IsolatedStorageFileStream(SpeakFileName, FileMode.Create, IsolatedStorageFile.GetUserStoreForApplication()))
                                                {
                                                    byte[] bs = new byte[args.Result.Length];
                                                    args.Result.Read(bs, 0, bs.Length);
                                                    isoStream.Write(bs, 0, bs.Length);
                                                    isoStream.Flush();
                                                    this.CanSpeak = true;
                                                }
                                            });

                client.OpenReadAsync(this.SpeakFileUrl);
            }
            else
                throw e.Error;
        }

        void srvc_GetLanguagesForSpeakCompleted(object sender, ServiceReference1.GetLanguagesForSpeakCompletedEventArgs e)
        {
            if (e.Error == null)
            {
                this._langSpeakCodes = e.Result;
            }
            else
                throw e.Error;
        }

        void srvc_GetLanguageNamesCompleted(object sender, ServiceReference1.GetLanguageNamesCompletedEventArgs e)
        {
            if (e.Error == null)
            {
                Language lang;
                for (int i = 0; i < this._langCodes.Count; i++)
                {
                    lang = new Language() { ShortAbrv = _langCodes[i], FullName = e.Result[i] };
                    this.SupportedLanguages.Add(lang);
                }
                this.FromLang = this.SupportedLanguages[0];
                this.ToLang = this.FromLang;
                OnLanguagesLoaded();
            }
            else
                throw e.Error;
        }

        void srvc_TranslateCompleted(object sender, ServiceReference1.TranslateCompletedEventArgs e)
        {
            if (e.Error == null)
            {
                this.SpeakAsync(e.Result);
                this.OnTranslated(this, new TranslatedEventArgs(e.Result));
            }
            else
                throw e.Error;
        }

        void srvc_GetLanguagesForTranslateCompleted(object sender, ServiceReference1.GetLanguagesForTranslateCompletedEventArgs e)
        {
            if (e.Error == null)
            {
                this._langCodes = e.Result;
                srvc.GetLanguageNamesAsync(_tokenID, System.Globalization.CultureInfo.CurrentCulture.TwoLetterISOLanguageName, this._langCodes);
                srvc.GetLanguagesForSpeakAsync(_tokenID);
            }
            else
                throw e.Error;
        }

        void srvc_GetAppIdTokenCompleted(object sender, ServiceReference1.GetAppIdTokenCompletedEventArgs e)
        {
            if (e.Error == null)
            {
                this._tokenID = e.Result;
                srvc.GetLanguagesForTranslateAsync(_tokenID);
            }
            else
                throw e.Error;
        }

        public void TranslateAsync()
        {
            if (string.IsNullOrEmpty(_tokenID)
                || !this.FromLang.IsValid
                || !this.ToLang.IsValid
                || string.IsNullOrEmpty(this.Text))
                return;

            srvc.TranslateAsync(_tokenID, this.Text, this.FromLang.ShortAbrv, this.ToLang.ShortAbrv, _textFormat, _textCategory);
        }

        public void SpeakAsync(string text)
        {
            this.SpeakFileUrl = null;
            this.CanSpeak = false;
            if (this._langSpeakCodes.Contains(this.ToLang.ShortAbrv))
                this.srvc.SpeakAsync(_tokenID, text, this.ToLang.ShortAbrv, _audioFormat, string.Empty);
        }

        public void Dispose()
        {
            if (srvc != null)
            {
                srvc.TranslateCompleted -= new EventHandler<ServiceReference1.TranslateCompletedEventArgs>(srvc_TranslateCompleted);
                srvc.GetLanguagesForTranslateCompleted -= new EventHandler<ServiceReference1.GetLanguagesForTranslateCompletedEventArgs>(srvc_GetLanguagesForTranslateCompleted);
                srvc.GetAppIdTokenCompleted -= new EventHandler<ServiceReference1.GetAppIdTokenCompletedEventArgs>(srvc_GetAppIdTokenCompleted);
                srvc.GetLanguagesForSpeakCompleted -= new EventHandler<ServiceReference1.GetLanguagesForSpeakCompletedEventArgs>(srvc_GetLanguagesForSpeakCompleted);
                srvc.SpeakCompleted -= new EventHandler<ServiceReference1.SpeakCompletedEventArgs>(srvc_SpeakCompleted);

                srvc.CloseAsync();
                srvc = null;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        void OnPropertyChanged(string propName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(propName));
        }
    }
}
