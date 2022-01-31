// FIXME: Threw away this class\file, and reuse vs api.

using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;

namespace MPLVS.Symbols {
  internal static class OpenedBuffers {
    public static void VsTextViewCreated(IVsTextView textViewAdapter, IWpfTextView textView) {
      var buffer = textView.TextBuffer;
      if (Buffers.ContainsKey(buffer)) {
        ++Buffers[buffer];
      }
      else {
        Collectors.AddProperty(buffer, textView.ObtainOrAttachProperty(() => new SymbolCollector(textView)));
        Buffers[buffer] = 1;
      }

      WpfToVs[textView]     = textViewAdapter;
      WpfToBuffer[textView] = buffer;

      textView.Closed += ViewClosed;
    }

    private static void ViewClosed(object view, EventArgs data) {
      var wpf = view as IWpfTextView;

      WpfToVs.Remove(wpf);

      var buffer = WpfToBuffer[wpf];
      if (--Buffers[buffer] == 0) {
        Collectors.RemoveProperty(buffer);
      }
    }

    public static List<ITextBuffer> TextBuffers =>
      Buffers.Keys.ToList();

    public static Dictionary<IWpfTextView, IVsTextView> WpfToVs     = new Dictionary<IWpfTextView, IVsTextView>();
    public static Dictionary<IWpfTextView, ITextBuffer> WpfToBuffer = new Dictionary<IWpfTextView, ITextBuffer>();

    public static IEnumerable<IWpfTextView> Views => WpfToBuffer.Keys;

    private static readonly Dictionary<ITextBuffer, int> Buffers = new Dictionary<ITextBuffer, int>();
    private static readonly PropertyCollection Collectors        = new PropertyCollection();
  }
}