using DevExpress.ExpressApp.Blazor.Components.Models;
using Microsoft.AspNetCore.Components;

namespace MailMergeTree.Blazor.Server.Editors;

// Component model of MergeFieldTreePopup: property names match the component's parameters.
public class MergeFieldTreePopupModel : ComponentModelBase {
    public EventCallback<string> FieldSelected {
        get => GetPropertyValue<EventCallback<string>>();
        set => SetPropertyValue(value);
    }
    public override Type ComponentType => typeof(MergeFieldTreePopup);
    public new MergeFieldTreePopup ComponentInstance => (MergeFieldTreePopup)base.ComponentInstance;
}
