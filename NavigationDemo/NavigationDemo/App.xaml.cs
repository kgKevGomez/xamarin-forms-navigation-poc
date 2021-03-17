using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

[assembly: XamlCompilation(XamlCompilationOptions.Compile)]

namespace NavigationDemo
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            
            MainPage = new MainPage();
        }

        protected override void OnStart()
        {
            // Handle when your app starts
            Esri.ArcGISRuntime.ArcGISRuntimeEnvironment.ApiKey = "AAPK027c57f902ad4ec7b4c45376e21e567doJh-Act8exDB4Fdjdtqcbaanm9CMBNkE5tBCmW-xU0rvzKxIgCvqe-ZMeoFj2OQJ";
        }

        protected override void OnSleep()
        {
            // Handle when your app sleeps
        }

        protected override void OnResume()
        {
            // Handle when your app resumes
        }
    }
}