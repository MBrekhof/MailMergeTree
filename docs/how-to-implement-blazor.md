# How to add the merge-field tree to your own XAF Blazor app

Written against XAF 26.1. Everything goes into the **Blazor Server project**; no changes to the module or the
business classes. It works the same for XPO and EF Core. The WinForms version is in
[how-to-implement.md](how-to-implement.md).

Prerequisite: mail merge already works in your app, that is `AddOffice(o => o.RichTextMailMergeDataType = ...)` is in
`Startup.cs` and you can open **Reports > Mail Merge Template**
([Mail Merge docs](https://docs.devexpress.com/eXpressAppFramework/400006)).

## 1. Copy four files

From `MailMergeTree/MailMergeTree.Blazor.Server/` into your Blazor project, and change the namespace:

- `Controllers/MailMergeFieldTreeController.cs` - the controller
- `Editors/MergeFieldNode.cs` - one tree node; decides which members show and which are insertable
- `Editors/MergeFieldTreePopup.razor` - the popup with the tree
- `Editors/MergeFieldTreePopupModel.cs` - its XAF component model

XAF finds the controller by itself. That is the whole installation; the rest explains what the files do.

## 2. The tree (`MergeFieldNode`, `MergeFieldTreePopup.razor`)

XAF's WinForms "Object Model" tree (`ModelBrowserAdapter`) does not exist for Blazor, so `MergeFieldNode` applies the
same rules to `ITypeInfo.Members`: the key member always; otherwise public and visible members, no collections (image
and rich-text `byte[]` members stay), caption from
`CaptionHelper.GetLastMemberPartCaption` (so Application Model captions and localization apply).

`Children` is created on first access and the `DxTreeView` runs with `LoadChildNodesOnDemand`, so only one level
ahead of what the user expanded exists. That keeps circular references (Invoice -> Debtor -> Invoices -> ...) harmless.

Only plain fields can be inserted (`IsField`): a reference node such as `Debtor` would merge as the object's default
text, which is the problem this picker solves.

## 3. Hooking the ribbon button (`MailMergeFieldTreeController`)

```csharp
View.CustomizeViewItemControl<RichTextPropertyEditor>(this, CustomizeEditor, nameof(IRichTextMailMergeData.Template));
```

In `CustomizeEditor`, three things happen on `editor.ComponentModel` (a `DxRichEditModel`):

**The button.** XAF already sets `CustomizeRibbon` (it hides Save and shows the Mail Merge tab), so chain to it instead
of replacing it, then give the built-in button a new action:

```csharp
EventCallback<IRibbon> xafCustomizeRibbon = editor.ComponentModel.CustomizeRibbon;
editor.ComponentModel.CustomizeRibbon = EventCallback.Factory.Create<IRibbon>(this, async ribbon => {
    await xafCustomizeRibbon.InvokeAsync(ribbon);
    IBarItem item = ribbon.Tabs[RichEditRibbonTabNames.MailMerge]?
        .Groups[RichEditRibbonGroupNames.MailMergeInsertFields]?
        .Items[RichEditBarItemNames.ShowInsertMergeFieldDialog];
    if(item is IBarButton button) button.Click = ShowPicker;
});
```

**The caret.** `DxRichEdit` keeps `Selection` up to date on the server only while `SelectionChanged` has a handler.
Without one, `Selection.CaretPosition` stays 0 and every field lands at the start of the document. The controller
attaches an empty handler; that is enough.

**The popup.** `editor.Extensions.Add(popupModel)` renders the popup next to the editor (DxRichEdit's `Extensions`
fragment).

## 4. The trap: do not change the popup's model after it is added

The models in `Extensions` share the editor's change notifier. Changing a property on one (for example a `Visible`
flag) re-renders the whole `DxRichEdit`. XAF mirrors the editor's `Modified` state into the model without rendering, so
that render can hand the editor a stale `Modified = false`. When it lands right after the insert (closing the popup
through the model does exactly that), the browser is told "not modified" and XAF's Save stores the document
**without** the new field. It only shows when inserting is the last thing the user does before Save.

So the popup keeps its visibility to itself and is opened by a method call on the component instance:

```csharp
MergeFieldTreePopup picker = popup?.ComponentInstance;   // null until the popup has rendered once
picker?.ShowAsync(Application.TypesInfo.FindTypeInfo(ViewCurrentObject.DataType));
```

## 5. Inserting the field

```csharp
Selection selection = richEdit.Selection;
SubDocument target = selection?.ActiveSubDocument ?? richEdit.DocumentAPI;
await target.Fields.CreateAsync(selection?.CaretPosition ?? 0, "MERGEFIELD " + path);
```

`ActiveSubDocument` makes it work in headers and footers. No explicit save is needed: the insert marks the document as
modified and XAF stores it like typed text. Image members need nothing special either; XAF rewrites their
`MERGEFIELD` into `INCLUDEPICTURE` when it merges (`BlazorShowInDocumentController`).

## 6. Things you may want to change

- **Hide fields** (for example `ID`): add a condition to `MergeFieldNode.IsDisplayed`.
- **Member-level security:** not applied; filter in `IsDisplayed` with the security strategy if users must not see
  field names they cannot read.
- **Toolbar bar mode:** hook `ComponentModel.CustomizeToolbar` the same way as `CustomizeRibbon`.
- **Replace a selected range:** remove the selection's interval before `CreateAsync`.

## 7. Check it

1. Open a template, pick a Data Type that has reference properties, type some text.
2. **Mail Merge > Insert Merge Field**: the tree opens. A reference node cannot be inserted, a field can.
3. Insert a nested field as the **last** action, press **Save**, reopen the template: the field is still there.
4. **Show All Field Codes**: the field sits where the caret was.
5. On the Data Type's list view, select a record and run **Show in document**: the nested value is there. (The
   in-editor **View Merged Data** preview shows nested fields empty; that is a DevExpress limitation, see findings.)
