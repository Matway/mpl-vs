/* ****************************************************************************
 *
 * Copyright (c) Microsoft Corporation.
 *
 * This source code is subject to terms and conditions of the Apache License, Version 2.0. A
 * copy of the license can be found in the License.html file at the root of this distribution. If
 * you cannot locate the Apache License, Version 2.0, please send an email to
 * vspython@microsoft.com. By using this source code in any fashion, you are agreeing to be bound
 * by the terms of the Apache License, Version 2.0.
 *
 * You must not remove this notice, or any other, from this software.
 *
 * This source code has been modified from its original form.
 *
 * ***************************************************************************/

using System;
using System.Runtime.InteropServices;

using Microsoft;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;

using IServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;

namespace MPLVS {
  using IOleServiceProvider = IServiceProvider;

  //About IVsEditorFactory.
  //You need it to make a full implementation of a language service.
  //If all you want to do is provide syntax highlighting and Intellisense, then you don't need it.

  [Guid(Constants.guidMplEditorString)]
  public class EditorFactory : IVsEditorFactory {
    private readonly Guid _languageServiceId;

    public EditorFactory(Package package, Guid languageServiceId) {
      Package            = package;
      _languageServiceId = languageServiceId;
    }

    protected Package Package { get; }

    protected ServiceProvider ServiceProvider { get; private set; }

    protected static bool PromptEncodingOnLoad => false;

    public virtual int SetSite(IOleServiceProvider psp) {
      ServiceProvider = new ServiceProvider(psp);
      return VSConstants.S_OK;
    }

    // This method is called by the Environment (inside IVsUIShellOpenDocument::
    // OpenStandardEditor and OpenSpecificEditor) to map a LOGICAL view to a
    // PHYSICAL view. A LOGICAL view identifies the purpose of the view that is
    // desired (e.g. a view appropriate for Debugging [LOGVIEWID_Debugging], or a
    // view appropriate for text view manipulation as by navigating to a find
    // result [LOGVIEWID_TextView]). A PHYSICAL view identifies an actual type
    // of view implementation that an IVsEditorFactory can create.
    //
    // NOTE: Physical views are identified by a string of your choice with the
    // one constraint that the default/primary physical view for an editor
    // *MUST* use a NULL string as its physical view name (*pbstrPhysicalView = NULL).
    //
    // NOTE: It is essential that the implementation of MapLogicalView properly
    // validates that the LogicalView desired is actually supported by the editor.
    // If an unsupported LogicalView is requested then E_NOTIMPL must be returned.
    //
    // NOTE: The special Logical Views supported by an Editor Factory must also
    // be registered in the local registry hive. LOGVIEWID_Primary is implicitly
    // supported by all editor types and does not need to be registered.
    // For example, an editor that supports a ViewCode/ViewDesigner scenario
    // might register something like the following:
    //        HKLM\Software\Microsoft\VisualStudio\9.0\Editors\
    //            {...guidEditor...}\
    //                LogicalViews\
    //                    {...LOGVIEWID_TextView...} = s ''
    //                    {...LOGVIEWID_Code...} = s ''
    //                    {...LOGVIEWID_Debugging...} = s ''
    //                    {...LOGVIEWID_Designer...} = s 'Form'
    //
    public virtual int MapLogicalView(ref Guid logicalView, out string physicalView) {
      // initialize out parameter
      physicalView = null;

      var isSupportedView = false;
      // Determine the physical view
      if (VSConstants.LOGVIEWID_Primary == logicalView
          || VSConstants.LOGVIEWID_Debugging == logicalView
          || VSConstants.LOGVIEWID_Code == logicalView
          || VSConstants.LOGVIEWID_TextView == logicalView) {
        // primary view uses NULL as pbstrPhysicalView
        isSupportedView = true;
      }
      else if (VSConstants.LOGVIEWID_Designer == logicalView) {
        physicalView    = "Design";
        isSupportedView = true;
      }

      if (isSupportedView) {
        return VSConstants.S_OK;
      }
      // E_NOTIMPL must be returned for any unrecognized rguidLogicalView values
      return VSConstants.E_NOTIMPL;
    }

    public virtual int Close() {
      return VSConstants.S_OK;
    }

