using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage.Streams;

using SDKTemplate;

using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Devices.Enumeration.Pnp;

namespace BluetoothGattHeartRate
{


    public class BLEBaseService
    {

        // A pointer back to the main page.  This is needed if you want to call methods in MainPage such
        // as NotifyUser().
        protected MainPage rootPage = MainPage.Current;

        protected GattDeviceService service;
        protected GattCharacteristic characteristic;
        protected String deviceContainerId;

        public event ValueChangeCompletedHandler ValueChangeCompleted;
        public event DeviceConnectionUpdatedHandler DeviceConnectionUpdated;

        public bool IsServiceInitialized { get; set; }

        public GattDeviceService Service
        {
            get { return service; }
        }

        protected void App_Resuming(object sender, object e)
        {
            // Since the Windows Runtime will close resources to the device when the app is suspended,
            // the device needs to be reinitialized when the app is resumed.
        }

        protected void App_Suspending(object sender, Windows.ApplicationModel.SuspendingEventArgs e)
        {
            IsServiceInitialized = false;

            if (service != null)
            {
                service.Dispose();
                service = null;
            }

            if (characteristic != null)
            {
                characteristic = null;
            }

        }
    }
}
