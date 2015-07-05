

using System;
using System.Collections.Generic;
using Windows.UI.Xaml.Controls;
using Cyclometer;


namespace CyclometerInternals
{
    public partial class MainPage : Page
    {
        public const string APP_NAME = "Cyclometer";

        private List<Scenario> scenarios = new List<Scenario>
        {
            new Scenario() {Title = "CSC Measurements", ClassType = typeof (scenario_CSCMeasurement)}
        };
    }

        public class Scenario
    {
        public string Title { get; set; }

        public Type ClassType { get; set; }

        public override string ToString()
        {
            return Title;
        }
    }

}