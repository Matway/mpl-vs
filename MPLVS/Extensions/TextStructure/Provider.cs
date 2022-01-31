using System.ComponentModel.Composition;

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;

namespace MPLVS.Extensions.TextStructure {
  [Export(typeof(ITextStructureNavigatorProvider))]
  [ContentType(Constants.MPLContentType)]
  internal class Provider : ITextStructureNavigatorProvider {
    public ITextStructureNavigator CreateTextStructureNavigator(ITextBuffer textBuffer) => Navigator;

    internal static readonly Navigator Navigator = new Navigator();
  }
}