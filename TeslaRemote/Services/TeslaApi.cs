using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SimpleAuth;

namespace TeslaRemote.Services {
	public class TeslaApi : SimpleAuth.OAuthPasswordApi {
		public TeslaApi (string id) : base (id,
			"81527cff06843c8634fdc09e8ac0abefb46ac849f38fe1e431c2ef2106796384",
			"c7257eb71a564034f9419ee651c7d0e5f7aa6bfbd18bafb5c5c033b093bb2fa3",
			"https://owner-api.teslamotors.com/oauth/token?grant_type=password", "https://owner-api.teslamotors.com/oauth/token?grant_type=password", "https://owner-api.teslamotors.com/oauth/token?grant_type=refresh_token")
		{
			BaseAddress = new Uri ("https://owner-api.teslamotors.com/api/1/");
			Converter = new InspectorConverter ();
		}

		//State
		public async Task<IList<Vehicle>> GetVehicles ()
		{
			const string path = "vehicles";
			var resp = await Get<CommandListResponse<Vehicle>> (path);
			return resp.Response;
		}


		public async Task<VehicleData> GetAllVehicleData (Vehicle vehicle)
		{
			var path = $"vehicles/{vehicle.Id}/vehicle_data";
			var resp = await Get<CommandResponse<VehicleData>> (path);
			return resp.Response;
		}

		public async Task<ChargeState> GetChargeState (Vehicle vehicle)
		{
			var path = $"vehicles/{vehicle.Id}/data_request/charge_state";
			var resp = await Get<CommandResponse<ChargeState>> (path);
			return resp.Response;
		}

		public async Task<ClimateState> GetClimateState (Vehicle vehicle)
		{
			var path = $"vehicles/{vehicle.Id}/data_request/climate_state";
			var resp = await Get<CommandResponse<ClimateState>> (path);
			return resp.Response;
		}

		public async Task<DriveState> GetDriveState (Vehicle vehicle)
		{
			var path = $"vehicles/{vehicle.Id}/data_request/drive_state";
			var resp = await Get<CommandResponse<DriveState>> (path);
			return resp.Response;
		}

		public async Task<GuiSettings> GetGuiSettings (Vehicle vehicle)
		{
			var path = $"vehicles/{vehicle.Id}/data_request/gui_settings";
			var resp = await Get<CommandResponse<GuiSettings>> (path);
			return resp.Response;
		}

		public async Task<VehicleState> GetVehicleState (Vehicle vehicle)
		{
			var path = $"vehicles/{vehicle.Id}/data_request/vehicle_state";
			var resp = await Get<CommandResponse<VehicleState>> (path);
			return resp.Response;
		}

		public async Task<VehicleConfig> GetVehicleConfig (Vehicle vehicle)
		{
			var path = $"vehicles/{vehicle.Id}/data_request/vehicle_config";
			var resp = await Get<CommandResponse<VehicleConfig>> (path);
			return resp.Response;
		}

		public async Task<bool> MobileEnabled (Vehicle vehicle)
		{
			var path = $"vehicles/{vehicle.Id}/mobile_enabled";
			var resp = await Get<CommandResponse<CommandResponse>> (path);
			return resp.Response.Result;
		}

		public async Task<ChargingSitesResponse> NearbyChargingSites (Vehicle vehicle)
		{
			var path = $"vehicles/{vehicle.Id}/nearby_charging_sites";
			var resp = await Get<CommandResponse<ChargingSitesResponse>> (path);
			return resp.Response;
		}

		//Commands 
		public async Task<VehicleData> Wakeup (Vehicle vehicle, bool waitForWakeup = true, int timeout = 30)
		{
			string path = $"vehicles/{vehicle.Id}/wake_up";
			var resp = await Post<CommandResponse<VehicleData>> (path, "");
			if (resp.Response.State == "online")
				return resp.Response;
			if (waitForWakeup) {
				var startTime = DateTime.Now;
				var success = false;
				VehicleData carState = null;
				while (!success && (DateTime.Now - startTime).TotalSeconds < timeout) {
					carState = await GetAllVehicleData (vehicle);
					//TODO: change to enums!
					if (carState.State == "online")
						return carState;
					await Task.Delay (1000);
				}
				return carState;

			}
			return resp.Response;
		}

