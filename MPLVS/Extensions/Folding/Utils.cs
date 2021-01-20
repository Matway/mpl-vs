using System.Linq;
using System.Runtime.CompilerServices;

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;

using static MPLVS.Folding.Tagger;

namespace MPLVS.Folding {
  internal static class Utils {
    internal static OutliningRegionTag ToOutliningTag(this Region region, SnapshotSpan snapshot) =>
      new OutliningRegionTag(false, region.IsImplementation(), " ... ", new Tooltip(snapshot));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool IsImplementation(this Region region) => "[(".Contains(region.type);
  }
}