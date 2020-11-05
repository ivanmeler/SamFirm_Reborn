using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Resources;
using System.Runtime.CompilerServices;

namespace SamFirm.Properties
{
  [GeneratedCode("System.Resources.Tools.StronglyTypedResourceBuilder", "4.0.0.0")]
  [DebuggerNonUserCode]
  [CompilerGenerated]
  internal class Resources
  {
    private static ResourceManager resourceMan;
    private static CultureInfo resourceCulture;

    internal Resources()
    {
    }

    [EditorBrowsable(EditorBrowsableState.Advanced)]
    internal static ResourceManager ResourceManager
    {
      get
      {
        if (object.ReferenceEquals((object) SamFirm.Properties.Resources.resourceMan, (object) null))
          SamFirm.Properties.Resources.resourceMan = new ResourceManager("SamFirm.Properties.Resources", typeof (SamFirm.Properties.Resources).Assembly);
        return SamFirm.Properties.Resources.resourceMan;
      }
    }

    [EditorBrowsable(EditorBrowsableState.Advanced)]
    internal static CultureInfo Culture
    {
      get
      {
        return SamFirm.Properties.Resources.resourceCulture;
      }
      set
      {
        SamFirm.Properties.Resources.resourceCulture = value;
      }
    }
  }
}
