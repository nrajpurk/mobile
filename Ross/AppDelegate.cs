using System;
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using PixateFreestyleLib;
using Toggl.Phoebe;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Net;
using XPlatUtils;
using Toggl.Ross.Data;
using Toggl.Ross.ViewControllers;

namespace Toggl.Ross
{
    // The UIApplicationDelegate for the application. This class is responsible for launching the
    // User Interface of the application, as well as listening (and optionally responding) to
    // application events from iOS.
    [Register ("AppDelegate")]
    public partial class AppDelegate : UIApplicationDelegate, IPlatformInfo
    {
        private UIWindow window;

        public override bool FinishedLaunching (UIApplication app, NSDictionary options)
        {
            RegisterComponents ();

            #if DEBUG
            SetupDevelopmentStyles ();
            #endif

            // Start app
            window = new UIWindow (UIScreen.MainScreen.Bounds);
            window.SetStyleMode (PXStylingMode.PXStylingNormal);
            window.RootViewController = new AppViewController ();
            window.MakeKeyAndVisible ();
            
            return true;
        }

        private void RegisterComponents ()
        {
            // Register common Phoebe components:
            ServiceContainer.Register<MessageBus> ();
            ServiceContainer.Register<Logger> ();
            ServiceContainer.Register<ModelManager> ();
            ServiceContainer.Register<AuthManager> ();
            ServiceContainer.Register<SyncManager> ();
            ServiceContainer.Register<ITogglClient> (() => new TogglRestClient (Build.ApiUrl));
            ServiceContainer.Register<IPushClient> (() => new PushRestClient (Build.ApiUrl));

            // Register Ross components:
            ServiceContainer.Register<IPlatformInfo> (this);
            ServiceContainer.Register<SettingsStore> ();
            ServiceContainer.Register<ISettingsStore> (() => ServiceContainer.Resolve<SettingsStore> ());
            ServiceContainer.Register<IModelStore> (delegate {
                string folder = Environment.GetFolderPath (Environment.SpecialFolder.Personal);
                var path = System.IO.Path.Combine (folder, "toggl.db");
                return new SQLiteModelStore (path);
            });
        }

        private void SetupDevelopmentStyles ()
        {
            // You should create a symbolic link of the Resources directory to /tmp/ross-resources
            var cssPath = "/tmp/ross-resources/default.css";
            if (System.IO.File.Exists (cssPath)) {
                PixateFreestyle.StyleSheetFromFilePathWithOrigin (cssPath, PXStylesheetOrigin.PXStylesheetOriginApplication);
            }

            var stylesheet = PixateFreestyle.CurrentApplicationStylesheet ();
            stylesheet.MonitorChanges = true;
            Console.WriteLine ("Monitoring {0} for changes...", stylesheet.FilePath);
        }

        string IPlatformInfo.AppIdentifier {
            get { return Build.AppIdentifier; }
        }

        private string appVersion;

        string IPlatformInfo.AppVersion {
            get {
                if (appVersion == null) {
                    appVersion = NSBundle.MainBundle.InfoDictionary.ObjectForKey (
                        new NSString ("CFBundleVersion")).ToString ();
                }
                return appVersion;
            }
        }
    }
}

