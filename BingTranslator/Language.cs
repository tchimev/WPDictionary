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
using System.Collections.ObjectModel;
using System.Runtime.Serialization;

namespace BingTranslator
{
    public class Language : INotifyPropertyChanged
    {
        string _shortAbrv;
        public string ShortAbrv
        {
            get { return _shortAbrv; }
            set
            {
                _shortAbrv = value;
                OnPropertyChanged("ShortAbrv");
            }
        }

        string _fullName;
        public string FullName
        {
            get { return _fullName; }
            set
            {
                _fullName = value;
                OnPropertyChanged("FullName");
            }
        }

        public bool IsValid { get { return !(string.IsNullOrEmpty(this.FullName) && string.IsNullOrEmpty(this.ShortAbrv)); } }

        public Language Copy()
        {
            var l = new Language();
            l.ShortAbrv = string.Copy(this.ShortAbrv);
            l.FullName = string.Copy(this.FullName);

            return l;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        void OnPropertyChanged(string propName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(propName));
        }
    }

    public class Languages : ObservableCollection<Language>
    {
        public Languages() { }
    }
}