		public async Task<CommandResponse> Honk (Vehicle vehicle)
		{
			string path = $"vehicles/{vehicle.Id}/command/honk_horn";
			var resp = await Post<CommandResponse<CommandResponse>> (path, "");
			return resp.Response;
		}

		public async Task<CommandResponse> FlashLights (Vehicle vehicle)
		{
			string path = $"vehicles/{vehicle.Id}/command/flash_lights";
			var resp = await Post<CommandResponse<CommandResponse>> (path, "");
			return resp.Response;
		}

		public async Task<CommandResponse> EnableRemoteStart (Vehicle vehicle, string password)
		{
			string path = $"vehicles/{vehicle.Id}/command/remote_start_drive";
			var resp = await Post<CommandResponse<CommandResponse>> (new {password = password }, path, queryParameters: new Dictionary<string, string> {
				["password"] = password,
			});
			return resp.Response;
		}

		public async Task<CommandResponse> SetSpeedLimit (Vehicle vehicle, int speed)
		{
			if (speed < 50 || speed > 90) {
				return new CommandResponse { Reason = "Must be between 50 and 90 MPH", Result = false };
			}
			string path = $"vehicles/{vehicle.Id}/command/remote_start_drive";
			var resp = await Post<CommandResponse<CommandResponse>> (new { limit_mph  = speed}, path);
			return resp.Response;
		}

		public async Task<CommandResponse> ActivateSpeedLimit (Vehicle vehicle, string pin = null)
		{
			if(!string.IsNullOrWhiteSpace(pin) && ( int.TryParse(pin, out var foo) && pin.Trim().Length != 4))
				return new CommandResponse { Reason = "Pin must be 4 digits", Result = false };
			string path = $"vehicles/{vehicle.Id}/command/speed_limit_activate";
			var resp = await Post<CommandResponse<CommandResponse>> (new {pin = pin }, path);
			return resp.Response;
		}

		public async Task<CommandResponse> SetAndActivateSpeedLimit(Vehicle vehicle, int speed, string pin = null)
		{
			var r = await SetSpeedLimit (vehicle, speed);
			if (r.Result != true)
				return r;
			return await ActivateSpeedLimit (vehicle, pin);
		}


		public async Task<CommandResponse> ActivateValet (Vehicle vehicle, string pin = null)
		{
			if (!string.IsNullOrWhiteSpace (pin) && (int.TryParse (pin, out var foo) && pin.Trim ().Length != 4))
				return new CommandResponse { Reason = "Pin must be 4 digits", Result = false };
			string path = $"vehicles/{vehicle.Id}/command/set_valet_mode";
			var resp = await Post<CommandResponse<CommandResponse>> (new { password = pin}, path);
			return resp.Response;
		}

		public async Task<CommandResponse> ResetValetPin (Vehicle vehicle)
		{
			string path = $"vehicles/{vehicle.Id}/command/reset_valet_pin";
			var resp = await Post<CommandResponse<CommandResponse>> (path, "");
			return resp.Response;
		}

		public async Task<CommandResponse> SetSentryMode (Vehicle vehicle, bool state)
		{
			var on = state ? "on" : "off";
			string path = $"vehicles/{vehicle.Id}/command/set_sentry_mode";
			var resp = await Post<CommandResponse<CommandResponse>> (new {on = on },path);
			return resp.Response;
		}

		public async Task<CommandResponse> UnlockDoors (Vehicle vehicle)
		{
			string path = $"vehicles/{vehicle.Id}/command/door_unlock";
			var resp = await Post<CommandResponse<CommandResponse>> (path, "");
			return resp.Response;
		}

		public async Task<CommandResponse> LockDoors (Vehicle vehicle)
		{
			string path = $"vehicles/{vehicle.Id}/command/door_lock";
			var resp = await Post<CommandResponse<CommandResponse>> (path, "");
			return resp.Response;
		}

		async Task<CommandResponse> ActuateTrunk(Vehicle vehicle, string whichTrunk)
		{
			string path = $"vehicles/{vehicle.Id}/command/actuate_trunk";
			var resp = await Post<CommandResponse<CommandResponse>> (new  { which_trunk = whichTrunk },path);
			return resp.Response;
		}

		public Task<CommandResponse> OpenFrunk (Vehicle vehicle) => ActuateTrunk (vehicle, "front");

		public Task<CommandResponse> ToggleTrunk (Vehicle vehicle) => ActuateTrunk (vehicle, "rear");





