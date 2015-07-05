
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using CyclometerInternals;

using Cyclometer;


namespace Cyclometer
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class scenario_CSCMeasurement : Page
    {
        MainPage root = MainPage.current;

        public scenario_CSCMeasurement()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (CSCMeasurementService.Instance.IsServiceInited)
            {
                // KURTZ enable UI
            }
            else
            {
                root.NotifyUser("The asdfasdfsdf initialize the service before writing a Characteristic Value.", // KURTZ useful content
                    NotifyType.statusMessage);
            }
        }
    }
}
