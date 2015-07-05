

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Devices.Enumeration.Pnp;
using Windows.Storage.Streams;

using CyclometerInternals;

namespace Cyclometer
{
    public class CSCMeasurement
    {
        public ushort heartRateValue { get; set; }
        public bool hasExpendedEnergy { get; set; }
        public ushort expendedEnergy { get; set; }
        public DateTimeOffset timestamp { get; set; }

        public override string ToString()
        {
            return "asdfasdfasdf"; // KURTZ something useful. 
        }
    }

    public delegate void ValueChangeCompletedHandler(CSCMeasurement cscMeaturementValue);

    public delegate void DeviceConnectionUpdatedHandler(bool isConnected);


    public class CSCMeasurementService
    {
        private Guid CHARACTERISTIC_UUID = GattCharacteristicUuids.CscMeasurement;
        private const int CHARACTERISTIC_INDEX = 0;
        private const GattClientCharacteristicConfigurationDescriptorValue CHARACTERISTIC_NOTIFICATION_TYPE = GattClientCharacteristicConfigurationDescriptorValue.Notify;

        MainPage root = MainPage.current;

        private static CSCMeasurementService instance = new CSCMeasurementService();
        private GattDeviceService service;
        private GattCharacteristic characteristic;
        private List<CSCMeasurement> dataPoints;
        private PnpObjectWatcher watcher;
        private String deviceContainerId;

        public event ValueChangeCompletedHandler ValueChangeCompleted;
        public event DeviceConnectionUpdatedHandler DeviceConnectionUpdated;

        public static CSCMeasurementService Instance
        {
            get { return instance; }
        }

        public bool IsServiceInited { get; set; }

        public GattDeviceService Service
        {
            get { return service; }
        }

        public CSCMeasurement[] DataPoints
        {
            get
            {
                CSCMeasurement[] r;
                lock (dataPoints)
                {
                    r = dataPoints.ToArray();
                }
                return r;
            }
        }

        private CSCMeasurementService()
        {
            dataPoints = new List<CSCMeasurement>();
            App.Current.Suspending += appSuspending;
            App.Current.Resuming += appResuming;
        }

        private void appResuming(object sender, object e)
        {
            // KURTZ re-open data (See suspending method below)
            // KURTZ since connections are lost on suspend, they'll need to be re-establisheed at resume (here)
        }

        private void appSuspending(object sender, Windows.ApplicationModel.SuspendingEventArgs e)
        {
            IsServiceInited = false;

            // KURTZ save data here!

            dataPoints.Clear();

            if (service != null)
            {
                service.Dispose();
                service = null;
            }

            if (characteristic != null)
            {
                characteristic = null;
            }

            if (watcher != null)
            {
                watcher.Stop();
                watcher = null;
            }

        }

        public async Task initServiceAsyc(DeviceInformation device)
        {
            try
            {
                deviceContainerId = "{" + device.Properties["System.Devices.ContainerId"] + "}";

                service = await GattDeviceService.FromIdAsync(device.Id);
                if (service != null)
                {
                    IsServiceInited = true;
                    await ConfigureServiceForNotificationsAsync();
                }
                else
                {
                    root.notifyUser(
                        "Couldn't connect to the Cycling Speed & Cadence device, because it is either being used by another app, or you didn't give me permission.", // KURTZ localize
                        NotifyType.StatusMessage); 

                }
            }
            catch (Exception e)
            {
                root.NotifyUser(
                    "Couldn't connect to your device.  This particular error doesn't happen very often.  Send us an error report with the following information: " + // KURTZ localize
                    Environment.NewLine + e.Message,
                    NotifyType.ErrorMessage);
            }
        }