		public async Task<CommandResponse> ClimateConditioningStart (Vehicle vehicle)
		{
			string path = $"vehicles/{vehicle.Id}/command/auto_conditioning_start";
			var resp = await Post<CommandResponse<CommandResponse>> (path, "");
			return resp.Response;
		}

		public async Task<CommandResponse> ClimateConditioningStop (Vehicle vehicle)
		{
			string path = $"vehicles/{vehicle.Id}/command/auto_conditioning_stop";
			var resp = await Post<CommandResponse<CommandResponse>> (path, "");
			return resp.Response;
		}

		public async Task<CommandResponse> CloseWindow (Vehicle vehicle)
		{
			var data = await GetAllVehicleData (vehicle);
			var resp = await WindowControl (vehicle, false, data.DriveState.Latitude, data.DriveState.Longitude);
			return resp;
		}





		public async Task<CommandResponse> WindowControl (Vehicle vehicle, bool open, double latitude = 0, double longitude = 0)
		{
			const string path = "vehicles/{id}/command/window_control";
			var cmd = open ? "vent" : "close";
			var resp = await Post<CommandResponse<CommandResponse>> (new { command = cmd, lat = latitude, lon = longitude }, path, queryParameters: new Dictionary<string, string> {
				["id"] = vehicle.Id.ToString (),
			});
			return resp.Response;

		}

		

		protected override WebAuthenticator CreateAuthenticator ()
		{
			return authenticator as TeslaAuthenticator ?? (authenticator = new TeslaAuthenticator {
				BaseUrl = TokenUrl,
				Api = this,
				ClientId = ClientId,
				ClientSecret = ClientSecret

			});
		}

		protected override Task<OAuthAccount> GetAccountFromAuthCode (WebAuthenticator authenticator, string identifier)
		{
			var auth = authenticator as TeslaAuthenticator;
			var account = new OAuthAccount () {
				ExpiresIn = auth.Token.ExpiresIn,
				Created = DateTime.UtcNow,
				RefreshToken = auth.Token.RefreshToken,
				Scope = authenticator.Scope?.ToArray (),
				TokenType = auth.Token.TokenType,
				Token = auth.Token.AccessToken,
				ClientId = ClientId,
				Identifier = identifier,
			};
			return Task.FromResult (account);
		}

		class InspectorConverter : SimpleAuth.JsonConverter {
			public override async Task<T> Deserialize<T> (HttpResponseMessage response)
			{
				var json = await response.Content.ReadAsStringAsync ();
				Debug.WriteLine (json);
				return await base.Deserialize<T> (response);
			}
		}


		public class TeslaAuthenticator : WebAuthenticator, IBasicAuthenicator {
			public override string BaseUrl { get; set; }

			public override Uri RedirectUrl { get; set; }

			public AuthTokenClass Token { get; set; }

			WeakReference api;

			public OAuthPasswordApi Api {
				get {
					return api?.Target as OAuthPasswordApi;
				}

				set {
					api = new WeakReference (value);
				}
			}

			public string ClientSecret { get; set; }
			public async Task<bool> VerifyCredentials (string username, string password)
			{
				Api.EnsureApiStatusCode = false;
				var tokenString = await Api.Post (new Dictionary<string, string> {
					{"client_id" ,ClientId },
					{"client_secret", ClientSecret },
					{ "email", username},
					{"password",password},
					{"grant_type","password"}
				}, BaseUrl, authenticated: false).ConfigureAwait (false);

				var token = tokenString.ToObject<AuthTokenClass> ();
				if (!string.IsNullOrEmpty (token.AccessToken)) {
					Token = token;
					this.FoundAuthCode (token.AccessToken);
					return true;
				}
				return false;
			}
			public class AuthTokenClass {

				[JsonProperty ("access_token")]
				public string AccessToken { get; set; }

				[JsonProperty ("token_type")]
				public string TokenType { get; set; }

				[JsonProperty ("expires_in")]
				public int ExpiresIn { get; set; }

				[JsonProperty ("refresh_token")]
				public string RefreshToken { get; set; }

				[JsonProperty ("userName")]
				public string UserName { get; set; }

				[JsonProperty (".issued")]
				public DateTime Issued { get; set; }

				[JsonProperty (".expires")]
				public DateTime Expires { get; set; }