    /// <summary>
    /// </summary>
    /// <param name="grfCreateDoc"></param>
    /// <param name="pszMkDocument"></param>
    /// <param name="pszPhysicalView"></param>
    /// <param name="pvHier"></param>
    /// <param name="itemid"></param>
    /// <param name="punkDocDataExisting"></param>
    /// <param name="ppunkDocView"></param>
    /// <param name="ppunkDocData"></param>
    /// <param name="pbstrEditorCaption"></param>
    /// <param name="pguidCmdUI"></param>
    /// <param name="pgrfCDW"></param>
    /// <returns></returns>
    public virtual int CreateEditorInstance(
        uint createEditorFlags,
        string documentMoniker,
        string physicalView,
        IVsHierarchy hierarchy,
        uint itemid,
        IntPtr docDataExisting,
        out IntPtr docView,
        out IntPtr docData,
        out string editorCaption,
        out Guid commandUIGuid,
        out int createDocumentWindowFlags) {
      ThreadHelper.ThrowIfNotOnUIThread();
      // Initialize output parameters
      docView                   = IntPtr.Zero;
      docData                   = IntPtr.Zero;
      commandUIGuid             = Guid.Empty;
      createDocumentWindowFlags = 0;
      editorCaption             = null;

      // Validate inputs
      if ((createEditorFlags & (uint)(VSConstants.CEF.OpenFile | VSConstants.CEF.Silent)) == 0) {
        return VSConstants.E_INVALIDARG;
      }

      if (docDataExisting != IntPtr.Zero && PromptEncodingOnLoad) {
        return VSConstants.VS_E_INCOMPATIBLEDOCDATA;
      }

      // Get a text buffer
      var textLines = GetTextBuffer(docDataExisting, documentMoniker);

      // Assign docData IntPtr to either existing docData or the new text buffer
      if (docDataExisting != IntPtr.Zero) {
        docData = docDataExisting;
        Marshal.AddRef(docData);
      }
      else {
        docData = Marshal.GetIUnknownForObject(textLines);
      }

      try {
        var docViewObject = CreateDocumentView(documentMoniker, physicalView, hierarchy, itemid, textLines,
            docDataExisting == IntPtr.Zero, out editorCaption, out commandUIGuid);
        docView = Marshal.GetIUnknownForObject(docViewObject);
      }
      finally {
        if (docView == IntPtr.Zero) {
          if (docDataExisting != docData && docData != IntPtr.Zero) {
            // Cleanup the instance of the docData that we have addref'ed
            Marshal.Release(docData);
            docData = IntPtr.Zero;
          }
        }
      }

      return VSConstants.S_OK;
    }

    public virtual object GetService(Type serviceType) {
      ThreadHelper.ThrowIfNotOnUIThread();
      return ServiceProvider.GetService(serviceType);
    }

    protected virtual IVsTextLines GetTextBuffer(IntPtr docDataExisting, string filename) {
      ThreadHelper.ThrowIfNotOnUIThread();
      IVsTextLines textLines;
      if (docDataExisting == IntPtr.Zero) {
        // Create a new IVsTextLines buffer.
        var textLinesType = typeof(IVsTextLines);
        var riid          = textLinesType.GUID;
        var clsid         = typeof(VsTextBufferClass).GUID;
        textLines         = Package.CreateInstance(ref clsid, ref riid, textLinesType) as IVsTextLines;

        // set the buffer's site
        ((IObjectWithSite)textLines).SetSite(ServiceProvider.GetService(typeof(IOleServiceProvider)));
      }
      else {
        // Use the existing text buffer
        var dataObject = Marshal.GetObjectForIUnknown(docDataExisting);
        textLines      = dataObject as IVsTextLines;
        if (textLines == null) {
          // Try get the text buffer from textbuffer provider
          var textBufferProvider = dataObject as IVsTextBufferProvider;
          if (textBufferProvider != null) {
            textBufferProvider.GetTextBuffer(out textLines);
          }
        }
        if (textLines == null) {
          // Unknown docData type then, so we have to force VS to close the other editor.
          throw Marshal.GetExceptionForHR(VSConstants.VS_E_INCOMPATIBLEDOCDATA);
        }
      }

      return textLines;
    }

