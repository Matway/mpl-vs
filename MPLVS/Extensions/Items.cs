using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

using EnvDTE;

using EnvDTE80;

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;

using MPLVS.Core;
using MPLVS.Core.ParseTree;

namespace MPLVS.Extensions {
  public static class Projects {
    private static IVsSolution GetSolution(this IServiceProvider serviceProvider) =>
      serviceProvider.GetService(typeof(SVsSolution)) as IVsSolution;

    public static IEnumerable<IVsProject> GetLoadedProjects(this IServiceProvider serviceProvider) =>
      serviceProvider.GetSolution().GetLoadedProjects();

    public static string GetSolutionDirectory(this IServiceProvider serviceProvider) =>
      serviceProvider.GetSolution().GetSolutionDirectory();

    public static bool HasFile(this IServiceProvider serviceProvider, string file) =>
      serviceProvider.GetLoadedProjects().Any(p => p.HasFile(file));

    public static IEnumerable<IVsProject> GetLoadedProjects(this IVsSolution solution) =>
      solution.EnumerateLoadedProjects(__VSENUMPROJFLAGS.EPF_LOADEDINSOLUTION).OfType<IVsProject>();

    public static string GetProjectName(this IVsProject project) =>
      project.GetPropertyValue(__VSHPROPID.VSHPROPID_Name, VSConstants.VSITEMID.Root) as string;

    public static string GetProjectDir(this IVsProject project) =>
      project.GetPropertyValue(__VSHPROPID.VSHPROPID_ProjectDir, VSConstants.VSITEMID.Root) as string;

    public static IEnumerable<IVsHierarchy> EnumerateLoadedProjects(this IVsSolution solution, __VSENUMPROJFLAGS enumFlags) {
      var prjType = Guid.Empty;

      var hr = solution.GetProjectEnum((uint)enumFlags, ref prjType, out var ppHier);
      if (ErrorHandler.Succeeded(hr) && ppHier != null) {
        uint fetched = 0;
        var hierarchies = new IVsHierarchy[1];
        while (ppHier.Next(1, hierarchies, out fetched) == VSConstants.S_OK) {
          yield return hierarchies[0];
        }
      }
    }

    public static string GetSolutionDirectory(this IVsSolution solution) =>
      solution.GetSolutionInfo(out var solutionDir, out var solutionFile, out var userOpsFile) == VSConstants.S_OK
      ? solutionDir
      : null;

    public static bool HasFile(this IVsSolution solution, string file) =>
      solution.GetLoadedProjects().Any(p => p.HasFile(file));

    public static IEnumerable<string> GetProjectItems(this IVsProject project) =>
      // Each item in VS OM is IVSHierarchy.
      GetProjectItems((IVsHierarchy)project, VSConstants.VSITEMID_ROOT);

    public static IEnumerable<string> GetProjectItems(IVsHierarchy project, uint itemId) {
      var pVar = GetPropertyValue(project, (int)__VSHPROPID.VSHPROPID_FirstChild, itemId);

      var childId = GetItemId(pVar);
      while (childId != VSConstants.VSITEMID_NIL) {
        var childPath = GetCanonicalName(childId, project);
        yield return childPath;

        foreach (var childNodePath in GetProjectItems(project, childId)) { yield return childNodePath; }

        pVar = GetPropertyValue(project, (int)__VSHPROPID.VSHPROPID_NextSibling, childId);
        childId = GetItemId(pVar);
      }
    }

    public static bool HasFile(this IVsProject project, string file) =>
      ErrorHandler.Succeeded(project.IsDocumentInProject(file, out var found, new VSDOCUMENTPRIORITY[1], out var projectItemID))
      ? found != 0
      : false;

