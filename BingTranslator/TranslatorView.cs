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
using System.ComponentModel;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.Phone.Net.NetworkInformation;
using System.Runtime.Serialization;

namespace BingTranslator
{
    public class TranslatorView : INotifyPropertyChanged
    {
        Translator _trans;
        public Translator Translator
        {
            get
            { return _trans; }
            set
            {
                _trans = value;
                OnPropertyChanged("Translator");
            } 
        }

        string _toMsg = string.Empty;
        public string ToMessage
        {
            get { return _toMsg; }
            set
            {
                _toMsg = value;
                OnPropertyChanged("ToMessage");
            } 
        }

        string _error = string.Empty;
        public string Error
        {
            get { return _error; }
            set
            {
                _error = value;
                OnPropertyChanged("Error");
            }
        }

        public TranslatorView()
        {
            try
            {
                Translator = new Translator();
                Translator.Translated += new EventHandler<BingTranslator.Translator.TranslatedEventArgs>(_translator_Translated);
            }
            catch (Exception ex)
            {
                this.Error = ex.Message;
            }
        }

        void _translator_Translated(object sender, BingTranslator.Translator.TranslatedEventArgs e)
        {
            this.ToMessage = e.Result;
        }

        public void TranslateMsg()
        {
            try
            {
                if (this.Translator != null)
                    Translator.TranslateAsync();

                this.Error = string.Empty;
            }
            catch (Exception ex)
            {
                this.Error = ex.Message;
            }
        }

        public bool TrySpeakText()
        {
            if (!this.Translator.CanSpeakToLang)
                this.Error = "Language " + this.Translator.ToLang.FullName + " not supported.";
            else if (!this.Translator.CanSpeak)
                this.Error = "Speak unavailable. Try again later.";
            else
                this.Error = string.Empty;

            return this.Translator.CanSpeak && this.Translator.CanSpeakToLang;
        }

        public void SwitchLanguages()
        {
            try
            {
                if (this.Translator != null)
                {
                    var temp = Translator.FromLang;
                    Translator.FromLang = Translator.ToLang;
                    Translator.ToLang = temp;

                    Translator.Text = string.Empty;
                    this.ToMessage = string.Empty;
                }
                this.Error = string.Empty;
            }
            catch (Exception ex)
            {
                this.Error = ex.Message;
            }
        }

        public void Unload()
        {
            try
            {
                if (Translator != null)
                {
                    Translator.Translated -= new EventHandler<BingTranslator.Translator.TranslatedEventArgs>(_translator_Translated);
                    Translator.Dispose();
                }
            }
            catch (Exception ex)
            {
                this.Error = ex.Message;
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
