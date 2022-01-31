using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace MPLVS {
  internal class Constants {
    public const string LanguageName     = "MPL";
    public const string MPLContentType   = LanguageName;
    public const string MPLFileExtension = ".mpl";

    // product registration
    public const int MPLLanguageResourceId = 100;

    public const string MPLLanguagePackageNameResourceString    = "#110";
    public const string MPLLanguagePackageDetailsResourceString = "#111";
    public const string MPLLanguagePackageProductVersionString  = "1.0";

    public const string PackageGuidString = "4ff147de-0c36-4d06-98cd-b39630d67bad";
    public static Guid PackageGuid        = new Guid("{" + PackageGuidString + "}");

    public const string UIContextNoSolution     = "86EDA1AB-28E5-4797-AA0D-557C5653183F";
    public const string UIContextSolutionExists = "5C621FA6-C3ED-4244-B11F-5D4BFA93B649";

    internal static readonly ImmutableHashSet<string> Builtins = new List<string>() {
      "!",
      "&",
      "*",
      "+",
      "-",
      "/",
      "<",
      "=",
      ">",
      "@",
      "COMPILER_VERSION",
      "DEBUG",
      "FALSE",
      "LF",
      "TRUE",
      "^",
      "addressToReference",
      "alignment",
      "and",
      "array",
      "call",
      "callField",
      "cast",
      "ceil",
      "codeRef",
      "compileOnce",
      "const",
      "cos",
      "def",
      "dynamic",
      "exportFunction",
      "exportVariable",
      "failProc",
      "fieldCount",
      "fieldIndex",
      "fieldName",
      "floor",
      "getCallTrace",
      "has",
      "if",
      "importFunction",
      "importVariable",
      "is",
      "isCombined",
      "isConst",
      "isDirty",
      "isDynamic",
      "isRef",
      "isStatic",
      "log",
      "log10",
      "loop",
      "lshift",
      "manuallyDestroyVariable",
      "manuallyInitVariable",
      "mod",
      "neg",
      "new",
      "newVarOfTheSameType",
      "or",
      "overload",
      "printCompilerMaxAllocationSize",
      "printCompilerMessage",
      "printMatchingTree",
      "printShadowEvents",
      "printStack",
      "printStackTrace",
      "raiseStaticError",
      "recursive",
      "rshift",
      "same",
      "set",
      "sin",
      "sqrt",
      "static",
      "storageAddress",
      "storageSize",
      "textSize",
      "textSplit",
      "ucall",
      "uif",
      "use",
      "virtual",
      "xor",
      "~"
    }.ToImmutableHashSet();
  }
}