    public static uint GetItemId(object pvar) {
      if (pvar == null  ) { return VSConstants.VSITEMID_NIL; }
      if (pvar is int   ) { return (uint)(int)pvar;          }
      if (pvar is uint  ) { return (uint)pvar;               }
      if (pvar is short ) { return (uint)(short)pvar;        }
      if (pvar is ushort) { return (ushort)pvar;             }
      if (pvar is long  ) { return (uint)(long)pvar;         }

      return VSConstants.VSITEMID_NIL;
    }

    public static object GetPropertyValue(this IVsProject project, __VSHPROPID propid, VSConstants.VSITEMID itemId = VSConstants.VSITEMID.Root) =>
      GetPropertyValue((IVsHierarchy)project, propid, itemId);

    public static object GetPropertyValue(this IVsHierarchy vsHierarchy, __VSHPROPID propid, VSConstants.VSITEMID itemId = VSConstants.VSITEMID.Root) =>
      GetPropertyValue(vsHierarchy, (int)propid, (uint)itemId);

    public static object GetPropertyValue(this IVsHierarchy vsHierarchy, int propid, uint itemId) {
      if (itemId == VSConstants.VSITEMID_NIL) {
        return null;
      }

      try {
        ErrorHandler.ThrowOnFailure(vsHierarchy.GetProperty(itemId, propid, out var o));

        return o;
      }
      catch (System.NotImplementedException) {
        return null;
      }
      catch (System.Runtime.InteropServices.COMException) {
        return null;
      }
      catch (System.ArgumentException) {
        return null;
      }
    }

    public static string GetCanonicalName(uint itemId, IVsHierarchy hierarchy) {
      var strRet = string.Empty;
      var hr     = hierarchy.GetCanonicalName(itemId, out strRet);

      if (hr == VSConstants.E_NOTIMPL) {
        // Special case E_NOTIMLP to avoid perf hit to throw an exception.
        return string.Empty;
      }
      else {
        try {
          ErrorHandler.ThrowOnFailure(hr);
        }
        catch (System.Runtime.InteropServices.COMException) {
          strRet = string.Empty;
        }

        // This could be in the case of S_OK, S_FALSE, etc.
        return strRet;
      }
    }
  }

  public static class Files {
    /// <summary>
    /// Get the filename from the given text buffer
    /// </summary>
    /// <param name="buffer">The text buffer from which to get the filename</param>
    /// <returns>The filename or null if it could not be obtained</returns>
    public static string GetFileName(this ITextBuffer buffer) {
      ThreadHelper.ThrowIfNotOnUIThread();

      if (buffer != null) {
        // Most files have an ITextDocument property
        if (buffer.Properties.TryGetProperty(typeof(ITextDocument), out ITextDocument textDoc)) {
          if (textDoc != null && !string.IsNullOrEmpty(textDoc.FilePath)) {
            return textDoc.FilePath;
          }
        }

        // TODO: Check if we need this for mpl-files.
        if (buffer.Properties.TryGetProperty(typeof(IVsTextBuffer), out IVsTextBuffer vsTextBuffer)) {
          // Some, like HTML files, don't so we go through the IVsTextBuffer to get it
          if (vsTextBuffer != null) {
            if (vsTextBuffer is IPersistFileFormat persistFileFormat) {
              try {
                persistFileFormat.GetCurFile(out var ppzsFilename, out var _);

                if (!string.IsNullOrEmpty(ppzsFilename)) {
                  return ppzsFilename;
                }
              }
              catch (NotImplementedException) {
                // Secondary buffers throw an exception rather than returning E_NOTIMPL so we'll
                // ignore these. They are typically used for inline CSS, script, etc. and can be
                // safely ignored as they're part of a primary buffer that does have a filename.
                System.Diagnostics.Debug.WriteLine("Unable to obtain filename, probably a secondary buffer");

                return null;
              }
            }
          }
        }
      }

      return null;
    }