				public string Error { get; set; }
			}


			public Task<bool> SignUp (string username, string password)
			{
				return Task.FromResult (false);
			}
		}
	}



	///Responses
	///


	public class Location {

		[JsonProperty ("lat")]
		public double Lat { get; set; }

		[JsonProperty ("long")]
		public double Long { get; set; }
	}

	public class DestinationCharging {

		[JsonProperty ("location")]
		public Location Location { get; set; }

		[JsonProperty ("name")]
		public string Name { get; set; }

		[JsonProperty ("type")]
		public string Type { get; set; }

		[JsonProperty ("distance_miles")]
		public double DistanceMiles { get; set; }
	}

	public class Supercharger {

		[JsonProperty ("location")]
		public Location Location { get; set; }

		[JsonProperty ("name")]
		public string Name { get; set; }

		[JsonProperty ("type")]
		public string Type { get; set; }

		[JsonProperty ("distance_miles")]
		public double DistanceMiles { get; set; }

		[JsonProperty ("available_stalls")]
		public int AvailableStalls { get; set; }

		[JsonProperty ("total_stalls")]
		public int TotalStalls { get; set; }

		[JsonProperty ("site_closed")]
		public bool SiteClosed { get; set; }
	}

	public class ChargingSitesResponse {

		[JsonProperty ("congestion_sync_time_utc_secs")]
		public int CongestionSyncTimeUtcSecs { get; set; }

		[JsonProperty ("destination_charging")]
		public IList<DestinationCharging> DestinationCharging { get; set; }

		[JsonProperty ("superchargers")]
		public IList<Supercharger> Superchargers { get; set; }

		[JsonProperty ("timestamp")]
		public long Timestamp { get; set; }
	}

	public class CommandResponse<T> {

		[JsonProperty ("response")]
		public T Response { get; set; }
	}
	public class CommandListResponse<T> {

		[JsonProperty ("response")]
		public IList<T> Response { get; set; }

		[JsonProperty ("count")]
		public int Count { get; set; }
	}

	public class CommandResponse {

		[JsonProperty ("reason")]
		public string Reason { get; set; }

		[JsonProperty ("result")]
		public bool Result { get; set; }
	}

	public class Vehicle {

		[JsonProperty ("id")]
		public long Id { get; set; }

		[JsonProperty ("vehicle_id")]
		public int VehicleId { get; set; }

		[JsonProperty ("vin")]
		public string Vin { get; set; }

		[JsonProperty ("display_name")]
		public string DisplayName { get; set; }

		[JsonProperty ("option_codes")]
		public string OptionCodes { get; set; }

		[JsonProperty ("color")]
		public object Color { get; set; }

		[JsonProperty ("tokens")]
		public IList<string> Tokens { get; set; }

		[JsonProperty ("state")]
		public string State { get; set; }

		[JsonProperty ("in_service")]
		public bool? InService { get; set; }

		[JsonProperty ("id_s")]
		public string IdS { get; set; }

		[JsonProperty ("calendar_enabled")]
		public bool? CalendarEnabled { get; set; }

		[JsonProperty ("api_version")]
		public int ApiVersion { get; set; }

		[JsonProperty ("backseat_token")]
		public object BackseatToken { get; set; }

		[JsonProperty ("backseat_token_updated_at")]
		public object BackseatTokenUpdatedAt { get; set; }
	}

	public class DriveState {

		[JsonProperty ("gps_as_of")]
		public int GpsAsOf { get; set; }

		[JsonProperty ("heading")]
		public int Heading { get; set; }

		[JsonProperty ("latitude")]
		public double Latitude { get; set; }

		[JsonProperty ("longitude")]
		public double Longitude { get; set; }

		[JsonProperty ("native_latitude")]
		public double NativeLatitude { get; set; }

		[JsonProperty ("native_location_supported")]
		public int NativeLocationSupported { get; set; }

		[JsonProperty ("native_longitude")]
		public double NativeLongitude { get; set; }

		[JsonProperty ("native_type")]
		public string NativeType { get; set; }

		[JsonProperty ("power")]
		public int Power { get; set; }

		[JsonProperty ("shift_state")]
		public object ShiftState { get; set; }

		[JsonProperty ("speed")]
		public object Speed { get; set; }

		[JsonProperty ("timestamp")]
		public long Timestamp { get; set; }
	}