        private async Task ConfigureServiceForNotificationsAsync()
        {
            try
            {
                characteristic = service.GetCharacteristics(CHARACTERISTIC_UUID)[CHARACTERISTIC_INDEX];

                // Encryption, if it's supported.
                characteristic.ProtectionLevel = GattProtectionLevel.EncryptionRequired;

                characteristic.ValueChanged += Characteristic_ValueChanged;

                var currentDescriptorValue = await characteristic.ReadClientCharacteristicConfigurationDescriptorAsync();

                if ((currentDescriptorValue.Status != GattCommunicationStatus.Success) ||
                    (currentDescriptorValue.ClientCharacteristicConfigurationDescriptor !=
                     CHARACTERISTIC_NOTIFICATION_TYPE))
                {
                    GattCommunicationStatus status =
                        await
                            characteristic.WriteClientCharacteristicConfigurationDescriptorAsync(
                                CHARACTERISTIC_NOTIFICATION_TYPE);

                    if (status == GattCommunicationStatus.Unreachable)
                    {
                        startDeviceConnectionWatcher();
                    }
                }
            }
            catch (Exception e)
            {
                root.NotifyUser("Couldn't connect to your device.  This particular error doesn't happen very often.  Send us an error report with the following information: " + // KURTZ localize
                    Environment.NewLine + e.Message,
                    NotifyType.ErrorMessage);
            }
        }

        private void startDeviceConnectionWatcher()
        {
            watcher = PnpObject.CreateWatcher(
                PnpObjectType.DeviceContainer, new string[] {"System.Devices.Connected"},
                String.Empty);
            watcher.Updated += DeviceConnection_Updated;
            watcher.Start();
        }


        private async void DeviceConnection_Updated(PnpObjectWatcher sender, PnpObjectUpdate args)
        {
            var conenctedProperty = args.Properties["System.Devices.Connected"];
            bool isConnected = false;
            if ((deviceContainerId == args.Id) && Boolean.TryParse(conenctedProperty.ToString(), out isConnected) &&
                isConnected)
            {
                var status =
                    await
                        characteristic.WriteClientCharacteristicConfigurationDescriptorAsync(
                            CHARACTERISTIC_NOTIFICATION_TYPE);
                if (status == GattCommunicationStatus.Success)
                {
                    IsServiceInited = true;

                    watcher.Stop();
                    watcher = null;
                }

                if (DeviceConnectionUpdated != null)
                {
                    DeviceConnectionUpdated(isConnected);
                }
            }
        }


        private void Characteristic_ValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            var data = new byte[args.CharacteristicValue.Length];

            DataReader.FromBuffer(args.CharacteristicValue).ReadBytes(data);

            var value = ProcessData(data);
            value.timestamp = args.Timestamp;

            lock (dataPoints)
            {
                dataPoints.Add(value);
            }

            if (ValueChangeCompleted != null)
            {
                ValueChangeCompleted(value);
            }
        }


        private CSCMeasurement ProcessData(byte[] data)
        {
            // Heart Rate profile defined flag values // KURTZ correct for CSC schema
            const byte HEART_RATE_VALUE_FORMAT = 0x01;
            const byte ENERGY_EXPANDED_STATUS = 0x08;

            byte currentOffset = 0;
            byte flags = data[currentOffset];
            bool isHeartRateValueSizeLong = ((flags & HEART_RATE_VALUE_FORMAT) != 0);
            bool hasEnergyExpended = ((flags & ENERGY_EXPANDED_STATUS) != 0);

            currentOffset++;

            ushort heartRateMeasurementValue = 0;

            if (isHeartRateValueSizeLong)
            {
                heartRateMeasurementValue = (ushort)((data[currentOffset + 1] << 8) + data[currentOffset]);
                currentOffset += 2;
            }
            else
            {
                heartRateMeasurementValue = data[currentOffset];
                currentOffset++;
            }

            ushort expendedEnergyValue = 0;

            if (hasEnergyExpended)
            {
                expendedEnergyValue = (ushort)((data[currentOffset + 1] << 8) + data[currentOffset]);
                currentOffset += 2;
            }

            // The Heart Rate Bluetooth profile can also contain sensor contact status information,
            // and R-Wave interval measurements, which can also be processed here. 
            // For the purpose of this sample, we don't need to interpret that data.

            return new CSCMeasurement
            {
                heartRateValue = heartRateMeasurementValue,
                hasExpendedEnergy = hasEnergyExpended,
                expendedEnergy = expendedEnergyValue
            };
        }
    }


}