    /// <summary>
    /// This is used to get an <see cref="IVsTextView"/> reference for the given document
    /// </summary>
    /// <param name="filename">The filename for which to get a text view reference</param>
    /// <param name="position">The initial position at which to place the cursor or -1 to leave it at the
    /// top of the file.</param>
    /// <param name="selectionLength">The length of text to select a <paramref name="position"/> or -1 to
    /// not select any text at that location.</param>
    /// <returns>Returns the text view if the document could be opened in a text editor instance or was
    /// already open in one.  Returns null if the reference could not be obtained.</returns>
    public static IVsTextView GetTextViewForDocument(string filename, int position, int selectionLength) {
      IVsTextView textView = null;
      var frame = OpenTextEditorForFile(filename);

      if (frame != null) {
        textView = VsShellUtilities.GetTextView(frame);

        if (textView != null && position != -1) {
          int topLine;

          if (textView.GetLineAndColumn(position, out var startLine, out var startColumn) == VSConstants.S_OK
              && textView.GetLineAndColumn(position + selectionLength, out var endLine, out var endColumn) == VSConstants.S_OK
              && textView.SetCaretPos(startLine, startColumn) == VSConstants.S_OK) {
            if (selectionLength != -1) {
              textView.SetSelection(startLine, startColumn, endLine, endColumn);
            }

            // Ensure some surrounding lines are visible so that it's not right at the top
            // or bottom of the view.
            topLine = startLine - 5;

            if (topLine < 0) { topLine = 0; }

            textView.EnsureSpanVisible(new TextSpan {
              iStartLine = topLine,
              iStartIndex = startColumn,
              iEndLine = endLine + 5,
              iEndIndex = endColumn
            });
          }
          else {
            textView = null;
          }
        }
      }

      return textView;
    }

    /// <summary>
    /// This is used to open the given file in a text editor if possible
    /// </summary>
    /// <param name="filename">The filename for which to open a text editor</param>
    /// <returns>The window frame reference if successful, null if not</returns>
    public static IVsWindowFrame OpenTextEditorForFile(string filename) {
      ThreadHelper.ThrowIfNotOnUIThread();

      IVsWindowFrame ppWindowFrame = null;
      var openDoc = MplPackage.GetGlobalService(typeof(SVsUIShellOpenDocument)) as IVsUIShellOpenDocument;
      if (openDoc != null && openDoc.OpenDocumentViaProject(filename, VSConstants.LOGVIEWID_TextView,
        out _, out _, out _, out ppWindowFrame) == VSConstants.S_OK) {
        // On occasion, the call above is successful but we get a null frame for some reason
        if (ppWindowFrame != null) {
          if (ppWindowFrame.Show() != VSConstants.S_OK) {
            ppWindowFrame = null;
          }
        }
      }

      return ppWindowFrame;
    }
  }

  public static class Views {
    public static bool IsCaretInStringOrComment(this ITextView view) {
      var point  = view.Caret.Position.BufferPosition.Position;
      var symbol = view.TextBuffer.ObtainOrAttachTree().Root().YongestAncestor(point);

      // FIXME: Get rid of this.
      // When caret at the end of buffer, then the last tree's element will be returned,
      // which is always will be`EOF`.
      // So, here we workaround this.
      var current =
        symbol is object && symbol.IsEof()
        ? view.TextBuffer.ObtainOrAttachTree().Root().YongestAncestor(Math.Max(point, 1) - 1)
        : symbol;

      return
        current is object
        && point > current.begin
        && (current.name == "String" || current.name == "Comment");
    }
  }

