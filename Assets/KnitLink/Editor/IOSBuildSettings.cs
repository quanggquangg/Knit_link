// iOS build settings for Knit Link (27/10).
//   1) Before an iOS build: settings that must be right for the game (portrait only, full screen, no status bar, 60 fps-friendly).
//   2) After an iOS build: edits the Xcode project's Info.plist so App Store Connect does not ask about encryption on every upload
//      (the game uses no encryption of its own: ITSAppUsesNonExemptEncryption = false).
// Building for iOS on Windows makes an Xcode project (Builds/iOS); the app itself is built and signed on a Mac with Xcode.
// Still to set by hand when the team has them: Bundle Identifier and Apple Developer Team ID (Project Settings > Player > iOS).
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace KnitLink.EditorTools
{
    public class IOSBuildSettings : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.iOS) return;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.iOS.requiresFullScreen = true;   // no iPad split view: the layout is one portrait column
            PlayerSettings.statusBarHidden = true;          // the game uses the whole screen (the notch is handled by Screen.safeArea)
            if (string.IsNullOrEmpty(PlayerSettings.iOS.appleDeveloperTeamID))
                Debug.LogWarning("[Build] iOS: no Apple Developer Team ID set - choose the team in Xcode when signing (Project Settings > Player > iOS > Signing).");
            if (PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.iOS).Contains("DefaultCompany"))
                Debug.LogWarning("[Build] iOS: the Bundle Identifier is still Unity's default (com.DefaultCompany...) - set the studio's own before publishing.");
        }

        public void OnPostprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.iOS) return;
            // plain text edit of the plist (no #if UNITY_IOS: that define is not set yet when Unity switches to iOS inside the build)
            string plistPath = System.IO.Path.Combine(report.summary.outputPath, "Info.plist");
            if (!System.IO.File.Exists(plistPath)) { Debug.LogWarning("[Build] iOS: Info.plist not found at " + plistPath); return; }
            string text = System.IO.File.ReadAllText(plistPath);
            if (!text.Contains("ITSAppUsesNonExemptEncryption"))
            {
                int end = text.LastIndexOf("</dict>");
                text = text.Insert(end, "  <key>ITSAppUsesNonExemptEncryption</key>\n    <false/>\n  ");
                System.IO.File.WriteAllText(plistPath, text);
            }
            Debug.Log("[Build] iOS: Info.plist has ITSAppUsesNonExemptEncryption = false");
        }
    }
}