	public class ClimateState {

		[JsonProperty ("battery_heater")]
		public bool? BatteryHeater { get; set; }

		[JsonProperty ("battery_heater_no_power")]
		public bool? BatteryHeaterNoPower { get; set; }

		[JsonProperty ("climate_keeper_mode")]
		public string ClimateKeeperMode { get; set; }

		[JsonProperty ("defrost_mode")]
		public int DefrostMode { get; set; }

		[JsonProperty ("driver_temp_setting")]
		public double DriverTempSetting { get; set; }

		[JsonProperty ("fan_status")]
		public int FanStatus { get; set; }

		[JsonProperty ("inside_temp")]
		public object InsideTemp { get; set; }

		[JsonProperty ("is_auto_conditioning_on")]
		public object IsAutoConditioningOn { get; set; }

		[JsonProperty ("is_climate_on")]
		public bool? IsClimateOn { get; set; }

		[JsonProperty ("is_front_defroster_on")]
		public bool? IsFrontDefrosterOn { get; set; }

		[JsonProperty ("is_preconditioning")]
		public bool? IsPreconditioning { get; set; }

		[JsonProperty ("is_rear_defroster_on")]
		public bool? IsRearDefrosterOn { get; set; }

		[JsonProperty ("left_temp_direction")]
		public object LeftTempDirection { get; set; }

		[JsonProperty ("max_avail_temp")]
		public double MaxAvailTemp { get; set; }

		[JsonProperty ("min_avail_temp")]
		public double MinAvailTemp { get; set; }

		[JsonProperty ("outside_temp")]
		public object OutsideTemp { get; set; }

		[JsonProperty ("passenger_temp_setting")]
		public double PassengerTempSetting { get; set; }

		[JsonProperty ("remote_heater_control_enabled")]
		public bool? RemoteHeaterControlEnabled { get; set; }

		[JsonProperty ("right_temp_direction")]
		public object RightTempDirection { get; set; }

		[JsonProperty ("seat_heater_left")]
		public int SeatHeaterLeft { get; set; }

		[JsonProperty ("seat_heater_rear_center")]
		public int SeatHeaterRearCenter { get; set; }

		[JsonProperty ("seat_heater_rear_left")]
		public int SeatHeaterRearLeft { get; set; }

		[JsonProperty ("seat_heater_rear_left_back")]
		public int SeatHeaterRearLeftBack { get; set; }

		[JsonProperty ("seat_heater_rear_right")]
		public int SeatHeaterRearRight { get; set; }

		[JsonProperty ("seat_heater_rear_right_back")]
		public int SeatHeaterRearRightBack { get; set; }

		[JsonProperty ("seat_heater_right")]
		public int SeatHeaterRight { get; set; }

		[JsonProperty ("side_mirror_heaters")]
		public bool? SideMirrorHeaters { get; set; }

		[JsonProperty ("steering_wheel_heater")]
		public bool? SteeringWheelHeater { get; set; }

		[JsonProperty ("timestamp")]
		public long Timestamp { get; set; }

		[JsonProperty ("wiper_blade_heater")]
		public bool? WiperBladeHeater { get; set; }
	}

	public class ChargeState {

		[JsonProperty ("battery_heater_on")]
		public bool? BatteryHeaterOn { get; set; }

		[JsonProperty ("battery_level")]
		public int BatteryLevel { get; set; }

		[JsonProperty ("battery_range")]
		public double BatteryRange { get; set; }

		[JsonProperty ("charge_current_request")]
		public int ChargeCurrentRequest { get; set; }

		[JsonProperty ("charge_current_request_max")]
		public int ChargeCurrentRequestMax { get; set; }

		[JsonProperty ("charge_enable_request")]
		public bool? ChargeEnableRequest { get; set; }

		[JsonProperty ("charge_energy_added")]
		public double ChargeEnergyAdded { get; set; }

		[JsonProperty ("charge_limit_soc")]
		public int ChargeLimitSoc { get; set; }

		[JsonProperty ("charge_limit_soc_max")]
		public int ChargeLimitSocMax { get; set; }

		[JsonProperty ("charge_limit_soc_min")]
		public int ChargeLimitSocMin { get; set; }

		[JsonProperty ("charge_limit_soc_std")]
		public int ChargeLimitSocStd { get; set; }