  // Provides methods for processing file system strings in a cross-platform manner.
  // Most of the methods don't do a complete parsing (such as examining a UNC hostname),
  // but they will handle most string operations.
  // See - https://stackoverflow.com/questions/275689/how-to-get-relative-path-from-absolute-path.
  public static class PathNetCore {
    /// <summary>
    /// Create a relative path from one path to another. Paths will be resolved before calculating the difference.
    /// Default path comparison for the active platform will be used (OrdinalIgnoreCase for Windows or Mac, Ordinal for Unix).
    /// </summary>
    /// <param name="relativeTo">The source path the output should be relative to. This path is always considered to be a directory.</param>
    /// <param name="path">The destination path.</param>
    /// <returns>The relative path or <paramref name="path"/> if the paths don't share the same root.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="relativeTo"/> or <paramref name="path"/> is <c>null</c> or an empty string.</exception>
    public static string GetRelativePath(string relativeTo, string path) =>
      GetRelativePath(relativeTo, path, StringComparison);

    private static string GetRelativePath(string relativeTo, string path, StringComparison comparisonType) {
      if (string.IsNullOrEmpty(relativeTo)) { throw new ArgumentNullException(nameof(relativeTo)); }
      if (string.IsNullOrEmpty(path)) { throw new ArgumentNullException(nameof(path)); }

      Debug.Assert(comparisonType == StringComparison.Ordinal
                   || comparisonType == StringComparison.OrdinalIgnoreCase);

      relativeTo = Path.GetFullPath(relativeTo);
      path       = Path.GetFullPath(path);

      // Need to check if the roots are different- if they are we need to return the "to" path.
      if (!PathInternalNetCore.AreRootsEqual(relativeTo, path, comparisonType)) {
        return path;
      }

      var commonLength = PathInternalNetCore.GetCommonPathLength(
        relativeTo, path, ignoreCase: comparisonType == StringComparison.OrdinalIgnoreCase
      );

      // If there is nothing in common they can't share the same root, return the "to" path as is.
      if (commonLength == 0) {
        return path;
      }

      // Trailing separators aren't significant for comparison
      var relativeToLength = relativeTo.Length;
      if (PathInternalNetCore.EndsInDirectorySeparator(relativeTo)) {
        relativeToLength--;
      }

      var pathEndsInSeparator = PathInternalNetCore.EndsInDirectorySeparator(path);
      var pathLength          = path.Length;
      if (pathEndsInSeparator) {
        pathLength--;
      }

      // If we have effectively the same path, return "."
      if (relativeToLength == pathLength && commonLength >= relativeToLength) {
        return ".";
      }

      // We have the same root, we need to calculate the difference now using the
      // common Length and Segment count past the length.
      //
      // Some examples:
      //
      //  C:\Foo C:\Bar L3, S1 -> ..\Bar
      //  C:\Foo C:\Foo\Bar L6, S0 -> Bar
      //  C:\Foo\Bar C:\Bar\Bar L3, S2 -> ..\..\Bar\Bar
      //  C:\Foo\Foo C:\Foo\Bar L7, S1 -> ..\Bar

      var sb = new StringBuilder(); //StringBuilderCache.Acquire(Math.Max(relativeTo.Length, path.Length));

      // Add parent segments for segments past the common on the "from" path
      if (commonLength < relativeToLength) {
        sb.Append("..");

        for (var i = commonLength + 1; i < relativeToLength; i++) {
          if (PathInternalNetCore.IsDirectorySeparator(relativeTo[i])) {
            sb.Append(DirectorySeparatorChar);
            sb.Append("..");
          }
        }
      }
      else if (PathInternalNetCore.IsDirectorySeparator(path[commonLength])) {
        // No parent segments and we need to eat the initial separator
        //  (C:\Foo C:\Foo\Bar case)
        commonLength++;
      }

      // Now add the rest of the "to" path, adding back the trailing separator
      var differenceLength = pathLength - commonLength;
      if (pathEndsInSeparator) { differenceLength++; }

      if (differenceLength > 0) {
        if (sb.Length > 0) {
          sb.Append(DirectorySeparatorChar);
        }

        sb.Append(path, commonLength, differenceLength);
      }

      return sb.ToString(); //StringBuilderCache.GetStringAndRelease(sb);
    }

