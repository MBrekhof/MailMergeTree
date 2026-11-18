using DevExpress.Blazor.Office;
using DevExpress.Blazor.RichEdit;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Office.Blazor.Editors;
using DevExpress.Persistent.Base;
using MailMergeTree.Blazor.Server.Editors;
using Microsoft.AspNetCore.Components;

namespace MailMergeTree.Blazor.Server.Controllers;

// Replaces the flat "Insert Merge Field" dialog of the mail merge template editor with a field tree.
public class MailMergeFieldTreeController : ObjectViewController<DetailView, IRichTextMailMergeData> {
    private RichTextPropertyEditor editor;
    private MergeFieldTreePopupModel popup;

    protected override void OnActivated() {
        base.OnActivated();
        View.CustomizeViewItemControl<RichTextPropertyEditor>(this, CustomizeEditor, nameof(IRichTextMailMergeData.Template));
    }

    protected override void OnDeactivated() {
        editor = null;
        popup = null;
        base.OnDeactivated();
    }

    private void CustomizeEditor(RichTextPropertyEditor richTextEditor) {
        editor = richTextEditor;
        // Controller reactivated on a live editor: everything below is already in place, and repeating it
        // would add a second popup and re-render the editor (see MergeFieldTreePopup.ShowAsync).
        popup = editor.Extensions.OfType<MergeFieldTreePopupModel>().FirstOrDefault();
        if(popup != null) return;

        // XAF's own handler (hides Save, shows the Mail Merge tab) must keep running: chain, do not replace.
        EventCallback<IRibbon> xafCustomizeRibbon = editor.ComponentModel.CustomizeRibbon;
        editor.ComponentModel.CustomizeRibbon = EventCallback.Factory.Create<IRibbon>(this, async ribbon => {
            await xafCustomizeRibbon.InvokeAsync(ribbon);
            // ponytail: ribbon only (XAF's default bar mode); hook CustomizeToolbar the same way if the toolbar mode is used.
            IBarItem item = ribbon.Tabs[RichEditRibbonTabNames.MailMerge]?
                .Groups[RichEditRibbonGroupNames.MailMergeInsertFields]?
                .Items[RichEditBarItemNames.ShowInsertMergeFieldDialog];
            if(item is IBarButton button) {
                button.Click = ShowPicker;
            }
        });
        // DxRichEdit keeps its Selection up to date on the server only while SelectionChanged has a handler;
        // without one the caret position stays 0 and every field lands at the start of the document.
        if(!editor.ComponentModel.SelectionChanged.HasDelegate) {
            editor.ComponentModel.SelectionChanged = EventCallback.Factory.Create<Selection>(this, _ => { });
        }
        // Rendered next to the editor through DxRichEdit's Extensions fragment.
        // The model must not change after this point: a change re-renders the editor (see MergeFieldTreePopup.ShowAsync).
        popup = new MergeFieldTreePopupModel { FieldSelected = EventCallback.Factory.Create<string>(this, InsertField) };
        editor.Extensions.Add(popup);
    }

    private Task ShowPicker() {
        Type dataType = ViewCurrentObject?.DataType; // read at click time: the user can change Data Type
        if(dataType == null) {
            Application.ShowViewStrategy.ShowMessage("Select a Data Type first.", InformationType.Warning);
            return Task.CompletedTask;
        }
        MergeFieldTreePopup picker = popup?.ComponentInstance;
        return picker == null ? Task.CompletedTask : picker.ShowAsync(Application.TypesInfo.FindTypeInfo(dataType));
    }

    private async Task InsertField(string path) {
        DxRichEdit richEdit = editor?.DxRichEditInstance;
        if(richEdit == null) return;
        try {
            // ponytail: inserts at the caret and keeps a selected range; replace the range first if users expect Word's behavior.
            // Image members need no special code here: XAF turns their MERGEFIELD into INCLUDEPICTURE when it merges.
            Selection selection = richEdit.Selection;
            SubDocument target = selection?.ActiveSubDocument ?? richEdit.DocumentAPI;
            await target.Fields.CreateAsync(selection?.CaretPosition ?? 0, "MERGEFIELD " + path);
            // Nothing to save here: the insert flags the document as modified and XAF stores it like typed text.
        }
        catch(OperationCanceledException) { } // editor disposed while the call was on its way to the browser
        catch(Exception e) {
            // This runs from the popup's button, outside XAF's action error handling: report instead of breaking the circuit.
            Application?.ShowViewStrategy.ShowMessage("The field could not be inserted: " + e.Message, InformationType.Error);
        }
    }
}
