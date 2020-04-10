using System;
using System.Linq;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TextManager.Interop;

namespace MPL {
  public class LanguageSettings : IVsTextManagerEvents {
    LANGPREFERENCES _Preferences;

    public LanguageSettings() {
      IVsTextManager textManager = (IVsTextManager) Package.GetGlobalService(typeof(SVsTextManager));
      LANGPREFERENCES[] preferences = new LANGPREFERENCES[1];
      preferences[0].guidLang = new Guid("5b6692e7-a860-4b7d-9242-a6781510dee0");

      if (textManager.GetUserPreferences(null, null, preferences, null) == 0) {
        _Preferences = preferences[0];
      }
    }

    public int FormatterIndentSize {
      get {
        if (!IsUsingSpaces)
          return (int) (_Preferences.uIndentSize / _Preferences.uTabSize);

        return (int) _Preferences.uIndentSize;
      }
    }

    public int FormatterTabSize {
      get { return (int) _Preferences.uTabSize; }
    }

    public bool IsUsingSpaces {
      get {
        if (_Preferences.fInsertTabs != 0 && _Preferences.uTabSize != 0 && (_Preferences.uIndentSize % _Preferences.uTabSize) == 0)
          return false;

        return true;
      }
    }

    public void OnRegisterMarkerType(int iMarkerType) {
      throw new NotImplementedException();
    }

    public void OnRegisterView(IVsTextView pView) {
      throw new NotImplementedException();
    }

    public void OnUnregisterView(IVsTextView pView) {
      throw new NotImplementedException();
    }

    public void OnUserPreferencesChanged(VIEWPREFERENCES[] pViewPrefs, FRAMEPREFERENCES[] pFramePrefs, LANGPREFERENCES[] pLangPrefs, FONTCOLORPREFERENCES[] pColorPrefs) {
      if (pLangPrefs != null) {
        LANGPREFERENCES[] preferences = pLangPrefs.Where(i => i.guidLang == _Preferences.guidLang).ToArray();
        if (preferences.Length > 0)
          _Preferences = preferences[0];
      }
    }
  }
}