    // Public static readonly variant of the separators. The Path implementation itself is using
    // internal const variant of the separators for better performance.
    public static readonly char DirectorySeparatorChar    = PathInternalNetCore.DirectorySeparatorChar;
    public static readonly char AltDirectorySeparatorChar = PathInternalNetCore.AltDirectorySeparatorChar;
    public static readonly char VolumeSeparatorChar       = PathInternalNetCore.VolumeSeparatorChar;
    public static readonly char PathSeparator             = PathInternalNetCore.PathSeparator;

    /// <summary>Returns a comparison that can be used to compare file and directory names for equality.</summary>
    internal static StringComparison StringComparison => StringComparison.OrdinalIgnoreCase;

    internal static class PathInternalNetCore {
      internal const char DirectorySeparatorChar    = '\\';
      internal const char AltDirectorySeparatorChar = '/';
      internal const char VolumeSeparatorChar       = ':';
      internal const char PathSeparator             = ';';

      internal const string ExtendedDevicePathPrefix = @"\\?\";
      internal const string UncPathPrefix            = @"\\";
      internal const string UncDevicePrefixToInsert  = @"?\UNC\";
      internal const string UncExtendedPathPrefix    = @"\\?\UNC\";
      internal const string DevicePathPrefix         = @"\\.\";

      //internal const int MaxShortPath = 260;

      // \\?\, \\.\, \??\
      internal const int DevicePrefixLength = 4;

      /// <summary>
      /// Returns true if the two paths have the same root
      /// </summary>
      internal static bool AreRootsEqual(string first, string second, StringComparison comparisonType) {
        var firstRootLength  = GetRootLength(first);
        var secondRootLength = GetRootLength(second);

        return
          firstRootLength == secondRootLength &&
          string.Compare(
            strA: first,
            indexA: 0,
            strB: second,
            indexB: 0,
            length: firstRootLength,
            comparisonType: comparisonType
          ) == 0;
      }

      /// <summary>
      /// Gets the length of the root of the path (drive, share, etc.).
      /// </summary>
      internal static int GetRootLength(string path) {
        var i                     = 0;
        var volumeSeparatorLength = 2; // Length to the colon "C:"
        var uncRootLength         = 2; // Length to the start of the server name "\\"

        var extendedSyntax    = path.StartsWith(ExtendedDevicePathPrefix);
        var extendedUncSyntax = path.StartsWith(UncExtendedPathPrefix);
        if (extendedSyntax) {
          // Shift the position we look for the root from to account for the extended prefix
          if (extendedUncSyntax) {
            // "\\" -> "\\?\UNC\"
            uncRootLength = UncExtendedPathPrefix.Length;
          }
          else {
            // "C:" -> "\\?\C:"
            volumeSeparatorLength += ExtendedDevicePathPrefix.Length;
          }
        }

        if ((!extendedSyntax || extendedUncSyntax) && path.Length > 0 && IsDirectorySeparator(path[0])) {
          // UNC or simple rooted path (e.g. "\foo", NOT "\\?\C:\foo")

          i = 1; //  Drive rooted (\foo) is one character
          if (extendedUncSyntax || (path.Length > 1 && IsDirectorySeparator(path[1]))) {
            // UNC (\\?\UNC\ or \\), scan past the next two directory separators at most
            // (e.g. to \\?\UNC\Server\Share or \\Server\Share\)
            i     = uncRootLength;
            var n = 2; // Maximum separators to skip
            while (i < path.Length && (!IsDirectorySeparator(path[i]) || --n > 0)) {
              i++;
            }
          }
        }
        else if (path.Length >= volumeSeparatorLength && path[volumeSeparatorLength - 1] == PathNetCore.VolumeSeparatorChar) {
          // Path is at least longer than where we expect a colon, and has a colon (\\?\A:, A:)
          // If the colon is followed by a directory separator, move past it
          i = volumeSeparatorLength;
          if (path.Length >= volumeSeparatorLength + 1 && IsDirectorySeparator(path[volumeSeparatorLength])) {
            i++;
          }
        }

        return i;
      }