		[JsonProperty ("charge_miles_added_ideal")]
		public double ChargeMilesAddedIdeal { get; set; }

		[JsonProperty ("charge_miles_added_rated")]
		public double ChargeMilesAddedRated { get; set; }

		[JsonProperty ("charge_port_cold_weather_mode")]
		public bool? ChargePortColdWeatherMode { get; set; }

		[JsonProperty ("charge_port_door_open")]
		public bool? ChargePortDoorOpen { get; set; }

		[JsonProperty ("charge_port_latch")]
		public string ChargePortLatch { get; set; }

		[JsonProperty ("charge_rate")]
		public double ChargeRate { get; set; }

		[JsonProperty ("charge_to_max_range")]
		public bool? ChargeToMaxRange { get; set; }

		[JsonProperty ("charger_actual_current")]
		public int ChargerActualCurrent { get; set; }

		[JsonProperty ("charger_phases")]
		public object ChargerPhases { get; set; }

		[JsonProperty ("charger_pilot_current")]
		public int ChargerPilotCurrent { get; set; }

		[JsonProperty ("charger_power")]
		public int ChargerPower { get; set; }

		[JsonProperty ("charger_voltage")]
		public int ChargerVoltage { get; set; }

		[JsonProperty ("charging_state")]
		public string ChargingState { get; set; }

		[JsonProperty ("conn_charge_cable")]
		public string ConnChargeCable { get; set; }

		[JsonProperty ("est_battery_range")]
		public double EstBatteryRange { get; set; }

		[JsonProperty ("fast_charger_brand")]
		public string FastChargerBrand { get; set; }

		[JsonProperty ("fast_charger_present")]
		public bool? FastChargerPresent { get; set; }

		[JsonProperty ("fast_charger_type")]
		public string FastChargerType { get; set; }

		[JsonProperty ("ideal_battery_range")]
		public double IdealBatteryRange { get; set; }

		[JsonProperty ("managed_charging_active")]
		public bool? ManagedChargingActive { get; set; }

		[JsonProperty ("managed_charging_start_time")]
		public object ManagedChargingStartTime { get; set; }

		[JsonProperty ("managed_charging_user_canceled")]
		public bool? ManagedChargingUserCanceled { get; set; }

		[JsonProperty ("max_range_charge_counter")]
		public int MaxRangeChargeCounter { get; set; }

		[JsonProperty ("minutes_to_full_charge")]
		public int MinutesToFullCharge { get; set; }

		[JsonProperty ("not_enough_power_to_heat")]
		public bool? NotEnoughPowerToHeat { get; set; }

		[JsonProperty ("scheduled_charging_pending")]
		public bool? ScheduledChargingPending { get; set; }

		[JsonProperty ("scheduled_charging_start_time")]
		public object ScheduledChargingStartTime { get; set; }

		[JsonProperty ("time_to_full_charge")]
		public double TimeToFullCharge { get; set; }

		[JsonProperty ("timestamp")]
		public long Timestamp { get; set; }

		[JsonProperty ("trip_charging")]
		public bool? TripCharging { get; set; }

		[JsonProperty ("usable_battery_level")]
		public int UsableBatteryLevel { get; set; }

		[JsonProperty ("user_charge_enable_request")]
		public object UserChargeEnableRequest { get; set; }
	}

	public class GuiSettings {

		[JsonProperty ("gui_24_hour_time")]
		public bool? Gui24HourTime { get; set; }

		[JsonProperty ("gui_charge_rate_units")]
		public string GuiChargeRateUnits { get; set; }

		[JsonProperty ("gui_distance_units")]
		public string GuiDistanceUnits { get; set; }

		[JsonProperty ("gui_range_display")]
		public string GuiRangeDisplay { get; set; }

		[JsonProperty ("gui_temperature_units")]
		public string GuiTemperatureUnits { get; set; }

		[JsonProperty ("show_range_units")]
		public bool? ShowRangeUnits { get; set; }

		[JsonProperty ("timestamp")]
		public long Timestamp { get; set; }
	}

	public class MediaState {

		[JsonProperty ("remote_control_enabled")]
		public bool? RemoteControlEnabled { get; set; }
	}

	public class SoftwareUpdate {

		[JsonProperty ("download_perc")]
		public int DownloadPerc { get; set; }

		[JsonProperty ("expected_duration_sec")]
		public int ExpectedDurationSec { get; set; }

