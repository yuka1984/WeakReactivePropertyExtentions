using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Xamarin.Forms;

namespace App1
{
	public partial class App : Application
	{
        private IService _service;
		public App ()
		{
			InitializeComponent();

            _service = new WeakService();
            var navpage = new NavigationPage();

            navpage.Navigation.PushAsync(new MainPage { BindingContext = new MainPageViewModel(_service, navpage.Navigation) });
            MainPage = navpage;
		}

		protected override void OnStart ()
		{
			// Handle when your app starts
		}

		protected override void OnSleep ()
		{
			// Handle when your app sleeps
		}

		protected override void OnResume ()
		{
			// Handle when your app resumes
		}
	}
}