      /// <summary>
      /// True if the given character is a directory separator.
      /// </summary>
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      internal static bool IsDirectorySeparator(char c) =>
        c == PathNetCore.DirectorySeparatorChar || c == PathNetCore.AltDirectorySeparatorChar;

      /// <summary>
      /// Get the common path length from the start of the string.
      /// </summary>
      internal static int GetCommonPathLength(string first, string second, bool ignoreCase) {
        var commonChars = EqualStartingCharacterCount(first, second, ignoreCase: ignoreCase);

        // If nothing matches
        if (commonChars == 0) { return commonChars; }

        // Or we're a full string and equal length or match to a separator
        if (commonChars == first.Length && (commonChars == second.Length || IsDirectorySeparator(second[commonChars]))) {
          return commonChars;
        }

        if (commonChars == second.Length && IsDirectorySeparator(first[commonChars])) {
          return commonChars;
        }

        // It's possible we matched somewhere in the middle of a segment e.g. C:\Foodie and C:\Foobar.
        while (commonChars > 0 && !IsDirectorySeparator(first[commonChars - 1])) {
          commonChars--;
        }

        return commonChars;
      }

      /// <summary>
      /// Gets the count of common characters from the left optionally ignoring case
      /// </summary>
      internal static unsafe int EqualStartingCharacterCount(string first, string second, bool ignoreCase) {
        if (string.IsNullOrEmpty(first) || string.IsNullOrEmpty(second)) { return 0; }

        var commonChars = 0;

        fixed (char* f = first)
        fixed (char* s = second) {
          var l        = f;
          var r        = s;
          var leftEnd  = l + first.Length;
          var rightEnd = r + second.Length;

          while (l != leftEnd
                 && r != rightEnd
                 && (*l == *r || (ignoreCase && char.ToUpperInvariant(*l) == char.ToUpperInvariant(*r)))) {
            commonChars++;
            l++;
            r++;
          }
        }

        return commonChars;
      }

      /// <summary>
      /// Returns true if the path ends in a directory separator.
      /// </summary>
      internal static bool EndsInDirectorySeparator(string path) =>
        path.Length > 0 && IsDirectorySeparator(path[path.Length - 1]);
    }
  }

  public static class Output {
    static Output() {
      ThreadHelper.ThrowIfNotOnUIThread();

      var window = Package.GetGlobalService(typeof(SVsOutputWindow)) as IVsOutputWindow;

      var paneGuid = VSConstants.GUID_OutWindowGeneralPane;
      if (window != null) {
        window.GetPane(ref paneGuid, out Pane);

        if (Pane == null) {
          window.CreatePane(paneGuid, "MPLVS", 1, 1);
          window.GetPane(ref paneGuid, out Pane);

          Debug.Assert(Pane != null);

          Window = new OutputWindow();
          Window.Initialize(MplPackage.Instance);
        }
      }
    }

    public static void Activate() {
      Window.Activate();
      Pane.Activate();
    }

    public static readonly OutputWindow Window;
    public static readonly IVsOutputWindowPane Pane;

    public class OutputWindow {
      private DTE2 _dte2;
      private int _initialized;

      public void Initialize(IServiceProvider serviceProvider) {
        if (Interlocked.CompareExchange(ref _initialized, 1, 0) == 1) { return; }

        _dte2 = serviceProvider.GetService(typeof(DTE)) as DTE2;
      }

      public void Activate() {
        if (_initialized != 1) { return; }

        _dte2.ToolWindows.OutputWindow.Parent.Activate();
        foreach (OutputWindowPane pane in _dte2.ToolWindows.OutputWindow.OutputWindowPanes) {
          if (pane.Guid == VSConstants.OutputWindowPaneGuid.DebugPane_string) {
            pane.Activate();
            break;
          }
        }
      }
    }
  }
}