		[JsonProperty ("install_perc")]
		public int InstallPerc { get; set; }

		[JsonProperty ("scheduled_time_ms")]
		public long ScheduledTimeMs { get; set; }

		[JsonProperty ("status")]
		public string Status { get; set; }

		[JsonProperty ("version")]
		public string Version { get; set; }
	}

	public class SpeedLimitMode {

		[JsonProperty ("active")]
		public bool? Active { get; set; }

		[JsonProperty ("current_limit_mph")]
		public double CurrentLimitMph { get; set; }

		[JsonProperty ("max_limit_mph")]
		public int MaxLimitMph { get; set; }

		[JsonProperty ("min_limit_mph")]
		public int MinLimitMph { get; set; }

		[JsonProperty ("pin_code_set")]
		public bool? PinCodeSet { get; set; }
	}

	public class VehicleState {

		[JsonProperty ("api_version")]
		public int ApiVersion { get; set; }

		[JsonProperty ("autopark_state_v2")]
		public string AutoparkStateV2 { get; set; }

		[JsonProperty ("autopark_style")]
		public string AutoparkStyle { get; set; }

		[JsonProperty ("calendar_supported")]
		public bool? CalendarSupported { get; set; }

		[JsonProperty ("car_version")]
		public string CarVersion { get; set; }

		[JsonProperty ("center_display_state")]
		public int CenterDisplayState { get; set; }

		[JsonProperty ("df")]
		public int Df { get; set; }

		[JsonProperty ("dr")]
		public int Dr { get; set; }

		[JsonProperty ("fd_window")]
		public int FdWindow { get; set; }

		[JsonProperty ("fp_window")]
		public int FpWindow { get; set; }

		[JsonProperty ("ft")]
		public int Ft { get; set; }

		[JsonProperty ("homelink_device_count")]
		public int HomelinkDeviceCount { get; set; }

		[JsonProperty ("homelink_nearby")]
		public bool? HomelinkNearby { get; set; }

		[JsonProperty ("is_user_present")]
		public bool? IsUserPresent { get; set; }

		[JsonProperty ("last_autopark_error")]
		public string LastAutoparkError { get; set; }

		[JsonProperty ("locked")]
		public bool? Locked { get; set; }

		[JsonProperty ("media_state")]
		public MediaState MediaState { get; set; }

		[JsonProperty ("notifications_supported")]
		public bool? NotificationsSupported { get; set; }

		[JsonProperty ("odometer")]
		public double Odometer { get; set; }

		[JsonProperty ("parsed_calendar_supported")]
		public bool? ParsedCalendarSupported { get; set; }

		[JsonProperty ("pf")]
		public int Pf { get; set; }

		[JsonProperty ("pr")]
		public int Pr { get; set; }

		[JsonProperty ("rd_window")]
		public int RdWindow { get; set; }

		[JsonProperty ("remote_start")]
		public bool? RemoteStart { get; set; }

		[JsonProperty ("remote_start_enabled")]
		public bool? RemoteStartEnabled { get; set; }

		[JsonProperty ("remote_start_supported")]
		public bool? RemoteStartSupported { get; set; }

		[JsonProperty ("rp_window")]
		public int RpWindow { get; set; }

		[JsonProperty ("rt")]
		public int Rt { get; set; }

		[JsonProperty ("sentry_mode")]
		public bool? SentryMode { get; set; }

		[JsonProperty ("sentry_mode_available")]
		public bool? SentryModeAvailable { get; set; }

		[JsonProperty ("smart_summon_available")]
		public bool? SmartSummonAvailable { get; set; }

		[JsonProperty ("software_update")]
		public SoftwareUpdate SoftwareUpdate { get; set; }

		[JsonProperty ("speed_limit_mode")]
		public SpeedLimitMode SpeedLimitMode { get; set; }

		[JsonProperty ("summon_standby_mode_enabled")]
		public bool? SummonStandbyModeEnabled { get; set; }

		[JsonProperty ("sun_roof_percent_open")]
		public int SunRoofPercentOpen { get; set; }

		[JsonProperty ("sun_roof_state")]
		public string SunRoofState { get; set; }

		[JsonProperty ("timestamp")]
		public long Timestamp { get; set; }

		[JsonProperty ("valet_mode")]
		public bool? ValetMode { get; set; }

