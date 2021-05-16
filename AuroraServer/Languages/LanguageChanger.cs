using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace AuroraServer.Languages
{
    class LanguageChanger
    {
        public static void Updatelanguage(string lang)
        {
            List<ResourceDictionary> dictionaryList = new List<ResourceDictionary>();
            foreach (ResourceDictionary dictionary in Application.Current.Resources.MergedDictionaries)
            {
                dictionaryList.Add(dictionary);
            }
            string requestedCulture = "pack://application:,,,/AuroraServer;component/Languages/" + lang + ".xaml";
            ResourceDictionary resourceDictionary = dictionaryList.FirstOrDefault(d => d.Source.OriginalString.Equals(requestedCulture));
            Application.Current.Resources.MergedDictionaries.Remove(resourceDictionary);
            Application.Current.Resources.MergedDictionaries.Add(resourceDictionary);
        }
    }
}
