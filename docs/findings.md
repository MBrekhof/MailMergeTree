# Hierarchical merge-field picker for the XAF WinForms mail merge editor

Spike result, 2026-11-18. XAF 26.1, WinForms, Middle Tier security. The skeleton is EF Core (the original question
says XPO); the picker only uses XAF type info and the RichEditControl, so nothing in it is ORM-specific.

## Result

It works. In the Mail Merge Template detail view, both halves of the ribbon's Insert Merge Field button open a tree:
root = the template's Data Type, reference properties expand lazily, captions come from the Application Model, the
chosen path is inserted as a MERGEFIELD at the caret. Checked in the running app: `Debtor.Attention` and
`Debtor.Address.City` inserted from the tree, and View Merged Data showed the seeded values ("J. Jansen",
"Amsterdam") through the Middle Tier.

Code: `MailMergeTree.Win/Controllers/MailMergeFieldTreeController.cs` and `MergeFieldTreeForm.cs` (about 190 lines).

## Answers to the questions

1. **Built-in hierarchical field list?** No. The docs (Mail Merge, "Subfields") state subfields are not in the Insert
   Merge Field list and must be typed via Show All Field Codes. The 26.1 sources confirm it: the ribbon item
   (`DevExpress.XtraRichEdit.UI.InsertMergeFieldItem`) fills a flat `PopupMenu` from `GetDatabaseFieldNames()` plus
   the `CustomizeMergeFields` event.
2. **Roadmap / feature request?** Not answerable from docs or sources; only DevExpress support can say.
3. **Extension point.** Two are needed, because the ribbon item is a split button:
   - `RichEditControl.InsertMergeFieldFormShowing` (set `Handled`, show your own form) covers the button half and any
     other caller of the `ShowInsertMergeFieldForm` command.
   - The arrow half never runs that command. It opens the item's `PopupMenu`; cancelling `PopupMenu.BeforePopup`
     suppresses the flat list. The item is reachable through `MenuManagerController.CustomizeRibbonControl`.
   - Replacing the command through `IRichEditCommandFactoryService` is not needed and would not cover the arrow half
     either. XAF already replaces that service (`XafRichEditCommandFactoryService`) in `MailMergeController`; inserting
     through `richEdit.CreateCommand(RichEditCommandId.InsertMailMergeField)` keeps XAF's image handling.
   - There is no XAF-level hook for the field list in `MailMergeController`; `CustomizeInsertMergeField()` is virtual
     but only installs the command factory.
4. **Reusable "Object Model" tree?** Yes, both public in `DevExpress.ExpressApp.Win`:
   `DevExpress.ExpressApp.Win.Core.ModelBrowserAdapter` (members of an `ITypeInfo`, captions via `CaptionHelper`,
   `ShowCollection` switch) on a `DevExpress.ExpressApp.Win.Controls.ObjectTreeList` with
   `BuildChildNodesRecursive = false` for lazy loading. The complete dialog, `ModelBrowser`, is public too, but its OK
   button only enables after a check/double-click toggle, so the spike hosts adapter + tree in its own small form.
   Neither class is in the public documentation: treat them as undocumented API that can change between versions.

## Not covered by the spike

- The tree lists members from type info; member-level read permissions are not applied to it.
- Bars menu mode (no ribbon) keeps the flat drop-down; the button itself still opens the tree.
- Image members in a nested path were not tested.
- Only the default skin was checked.
- An external review predicted the arrow-half hook would hit the wrong tab with two templates open or after a reopen.
  That did not reproduce (two tabs at once, first tab closed, saved template reopened); in tabbed MDI each detail
  view gets its own rich-edit ribbon items. Not tested with other UI types (SDI, non-tabbed MDI).

## Blazor version (added 2026-11-18)

Same picker in a Blazor Server project that uses the database directly (integrated security, no Middle Tier). Code:
`MailMergeTree.Blazor.Server/Controllers/MailMergeFieldTreeController.cs` and `Editors/` (about 150 lines).

Checked in a browser (Playwright, light and dark theme): tree opens from Mail Merge > Insert Merge Field; a reference
node cannot be inserted; `Debtor.Address.City` and `Number` land at the caret; both survive Save; **Show in document**
on `INV-001` gives "City: Amsterdam Nr: INV-001".

What differs from WinForms:

- **Extension point:** `DxRichEditModel.CustomizeRibbon` (chained to XAF's own handler), then `IBarButton.Click` of
  `RichEditBarItemNames.ShowInsertMergeFieldDialog`. One hook; there is no split button.
- **No reusable Object Model tree:** `ModelBrowserAdapter` is Win-only. `MergeFieldNode` repeats its rules on
  `ITypeInfo`.
- **Caret:** `DxRichEdit` only tracks the selection on the server while `SelectionChanged` has a handler.
- **Popup hosting:** `RichTextPropertyEditor.Extensions`. Changing a model in that list re-renders the editor and
  resets its modified flag in the browser, which silently drops an insert made just before Save. The popup therefore
  holds its own state. Found by logging `ModifiedChanged` / `DocumentContentChanged`.

Finding, not fixed (DevExpress behaviour): **View Merged Data in the Blazor template editor shows nested fields
empty**, while top-level fields show values. The Blazor editor merges that preview in the browser and
`DevExpress.Blazor.RichEdit` (`Utils/DataConverter.cs`) sends only the data source's top-level columns. The server-side
merge behind **Show in document** resolves nested paths. A fix would mean supplying the preview with flattened rows
(dictionary rows keyed by the dotted paths used in the document); not tried.

Not covered: toolbar bar mode, replacing a selected range, double-click insert, image members, member-level
permissions.

## Run it

See the README. To build the picker into another app: [how-to-implement.md](how-to-implement.md).
