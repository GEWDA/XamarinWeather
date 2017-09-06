using Android.App;
using Android.Widget;
using Android.OS;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Timers;
using Android.Util;
using Object = Java.Lang.Object;

namespace XamarinWeather
{
    [Activity(Label = "XamarinWeather", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : Activity
    {
        public Timer weatherUpdater = new Timer();
        private TextView temperature;
        private ImageView imgCurrentWeather;
        private TextView degCelc;
        private Spinner locationSpinner;
        private string StrMetService;
        private bool locationSpinnerPopulated;
        public string[] Locations;
        private ArrayAdapter<String> cityArrayAdapter;
        
        private string URL { get; set; }

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            SetContentView(Resource.Layout.Main);

            InitializeThings();
        }

        private void InitializeThings()
        {
            weatherUpdater.Elapsed += GetWeather;
            weatherUpdater.Interval = 10000;
            weatherUpdater.AutoReset = true;
            locationSpinner = FindViewById<Spinner>(Resource.Id.spinnerCity);
            imgCurrentWeather = FindViewById<ImageView>(Resource.Id.imgCurrentWeather);
            temperature = FindViewById<TextView>(Resource.Id.textTemp);
            degCelc = FindViewById<TextView>(Resource.Id.textDegC);
            degCelc.Text = "°C";
            weatherUpdater.Enabled = true;
            Log.Info("myDebug", "Things Initialized");
            //Toast.MakeText(this, "Select a Town or City", ToastLength.Long).Show();
            UpdateWeather();
        }

        private void GetWeather(object sender, ElapsedEventArgs e)
        {
            weatherUpdater.Interval = 10000;//todo: change the 1 to a 60 in final version
            RunOnUiThread(UpdateWeather);

        }

        private void UpdateWeather()
        {
            Log.Info("myDebug", "Updating");//if it contains the word island, select the next item
            if (locationSpinner.SelectedItem!=null && locationSpinner.SelectedItem.ToString().ToLower().Contains("island")) { locationSpinner.SetSelection(Convert.ToInt32(locationSpinner.SelectedItemId) + 1); }
            URL = locationSpinnerPopulated?"http://m.metservice.com/towns-cities/"+locationSpinner.SelectedItem.ToString().Replace(" ","-").ToLower(): "http://m.metservice.com/towns-cities/";
            DownloadData();
        }

        private void DownloadData()
        {
            var webaddress = new Uri(URL);
            var webclient = new WebClient();
            webclient.DownloadStringCompleted += Webclient_DownloadStringCompleted;
            webclient.DownloadStringAsync(webaddress);
            Log.Info("myDebug", "Downloading");
        }

        private void Webclient_DownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            StrMetService = e.Result;
            RunOnUiThread(CleanString);
            locationSpinnerPopulated = true;
        }

        private void CleanString()
        {
            //cleaning up string
            StrMetService = StrMetService.Remove(0, StrMetService.IndexOf("<body"));//removes header
            StrMetService = StrMetService.Replace("\"", String.Empty);//removes " (single quotes) from string

            if (!locationSpinnerPopulated)
            {                
                Log.Info("myDebug", "populating list");
                string StrMetLocations = StrMetService.Substring(StrMetService.IndexOf("<ul>")+4, StrMetService.IndexOf("</ul>") - StrMetService.IndexOf("<ul>")-9);
                StrMetLocations = StrMetLocations.Replace("<li>", String.Empty);
                StrMetLocations = StrMetLocations.Replace("</li>", "\n");
                StrMetLocations = StrMetLocations.Replace("</a>", String.Empty);
                StrMetLocations = StrMetLocations.Replace("<a href=/towns-cities/", String.Empty);
                StrMetLocations = StrMetLocations.Replace("<h2>", String.Empty);
                StrMetLocations = StrMetLocations.Replace("</h2>", String.Empty);

                Locations = StrMetLocations.Split(Convert.ToChar("\n"));
                for (int i = 0; i < Locations.Length; i++)
                {
                    Locations[i]=Locations[i].Substring(Locations[i].IndexOf(">") + 1).Trim(Convert.ToChar(" "));
                }
                cityArrayAdapter = new ArrayAdapter<string>(this, Android.Resource.Layout.SimpleSpinnerItem, Locations);
                cityArrayAdapter.SetDropDownViewResource(Android.Resource.Layout.SimpleSpinnerDropDownItem);
                locationSpinner.Adapter = cityArrayAdapter;

                locationSpinner.SetSelection(1);
            }




            //if it is a city/location
            if(locationSpinnerPopulated)
            {
                int tempIndexLeft = StrMetService.IndexOf("<span class=actual-temp>") + "<span class=actual-temp>".Length;
                int tempLength = StrMetService.IndexOf("<span class=temp>&deg;C</span><span></span>")-tempIndexLeft;
                Log.Info("myDebug", "Showing temperature");
                if(tempLength<1)
                {
                    Toast.MakeText(this, "Exact temperature data could not be found", ToastLength.Long).Show();
                    tempIndexLeft = StrMetService.IndexOf("<span class=max>") + "<span class=max>".Length;
                    tempLength = StrMetService.IndexOf("<span class=temp>&deg;C</span></span> Overnight min<span class='min'>");
                    int tempMinIndLeft = tempLength + "<span class=temp>&deg;C</span></span> Overnight min<span class='min'>".Length;
                    tempLength -= tempIndexLeft;
                    int tempMinLength = StrMetService.IndexOf("<span class=temp>&deg;C</span></span></p>") - tempMinIndLeft;
                    temperature.Text = StrMetService.Substring(tempMinIndLeft, tempMinLength) + " - " + StrMetService.Substring(tempIndexLeft, tempLength);
                    return;
                }
                temperature.Text = StrMetService.Substring(tempIndexLeft, tempLength);
                return;
            }

        }
    }
}
