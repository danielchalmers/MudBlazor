using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Issue14134.Ios;

[Register ("SceneDelegate")]
public class SceneDelegate : UIResponder, IUIWindowSceneDelegate {

	[Export ("window")]
	public UIWindow? Window { get; set; }

	[Export ("scene:willConnectToSession:options:")]
	public void WillConnect (UIScene scene, UISceneSession session, UISceneConnectionOptions connectionOptions)
	{
		if (scene is not UIWindowScene windowScene)
			return;

		Window ??= new UIWindow (windowScene);
		var label = new UILabel (Window.Frame) {
			BackgroundColor = UIColor.SystemBackground,
			TextAlignment = UITextAlignment.Center,
			Lines = 0,
			AutoresizingMask = UIViewAutoresizing.All,
		};
		var vc = new UIViewController ();
		vc.View!.AddSubview (label);
		Window.RootViewController = vc;
		Window.MakeKeyAndVisible ();

		// run-sim.sh passes the case via SIMCTL_CHILD_I14134_CASE; a crash aborts the process before "ok" prints.
		var name = Environment.GetEnvironmentVariable ("I14134_CASE");
		if (string.IsNullOrEmpty (name)) {
			label.Text = "Set I14134_CASE to one of:\n" + string.Join ("\n", Cases.Names);
			return;
		}

		_ = RunAsync (name, label);
	}

	static async Task RunAsync (string name, UILabel label)
	{
		// Confirms which mode the job actually ran in: full AOT reports dynamicCode=False.
		Console.WriteLine ($"I14134 runtime {RuntimeInformation.FrameworkDescription} dynamicCode={RuntimeFeature.IsDynamicCodeSupported} compiled={RuntimeFeature.IsDynamicCodeCompiled}");
		Console.WriteLine ($"I14134 start {name}");
		try {
			var value = await Cases.RunAsync (name);
			Console.WriteLine ($"I14134 ok {name} value={value}");
			label.Text = $"{name}: {value}";
			Environment.Exit (0);
		} catch (Exception ex) {
			Console.WriteLine ($"I14134 error {name} {ex}");
			Environment.Exit (1);
		}
	}
}
