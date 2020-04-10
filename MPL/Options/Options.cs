using System;
using System.ComponentModel;
using System.Globalization;
using Microsoft.VisualStudio.Shell;
using System.Collections.Generic;

namespace MPL {
  public class Options : DialogPage {
    [Category("Brace Matching")]
    [DisplayName("Carriage behind the closing brace")]
    [Description("If option is true, the opening brace will be highlighted when the carriage is\nbehind the closing brace, otherwise the carriage must be before the brace")]
    [DefaultValue(false)]
    public bool SharpStyleBraceMatch { get; set; } = false;

    public enum LineEnding { LINUX, WINDOWS, AS_IS }

    [Category("Line Endings")]
    [DisplayName("Choose line endings")]
    [Description("UNIX(DEFAULT) - LF, WINDOWS - CRLF, AS IS - leave as is")]
    [DefaultValue(LineEnding.LINUX)]
    public LineEnding LineEndings { get; set; } = LineEnding.LINUX;

    [Category("Autoindentation")]
    [DisplayName("Autoindentation")]
    [Description("Defines if there will be autoindentation on ENTER")]
    [DefaultValue(true)]
    public bool AutoIndent { get; set; } = true;

    [Category("AutoBraceCompletion")]
    [DisplayName("Auto brace completion")]
    [Description("Defines if bracket will be completed automatically")]
    [DefaultValue(true)]
    public bool AutoBraceCompletion { get; set; } = true;

    [Category("Guillemets")]
    [DisplayName("Guillemets (angular quotes)")]
    [Description("When entering < or > symbols, but there is the same symbol on the left, transforms them into « or » symbols.")]
    [DefaultValue(true)]
    public bool AngularQuotes { get; set; } = true;

    [Category("Themes")]
    [DisplayName("Solarized theme")]
    [Description("Defines if solarized theme will be autoloaded")]
    [DefaultValue(false)]
    public bool SolarizedTheme { get; set; } = false;

    [Category("Themes")]
    [DisplayName("Dark themes")]
    [Description("List of themes for which dark solarized theme loads")]
    [DefaultValue(typeof(List<string>), "Dark")]
    [Editor(
        "System.Windows.Forms.Design.StringCollectionEditor, System.Design, Version=1.0.5000.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
        "System.Drawing.Design.UITypeEditor, System.Drawing, Version=1.0.5000.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    [TypeConverter(typeof(StringListConverter))]
    public List<string> DarkThemesList { get; set; } = new List<string>() { "Dark" };
  }

  class StringListConverter : TypeConverter {
    private const string delimiter = ", ";

    public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) {
      return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
    }

    public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) {
      return destinationType == typeof(List<string>) || base.CanConvertTo(context, destinationType);
    }

    public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value) {
      string v = value as string;
      List<string> lst = new List<string>();
      foreach (string str in v.Split(new[] { delimiter }, StringSplitOptions.RemoveEmptyEntries)) {
        lst.Add(str);
      }

      return v == null ? base.ConvertFrom(context, culture, value) : lst;
    }

    public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType) {
      List<string> v = value as List<string>;
      if (destinationType != typeof(string) || v == null) {
        return base.ConvertTo(context, culture, value, destinationType);
      }
      return string.Join(delimiter, v);
    }
  }

}