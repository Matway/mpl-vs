using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;

using MPLVS.Extensions;

namespace MPLVS.Folding {
  internal static class Utils {
    internal static Tag ToOutliningTag(this Region region, SnapshotSpan snapshot, object placeholder) =>
      new Tag(
        snapshot:                  snapshot.Snapshot,
        outliningSpan:             snapshot,
        headerSpan:                null,
        guideLineSpan:             null,
        guideLineHorizontalAnchor: null,
        type:                      PredefinedStructureTagTypes.Comment,
        isCollapsible:             true,
        isDefaultCollapsed:        false,
        isImplementation:          !region.IsSignificant,
        collapsedForm:             placeholder
      );

    internal static Tag ToStructureTag(this Region region, SnapshotSpan snapshot, object placeholder) =>
      new Tag(
        snapshot:                  snapshot.Snapshot,
        outliningSpan:             snapshot,
        headerSpan:                region.Header,
        guideLineSpan:             null,
        guideLineHorizontalAnchor: null,
        type:                      PredefinedStructureTagTypes.Structural,
        isCollapsible:             true,
        isDefaultCollapsed:        false,
        isImplementation:          !region.IsSignificant,
        collapsedForm:             placeholder
      );
  }
}