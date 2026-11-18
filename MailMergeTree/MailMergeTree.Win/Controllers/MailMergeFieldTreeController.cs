using System.ComponentModel;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Office.Win;
using DevExpress.Persistent.Base;
using DevExpress.XtraBars;
using DevExpress.XtraRichEdit;
using DevExpress.XtraRichEdit.Commands;
using DevExpress.XtraRichEdit.UI;

namespace MailMergeTree.Win.Controllers
{
    // Replaces the flat "Insert Merge Field" list of the mail merge template editor with a field tree.
    // Two hooks, because the ribbon item is a split button:
    //  - button half -> ShowInsertMergeFieldForm command -> RichEditControl.InsertMergeFieldFormShowing
    //  - arrow half  -> PopupMenu of InsertMergeFieldItem  -> cancelled in BeforePopup
    public class MailMergeFieldTreeController : ObjectViewController<DetailView, IRichTextMailMergeData>
    {
        private MenuManagerController menuManagerController;
        private RichTextPropertyEditor editor;
        private RichEditControl richEdit;
        private readonly List<PopupMenu> hookedMenus = new();

        protected override void OnActivated()
        {
            base.OnActivated();
            menuManagerController = Frame.GetController<MenuManagerController>();
            if (menuManagerController != null)
            {
                menuManagerController.CustomizeRibbonControl += MenuManagerController_CustomizeRibbonControl;
            }
        }

        protected override void OnViewControlsCreated()
        {
            base.OnViewControlsCreated();
            editor = View.FindItem(nameof(IRichTextMailMergeData.Template)) as RichTextPropertyEditor;
            if (editor == null) return;
            if (editor.RichEditControl != null) HookRichEdit();
            else editor.ControlCreated += Editor_ControlCreated;
        }

        protected override void OnDeactivated()
        {
            if (menuManagerController != null)
            {
                menuManagerController.CustomizeRibbonControl -= MenuManagerController_CustomizeRibbonControl;
                menuManagerController = null;
            }
            foreach (PopupMenu menu in hookedMenus) menu.BeforePopup -= InsertMergeFieldMenu_BeforePopup;
            hookedMenus.Clear();
            if (editor != null) editor.ControlCreated -= Editor_ControlCreated;
            if (richEdit != null) richEdit.InsertMergeFieldFormShowing -= RichEdit_InsertMergeFieldFormShowing;
            editor = null;
            richEdit = null;
            base.OnDeactivated();
        }

        private void Editor_ControlCreated(object sender, EventArgs e)
        {
            editor.ControlCreated -= Editor_ControlCreated;
            HookRichEdit();
        }

        private void HookRichEdit()
        {
            if (richEdit != null) richEdit.InsertMergeFieldFormShowing -= RichEdit_InsertMergeFieldFormShowing;
            richEdit = editor.RichEditControl;
            if (richEdit != null) richEdit.InsertMergeFieldFormShowing += RichEdit_InsertMergeFieldFormShowing;
        }

        private void MenuManagerController_CustomizeRibbonControl(object sender, CustomizeRibbonControlEventArgs e)
        {
            foreach (InsertMergeFieldItem item in e.RibbonControl.Items.OfType<InsertMergeFieldItem>())
            {
                if (item.DropDownControl is PopupMenu menu && !hookedMenus.Contains(menu))
                {
                    menu.BeforePopup += InsertMergeFieldMenu_BeforePopup;
                    hookedMenus.Add(menu);
                }
            }
        }

        private void InsertMergeFieldMenu_BeforePopup(object sender, CancelEventArgs e)
        {
            e.Cancel = true;
            // Let the ribbon finish closing its drop-down before a modal form opens.
            if (richEdit != null && richEdit.IsHandleCreated) richEdit.BeginInvoke(new Action(ShowPicker));
        }

        private void RichEdit_InsertMergeFieldFormShowing(object sender, InsertMergeFieldFormShowingEventArgs e)
        {
            e.Handled = true;
            ShowPicker();
        }

        private void ShowPicker()
        {
            if (View == null || richEdit == null || richEdit.IsDisposed) return;
            Type dataType = ViewCurrentObject?.DataType;
            if (dataType == null) return;
            using var form = new MergeFieldTreeForm(Application.TypesInfo.FindTypeInfo(dataType));
            if (form.ShowDialog(richEdit.FindForm()) != DialogResult.OK) return;
            // Goes through XAF's command factory, so image members keep their INCLUDEPICTURE handling.
            if (richEdit.CreateCommand(RichEditCommandId.InsertMailMergeField) is InsertMergeFieldCommand command)
            {
                command.FieldArgument = form.SelectedPath;
                command.Execute();
            }
        }
    }
}