    protected virtual object CreateDocumentView(string documentMoniker, string physicalView, IVsHierarchy hierarchy,
        uint itemid, IVsTextLines textLines, bool createdDocData, out string editorCaption, out Guid cmdUI) {
      ThreadHelper.ThrowIfNotOnUIThread();
      //Init out params
      editorCaption = string.Empty;
      cmdUI         = Guid.Empty;

      if (string.IsNullOrEmpty(physicalView)) {
        // create code window as default physical view
        return CreateCodeView(documentMoniker, textLines, createdDocData, ref editorCaption, ref cmdUI);
      }

      // We couldn't create the view
      // Return special error code so VS can try another editor factory.
      throw Marshal.GetExceptionForHR(VSConstants.VS_E_UNSUPPORTEDFORMAT);
    }

    protected virtual IVsCodeWindow CreateCodeView(string documentMoniker, IVsTextLines textLines,
        bool createdDocData, ref string editorCaption, ref Guid cmdUI) {
      ThreadHelper.ThrowIfNotOnUIThread();
      var codeWindowType = typeof(IVsCodeWindow);
      var riid           = codeWindowType.GUID;
      var clsid          = typeof(VsCodeWindowClass).GUID;

      //var compModel      = (IComponentModel)new VsServiceProviderWrapper(Package).GetService(typeof(SComponentModel)); zdes bylo tak
      var compModel      = (IComponentModel) ServiceProvider.GetService(typeof(SComponentModel));
      Assumes.Present(compModel);
      var adapterService = compModel.GetService<IVsEditorAdaptersFactoryService>();

      var window = adapterService.CreateVsCodeWindowAdapter((IOleServiceProvider) ServiceProvider.GetService(typeof(IOleServiceProvider)));
      ErrorHandler.ThrowOnFailure(window.SetBuffer(textLines));
      ErrorHandler.ThrowOnFailure(window.SetBaseEditorCaption(null));
      ErrorHandler.ThrowOnFailure(window.GetEditorCaption(READONLYSTATUS.ROSTATUS_Unknown, out editorCaption));

      var userData = textLines as IVsUserData;
      if (userData != null) {
        if (PromptEncodingOnLoad) {
          var guid = VSConstants.VsTextBufferUserDataGuid.VsBufferEncodingPromptOnLoad_guid;
          userData.SetData(ref guid, (uint)1);
        }
      }

      cmdUI = VSConstants.GUID_TextEditorFactory;

      //var compModel = (IComponentModel)new VsServiceProviderWrapper(Package).GetService(typeof(SComponentModel)); zdes bylo tak
      var componentModel = (IComponentModel) ServiceProvider.GetService(typeof(SComponentModel));
      var bufferEventListener = new TextBufferEventListener(componentModel, textLines, _languageServiceId);
      if (!createdDocData) {
        // we have a pre-created buffer, go ahead and initialize now as the buffer already
        // exists and is initialized
        bufferEventListener.OnLoadCompleted(0);
      }

      return window;
    }

    private sealed class TextBufferEventListener : IVsTextBufferDataEvents {
      private readonly IComponentModel _componentModel;

      private readonly IConnectionPoint _connectionPoint;
      private readonly uint _cookie;
      private readonly IVsTextLines _textLines;
      private Guid _languageServiceId;

      public TextBufferEventListener(IComponentModel componentModel, IVsTextLines textLines, Guid languageServiceId) {
        ThreadHelper.ThrowIfNotOnUIThread();
        _componentModel    = componentModel;
        _textLines         = textLines;
        _languageServiceId = languageServiceId;

        var connectionPointContainer = textLines as IConnectionPointContainer;
        var bufferEventsGuid         = typeof(IVsTextBufferDataEvents).GUID;
        connectionPointContainer.FindConnectionPoint(ref bufferEventsGuid, out _connectionPoint);
        _connectionPoint.Advise(this, out _cookie);
      }

      public void OnFileChanged(uint grfChange, uint dwFileAttrs) { }

      public int OnLoadCompleted(int fReload) {
        ThreadHelper.ThrowIfNotOnUIThread();
        _connectionPoint.Unadvise(_cookie);
        _textLines.SetLanguageServiceID(ref _languageServiceId);

        return VSConstants.S_OK;
      }
    }
  }
}