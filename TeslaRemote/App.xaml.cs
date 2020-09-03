using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using TeslaRemote.Services;
using TeslaRemote.Views;

namespace TeslaRemote {
	public partial class App : Application {
		public static readonly TeslaApi TeslaApi = new TeslaApi ("tesla");
		public App ()
		{
			InitializeComponent ();

			DependencyService.Register<MockDataStore> ();
			MainPage = new MainPage ();
		}

		protected override void OnStart ()
		{
		}

		protected override void OnSleep ()
		{
		}

		protected override void OnResume ()
		{
		}
	}
}
