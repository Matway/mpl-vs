using System;
using System.ComponentModel.Design;
using System.Runtime.InteropServices;
using System.Threading;

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace MPLVS {
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
                          EnableLineNumbers     = true,
                          ShowDropDownOptions   = true,
                          ShowCompletion        = true,
                          DefaultToInsertSpaces = true,
                          EnableCommenting      = true,
                          AutoOutlining         = true,
                          MatchBraces           = true,
                          MatchBracesAtCaret    = true,
                          ShowMatchingBrace     = false,
                          ShowSmartIndent       = true)]
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
    public static bool completionSession = false; // Get rid of this.

    private static readonly object syncRoot = new object();
    private static MplPackage instance;
    private static Options options;

    internal LanguageSettings LanguageSettings { get; private set; }

    public static Options Options {
      get {
        ThreadHelper.ThrowIfNotOnUIThread();

        lock (syncRoot) { if (options is null) { options = LoadPackage(); } }
        return options;
      }
    }

    internal static MplPackage Instance {
      get {
        ThreadHelper.ThrowIfNotOnUIThread();

        lock (syncRoot) { if (instance is null) { options = LoadPackage(); } }
        return instance;
      }
    }

    public static MplLanguage Language { get; private set; }

    protected override async System.Threading.Tasks.Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress) {
      await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

      instance = this;

      LanguageSettings = new LanguageSettings();

      var serviceContainer = this as IServiceContainer;
      Language = new MplLanguage(this);
      serviceContainer.AddService(typeof(MplLanguage), Language, true);
      //serviceContainer.AddService(typeof(MplLanguage), callback, true);

      var editorFactory = new EditorFactory(this, typeof(MplLanguage).GUID);
      RegisterEditorFactory(editorFactory);
    }

    private static Options LoadPackage() {
      ThreadHelper.ThrowIfNotOnUIThread();

      var shell = (IVsShell)GetGlobalService(typeof(SVsShell));
      if (shell.IsPackageLoaded(ref Constants.PackageGuid, out var package) != VSConstants.S_OK) {
        ErrorHandler.Succeeded(shell.LoadPackage(ref Constants.PackageGuid, out package));
      }

      // We will not check the package against null, because there is no another way to get an object at this time.
      // So if it's failed then we cannot do something useful anyway.
      return (package as MplPackage).GetDialogPage(typeof(Options)) as Options;
    }
  }
}