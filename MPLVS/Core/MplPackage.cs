//------------------------------------------------------------------------------
// <copyright file="MplPackage.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Threading;
using System.ComponentModel.Design;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.Win32;
using EnvDTE;
using System.Windows.Media;
using System.Globalization;
using System.Linq;

namespace MPL {
  /// <summary>
  /// This is the class that implements the package exposed by this assembly.
  /// </summary>
  /// <remarks>
  /// <para>
  /// The minimum requirement for a class to be considered a valid package for Visual Studio
  /// is to implement the IVsPackage interface and register itself with the shell.
  /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
  /// to do it: it derives from the Package class that provides the implementation of the
  /// IVsPackage interface and uses the registration attributes defined in the framework to
  /// register itself and its components with the shell. These attributes tell the pkgdef creation
  /// utility what data to put into .pkgdef file.
  /// </para>
  /// <para>
  /// To get loaded into VS, the package must be referred by &lt;Asset Type="Microsoft.VisualStudio.VsPackage" ...&gt; in .vsixmanifest file.
  /// </para>
  /// </remarks>
  [Guid(Constants.PackageGuidString)]
  [ProvideAutoLoad(UIContextGuids80.SolutionExists, PackageAutoLoadFlags.BackgroundLoad)]
  [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
  [InstalledProductRegistration(Constants.MPLLanguagePackageNameResourceString,
                                Constants.MPLLanguagePackageDetailsResourceString,
                                Constants.MPLLanguagePackageProductVersionString /*, IconResourceID = 400*/)]
  //[ProvideService(typeof(MplLanguage), ServiceName = "MPL", IsAsyncQueryable = true)]
  [ProvideLanguageService(typeof(MplLanguage),
                          Constants.LanguageName,
                          Constants.MPLLanguageResourceId,
                          EnableLineNumbers = true,
                          ShowDropDownOptions = true,
                          DefaultToInsertSpaces = true,
                          EnableCommenting = true,
                          AutoOutlining = true,
                          MatchBraces = true,
                          MatchBracesAtCaret = true,
                          ShowMatchingBrace = true,
                          ShowSmartIndent = true)]
  [ProvideLanguageEditorOptionPage(typeof(Options), Constants.LanguageName, null, "Advanced", "#101", new[] { "mpl" })]
  [ProvideLanguageExtension(typeof(MplLanguage), Constants.MPLFileExtension)]
  [ProvideLanguageExtension(typeof(MplLanguage), ".smart")]
  [ProvideLanguageExtension(typeof(MplLanguage), ".fast")]
  [ProvideLanguageExtension(typeof(MplLanguage), ".easy")]
  [ProvideEditorFactory(typeof(EditorFactory),
                        110,
                        CommonPhysicalViewAttributes = (int)__VSPHYSICALVIEWATTRIBUTES.PVA_None,
                        TrustLevel = __VSEDITORTRUSTLEVEL.ETL_AlwaysTrusted)]
  [ProvideEditorLogicalView(typeof(EditorFactory), VSConstants.LOGVIEWID.TextView_string, IsTrusted = true)]
  [ProvideEditorExtension(typeof(EditorFactory), Constants.MPLFileExtension, 1000)]
  [ProvideEditorExtension(typeof(EditorFactory), ".smart", 1000)]
  [ProvideEditorExtension(typeof(EditorFactory), ".fast", 1000)]
  [ProvideEditorExtension(typeof(EditorFactory), ".easy", 1000)]
  [ProvideEditorExtension(typeof(EditorFactory), ".*", 2, NameResourceID = 110)]
  public sealed class MplPackage : AsyncPackage {
    private static Options _options;
    private static object _syncRoot = new object();

    public static bool completionSession = false;

    internal LanguageSettings LanguageSettings {
      get;
      private set;
    }

    internal static MplPackage Instance {
      get;
      private set;
    }

    internal static List<Theme> LoadedThemes {
      get;
      private set;
    }

    internal static Color MplContentColor {
      get {
        ThreadHelper.ThrowIfNotOnUIThread();
        if (MplPackage.Options.DarkThemesList.Contains(GetThemeName())) {
          //dark theme
          return Constants.MplContentColorDark;
        } else {
          //light theme
          return Constants.MplContentColorLight;
        }
      }
    }

    internal static Color MplEmphasizedColor {
      get {
        ThreadHelper.ThrowIfNotOnUIThread();
        if (MplPackage.Options.DarkThemesList.Contains(GetThemeName())) {
          //dark theme
          return Constants.MplEmphasizedColorDark;
        } else {
          //light theme
          return Constants.MplEmphasizedColorLight;
        }
      }
    }

    internal static Color MplBraceMatchingColor {
      get {
        ThreadHelper.ThrowIfNotOnUIThread();
        if (MplPackage.Options.DarkThemesList.Contains(GetThemeName())) {
          //dark theme
          return Constants.backgroundHighlightColorLight;
        } else {
          //light theme
          return Constants.backgroundHighlightColorDark;
        }
      }
    }

    public static Options Options {
      get {
        ThreadHelper.ThrowIfNotOnUIThread();
        if (_options == null) {
          lock (_syncRoot) {
            if (_options == null) {
              LoadPackage();
            }
          }
        }

        return _options;
      }
    }

    public static MplLanguage Language {
      get;
      private set;
    }

    protected override async System.Threading.Tasks.Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress) {
      await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

      Instance = this;
      _options = (Options)GetDialogPage(typeof(Options));

      LanguageSettings = new LanguageSettings();

      var serviceContainer = this as IServiceContainer;
      Language = new MplLanguage(this);
      serviceContainer.AddService(typeof(MplLanguage), Language, true);
      //serviceContainer.AddService(typeof(MplLanguage), callback, true);

      var editorFactory = new EditorFactory(this, typeof(MplLanguage).GUID);
      RegisterEditorFactory(editorFactory);

      GetLoadedThemes();
    }

    private static void LoadPackage() {
      ThreadHelper.ThrowIfNotOnUIThread();
      var shell = (IVsShell)GetGlobalService(typeof(SVsShell));

      IVsPackage package;

      if (shell.IsPackageLoaded(ref Constants.PackageGuid, out package) != VSConstants.S_OK)
        ErrorHandler.Succeeded(shell.LoadPackage(ref Constants.PackageGuid, out package));
    }

    private static void GetLoadedThemes() {
      ThreadHelper.ThrowIfNotOnUIThread();
      DTE Dte = (DTE)Package.GetGlobalService(typeof(DTE));

      string registryPath = Dte.RegistryRoot + "_Config\\Themes";
      var themes = new List<Theme>();
      string[] installedThemesKeys;
      RegistryKey themesKey = Registry.CurrentUser.OpenSubKey(registryPath);

      if (themesKey != null) {
        installedThemesKeys = themesKey.GetSubKeyNames();
        foreach (string key in installedThemesKeys) {
          using (RegistryKey themeKey = themesKey.OpenSubKey(key)) {
            if (themeKey != null) {
              themes.Add(new Theme {id = key, name = themeKey.GetValue(null).ToString()});
            }
          }
        }
      }

      LoadedThemes = themes;
    }

    internal static string GetThemeName() {
      ThreadHelper.ThrowIfNotOnUIThread();

      DTE Dte = (DTE)Package.GetGlobalService(typeof(DTE));

      string themeName = "";
      string storedSetting;
      string[] settings;
      string id;
      var themes = MplPackage.LoadedThemes;

      string keyName = string.Format(CultureInfo.InvariantCulture, @"{0}\ApplicationPrivateSettings\Microsoft\VisualStudio", Dte.RegistryRoot);
      RegistryKey regKey = Registry.CurrentUser.OpenSubKey(keyName, true);

      if (regKey != null) {
        storedSetting = (string)regKey.GetValue("ColorTheme", string.Empty);
        if (!string.IsNullOrEmpty(storedSetting)) {
          settings = storedSetting.Split('*');
          if (settings.Length > 2) {
            id = string.Format(CultureInfo.InvariantCulture, "{{{0}}}", settings[2]);
            themeName = themes.FirstOrDefault(t => t.id.Equals(id, StringComparison.OrdinalIgnoreCase)).name;
          }
        }
      }

      return themeName;
    }

    internal struct Theme {
      public string id;
      public string name;
    }
  }
}