		[JsonProperty ("valet_pin_needed")]
		public bool? ValetPinNeeded { get; set; }

		[JsonProperty ("vehicle_name")]
		public string VehicleName { get; set; }
	}

	public class VehicleConfig {

		[JsonProperty ("can_accept_navigation_requests")]
		public bool? CanAcceptNavigationRequests { get; set; }

		[JsonProperty ("can_actuate_trunks")]
		public bool? CanActuateTrunks { get; set; }

		[JsonProperty ("car_special_type")]
		public string CarSpecialType { get; set; }

		[JsonProperty ("car_type")]
		public string CarType { get; set; }

		[JsonProperty ("charge_port_type")]
		public string ChargePortType { get; set; }

		[JsonProperty ("eu_vehicle")]
		public bool? EuVehicle { get; set; }

		[JsonProperty ("exterior_color")]
		public string ExteriorColor { get; set; }

		[JsonProperty ("has_air_suspension")]
		public bool? HasAirSuspension { get; set; }

		[JsonProperty ("has_ludicrous_mode")]
		public bool? HasLudicrousMode { get; set; }

		[JsonProperty ("key_version")]
		public int KeyVersion { get; set; }

		[JsonProperty ("motorized_charge_port")]
		public bool? MotorizedChargePort { get; set; }

		[JsonProperty ("perf_config")]
		public string PerfConfig { get; set; }

		[JsonProperty ("plg")]
		public bool? Plg { get; set; }

		[JsonProperty ("rear_seat_heaters")]
		public int RearSeatHeaters { get; set; }

		[JsonProperty ("rear_seat_type")]
		public int RearSeatType { get; set; }

		[JsonProperty ("rhd")]
		public bool? Rhd { get; set; }

		[JsonProperty ("roof_color")]
		public string RoofColor { get; set; }

		[JsonProperty ("seat_type")]
		public int SeatType { get; set; }

		[JsonProperty ("spoiler_type")]
		public string SpoilerType { get; set; }

		[JsonProperty ("sun_roof_installed")]
		public int SunRoofInstalled { get; set; }

		[JsonProperty ("third_row_seats")]
		public string ThirdRowSeats { get; set; }

		[JsonProperty ("timestamp")]
		public long Timestamp { get; set; }

		[JsonProperty ("trim_badging")]
		public string TrimBadging { get; set; }

		[JsonProperty ("use_range_badging")]
		public bool? UseRangeBadging { get; set; }

		[JsonProperty ("wheel_type")]
		public string WheelType { get; set; }
	}

	public class VehicleData {

		[JsonProperty ("id")]
		public long Id { get; set; }

		[JsonProperty ("user_id")]
		public int UserId { get; set; }

		[JsonProperty ("vehicle_id")]
		public int VehicleId { get; set; }

		[JsonProperty ("vin")]
		public string Vin { get; set; }

		[JsonProperty ("display_name")]
		public string DisplayName { get; set; }

		[JsonProperty ("option_codes")]
		public string OptionCodes { get; set; }

		[JsonProperty ("color")]
		public object Color { get; set; }

		[JsonProperty ("tokens")]
		public IList<string> Tokens { get; set; }

		[JsonProperty ("state")]
		public string State { get; set; }

		[JsonProperty ("in_service")]
		public bool? InService { get; set; }

		[JsonProperty ("id_s")]
		public string IdS { get; set; }

		[JsonProperty ("calendar_enabled")]
		public bool? CalendarEnabled { get; set; }

		[JsonProperty ("api_version")]
		public int ApiVersion { get; set; }

		[JsonProperty ("backseat_token")]
		public object BackseatToken { get; set; }

		[JsonProperty ("backseat_token_updated_at")]
		public object BackseatTokenUpdatedAt { get; set; }

		[JsonProperty ("drive_state")]
		public DriveState DriveState { get; set; }

		[JsonProperty ("climate_state")]
		public ClimateState ClimateState { get; set; }

		[JsonProperty ("charge_state")]
		public ChargeState ChargeState { get; set; }

		[JsonProperty ("gui_settings")]
		public GuiSettings GuiSettings { get; set; }

		[JsonProperty ("vehicle_state")]
		public VehicleState VehicleState { get; set; }

		[JsonProperty ("vehicle_config")]
		public VehicleConfig VehicleConfig { get; set; }
	}
}
