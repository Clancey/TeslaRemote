using System;
using System.ComponentModel;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using System.Linq;
namespace TeslaRemote.Views {
	public partial class AboutPage : ContentPage {
		public AboutPage ()
		{
			InitializeComponent ();
		}

		async void Button_Clicked (System.Object sender, System.EventArgs e)
		{
			var api = App.TeslaApi;
			try {
				var vResp = await App.TeslaApi.GetVehicles ();
				var vehicle = vResp.First ();
				var wakeSteat = await api.Wakeup (vehicle);
				var trunk = await api.ToggleTrunk (vehicle);
				//var vehicleState = await api.GetAllVehicleData (vehicle);
				//var door = await App.TeslaApi.LockDoors (vehicle);
				//var windowStatus = await App.TeslaApi.WindowControl (vehicle, true,0,0);
				//var windowCloseStatus = await App.TeslaApi.CloseWindow (vehicle);
				//var didHonk = await App.TeslaApi.Honk (vehicle);
				Console.WriteLine ("It worked!!!");
			} catch (Exception ex) {
				Console.WriteLine (ex);
			}
		}
	}
}