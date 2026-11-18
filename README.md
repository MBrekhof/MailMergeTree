# MailMergeTree

A hierarchical field picker for the XAF Office Module mail merge template editor, for WinForms and for Blazor.

Out of the box, **Insert Merge Field** shows a flat list of the Data Type's top-level properties. Nested values such as
`Debtor.Attention` have to be typed by hand through *Show All Field Codes*. This sample replaces the flat list with a
tree: the template's Data Type is the root, reference properties expand lazily over any number of levels, captions come
from the Application Model, and the chosen path is inserted as a `MERGEFIELD` at the caret.

![Field tree opened from Insert Merge Field](docs/images/field-tree.png)

View Merged Data resolves the inserted subfields (here `Debtor.Attention` and `Debtor.Address.City`):

![Merged result](docs/images/merged-result.png)

The Blazor version (its own project, no Middle Tier) does the same in the browser:

![Blazor field tree](docs/images/blazor-field-tree.png)

![Blazor merged result](docs/images/blazor-merged-result.png)

## What is in here

| Path | What |
| --- | --- |
| `MailMergeTree/MailMergeTree.Blazor.Server/Controllers/MailMergeFieldTreeController.cs` | Blazor: replaces the ribbon button's action and inserts the field |
| `MailMergeTree/MailMergeTree.Blazor.Server/Editors/MergeFieldTreePopup.razor` (+ `MergeFieldNode.cs`, `MergeFieldTreePopupModel.cs`) | Blazor: the tree popup |
| [docs/how-to-implement-blazor.md](docs/how-to-implement-blazor.md) | Step by step: add this to your own XAF Blazor app |
| `MailMergeTree/MailMergeTree.Win/Controllers/MailMergeFieldTreeController.cs` | Hooks both halves of the ribbon button and inserts the field |
| `MailMergeTree/MailMergeTree.Win/Controllers/MergeFieldTreeForm.cs` | The tree dialog, built from XAF's own Object Model parts |
| `MailMergeTree/MailMergeTree.Module/BusinessObjects/DemoObjects.cs` | Demo model: Invoice -> Debtor -> Address, Supplier |
| [docs/how-to-implement.md](docs/how-to-implement.md) | Step by step: add this to your own XAF WinForms app |
| [docs/findings.md](docs/findings.md) | What DevExpress offers, which extension points exist, what the spike does not cover |

Stack: XAF 26.1, EF Core, .NET 10; WinForms with Middle Tier security, Blazor Server with integrated security. The
pickers only use XAF type info and the rich text editor, so they are the same for XPO.

## Run it

Needs DevExpress 26.1 (NuGet feed configured) and SQL Server LocalDB. Build once:

    dotnet build C:\Projects\MailMergeTree\MailMergeTree.slnx

Then, in PowerShell (no admin), one line each, middle tier first. The LocalDB database `MailMergeTree` is created and
seeded on first start.

    Start-Process "C:\Projects\MailMergeTree\MailMergeTree\MailMergeTree.MiddleTier\bin\Debug\net10.0\MailMergeTree.MiddleTier.exe" -WorkingDirectory "C:\Projects\MailMergeTree\MailMergeTree\MailMergeTree.MiddleTier"
    Start-Process "C:\Projects\MailMergeTree\MailMergeTree\MailMergeTree.Win\bin\Debug\net10.0-windows\MailMergeTree.Win.exe"

Log in as `Admin` (empty password), open **Reports > Mail Merge Template**, create a template with Data Type
**Invoice**, click into the editor and press **Insert Merge Field**. Double-click a field, or select it and press
**Insert**. One invoice (`INV-001`) with a debtor, address and supplier is seeded, so **View Merged Data** shows values.

### Blazor

The Blazor app talks to the same LocalDB database directly; the Middle Tier does not have to run. PowerShell, no admin:

    dotnet run --project C:\Projects\MailMergeTree\MailMergeTree\MailMergeTree.Blazor.Server --launch-profile MailMergeTree.Blazor.Server

Open https://localhost:5001, log in as `Admin` (empty password), open **Reports > Mail Merge Template**, open or create
a template with Data Type **Invoice**, go to the editor's **Mail Merge** tab and press **Insert Merge Field**. To see the
merged result: **Invoice** list, tick `INV-001`, **Show in document**. Stop the app with Ctrl+C.

## Limits

Blazor:

- **View Merged Data inside the template editor shows nested fields empty.** The DevExpress Blazor editor sends only
  the top-level columns of the data source to the browser for that preview. The real merge (**Show in document**)
  resolves them. See [docs/findings.md](docs/findings.md).
- The field goes in at the caret; a selected range is not replaced. No double-click insert.
- Ribbon mode only (the XAF default); toolbar bar mode keeps the flat dialog.

WinForms:

- In Bars menu mode (no ribbon) the drop-down arrow keeps the flat list; the button itself opens the tree.
- `ModelBrowserAdapter` and `ObjectTreeList` are public but undocumented DevExpress classes and can change between versions.
- Verified with the tabbed MDI UI and the default skin only.

Both: the tree lists members from type info; member-level read permissions are not applied to it.
