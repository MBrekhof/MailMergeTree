# How to add the merge-field tree to your own XAF WinForms app

Written against XAF 26.1. Everything goes into the **WinForms project**; no changes to the module, the business
classes or the Middle Tier are needed. It works the same for XPO and EF Core.

Prerequisite: mail merge already works in your app, that is the Office module is added with
`RichTextMailMergeDataType` set and you can open **Reports > Mail Merge Template**
([Mail Merge docs](https://docs.devexpress.com/eXpressAppFramework/400006)).

## 1. Copy two files

From `MailMergeTree/MailMergeTree.Win/Controllers/` into your WinForms project, and change the namespace:

- `MergeFieldTreeForm.cs` - the dialog
- `MailMergeFieldTreeController.cs` - the controller

XAF finds the controller by itself (it is a public `Controller` in a module assembly); there is nothing to register.
The WinForms project already references everything they use (`DevExpress.ExpressApp.Win`,
`DevExpress.ExpressApp.Office.Win`).

That is the whole installation. The rest of this page explains what the two files do, so you can adapt them.

## 2. How the tree is built (`MergeFieldTreeForm`)

XAF ships the parts of its own "Object Model" dialog as public classes:

```csharp
tree = new ObjectTreeList(new ModelBrowserAdapter { ShowCollection = false, RootValue = typeInfo })
{
    Dock = DockStyle.Fill,
    BuildChildNodesRecursive = false   // children are created when a node is expanded
};
tree.DataSource = typeInfo;            // ITypeInfo of the template's Data Type
```

- `DevExpress.ExpressApp.Win.Core.ModelBrowserAdapter` supplies the members of an `ITypeInfo` and their captions
  (through `CaptionHelper`, so Application Model captions and localization apply). `ShowCollection = false` hides
  collection properties, which cannot be merged as a single field.
- `DevExpress.ExpressApp.Win.Controls.ObjectTreeList` is the tree control. With `BuildChildNodesRecursive = false`
  it loads lazily, so circular references (Invoice -> Debtor -> Invoices -> ...) are not a problem.

Each node carries its `IMemberInfo` in `ObjectTreeListNode.Object`. The field path is the member names from the
root down, joined with dots:

```csharp
for (; node is ObjectTreeListNode { Object: IMemberInfo member }; node = node.ParentNode)
    names.Push(member.Name);
return string.Join(".", names);        // "Debtor.Address.City"
```

Use `member.Name`, never the node's display text: the display text is the caption.

Only plain fields can be inserted. A reference node such as `Debtor` would merge as the object's default text, which
is the problem this picker solves:

```csharp
node.ParentNode != null && !node.HasChildren && !member.MemberTypeInfo.IsDomainComponent
```

Check `HasChildren`, not `Nodes.Count`: with lazy loading an unexpanded reference node has no child nodes yet.

## 3. How the ribbon button is hooked (`MailMergeFieldTreeController`)

The ribbon item (`DevExpress.XtraRichEdit.UI.InsertMergeFieldItem`) is a split button, and its two halves take
different routes. Both must be handled.

**Button half** runs the `ShowInsertMergeFieldForm` command, which raises an event before showing the built-in form:

```csharp
richEdit.InsertMergeFieldFormShowing += (s, e) => { e.Handled = true; ShowPicker(); };
```

`richEdit` is the `RichEditControl` of the template editor:
`((RichTextPropertyEditor)View.FindItem(nameof(IRichTextMailMergeData.Template))).RichEditControl`. It can still be
null in `OnViewControlsCreated`; in that case subscribe in the editor's `ControlCreated` event.

**Arrow half** never runs that command. It opens the item's `PopupMenu` with the flat list. Cancel the popup and
show the picker instead. The item is reachable when XAF creates the rich edit ribbon:

```csharp
// OnActivated
Frame.GetController<MenuManagerController>().CustomizeRibbonControl += (s, e) =>
{
    foreach (InsertMergeFieldItem item in e.RibbonControl.Items.OfType<InsertMergeFieldItem>())
        if (item.DropDownControl is PopupMenu menu)
            menu.BeforePopup += InsertMergeFieldMenu_BeforePopup;
};

void InsertMergeFieldMenu_BeforePopup(object sender, CancelEventArgs e)
{
    e.Cancel = true;
    if (richEdit != null && richEdit.IsHandleCreated) richEdit.BeginInvoke(new Action(ShowPicker));
}
```

`BeginInvoke` lets the ribbon finish closing its drop-down before a modal form opens. Subscribe in `OnActivated`:
the ribbon is created during control creation, after activation. The real controller uses named handlers and
removes all of them in `OnDeactivated`; the lambdas above are shortened for reading.

## 4. Inserting the field

```csharp
Type dataType = ViewCurrentObject?.DataType;          // read at click time: the user can change Data Type
using var form = new MergeFieldTreeForm(Application.TypesInfo.FindTypeInfo(dataType));
if (form.ShowDialog(richEdit.FindForm()) != DialogResult.OK) return;
if (richEdit.CreateCommand(RichEditCommandId.InsertMailMergeField) is InsertMergeFieldCommand command)
{
    command.FieldArgument = form.SelectedPath;
    command.Execute();
}
```

Create the command through `richEdit.CreateCommand(...)` rather than `new InsertMergeFieldCommand(...)`. XAF's
`MailMergeController` has replaced the command factory, and its command inserts image properties as
`INCLUDEPICTURE` instead of `MERGEFIELD`. Going through the command also gives undo/redo.

## 5. Things you may want to change

- **Hide fields.** The adapter already skips non-public and non-visible members. To filter further (for example
  `ID`, or members the user may not read), subclass `ModelBrowserAdapter` and override `GetChildren` /
  `HasChildren`, or set `CheckForHideInUIValue`.
- **Member-level security.** Not applied in the sample. Filter in the adapter subclass with
  `Application.GetSecurityStrategy()` if users must not see field names they cannot read.
- **Keep the dialog open.** The sample closes after one field. Showing the form non-modal and inserting on every
  double-click gives a Field List feel; then the form's lifetime must be tied to the view.
- **Bars menu mode.** If your rich text editors use `MenuManagerType.Bars`, the arrow's flat list comes from a
  `BarManager` created in `MenuManagerController.CreateBars`, which has no customization event; the button half
  still opens the tree.

## 6. Check it

1. Open a template, pick a Data Type that has reference properties, click into the editor.
2. Click the top half of **Insert Merge Field**: the tree opens. Click the arrow: the tree opens, not the flat list.
3. Expand a reference, double-click a field: `«Debtor.Attention»` appears at the caret.
4. **View Merged Data** shows the value.
5. Open a second template with another Data Type in a second tab and repeat: each tab must show its own type.
