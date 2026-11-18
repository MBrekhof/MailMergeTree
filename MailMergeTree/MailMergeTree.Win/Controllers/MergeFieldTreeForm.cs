using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Win.Controls;
using DevExpress.ExpressApp.Win.Core;
using DevExpress.XtraEditors;
using DevExpress.XtraTreeList;
using DevExpress.XtraTreeList.Nodes;

namespace MailMergeTree.Win.Controllers
{
    // Field tree built from XAF's own "Object Model" parts: ModelBrowserAdapter (members + Application Model
    // captions) on an ObjectTreeList (children are created when a node is expanded).
    public class MergeFieldTreeForm : XtraForm
    {
        private readonly ObjectTreeList tree;
        private readonly SimpleButton okButton;

        public MergeFieldTreeForm(ITypeInfo typeInfo)
        {
            Text = "Insert Merge Field";
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(420, 480);
            MinimizeBox = false;
            MinimumSize = new Size(320, 300); // resizable, so deep paths stay readable
            ShowInTaskbar = false;

            tree = new ObjectTreeList(new ModelBrowserAdapter { ShowCollection = false, RootValue = typeInfo })
            {
                Dock = DockStyle.Fill,
                BuildChildNodesRecursive = false
            };
            tree.OptionsBehavior.Editable = false;
            tree.OptionsBehavior.AllowExpandOnDblClick = true;
            tree.OptionsView.ShowIndicator = false;
            tree.OptionsView.ShowHorzLines = false;
            tree.OptionsView.ShowVertLines = false;
            tree.OptionsView.ShowColumns = false;
            tree.OptionsFind.AllowIncrementalSearch = true;
            tree.DataSource = typeInfo;
            if (tree.Columns.Count > 0)
            {
                tree.Columns[0].UnboundType = DevExpress.XtraTreeList.Data.UnboundColumnType.String;
            }

            okButton = new SimpleButton { Text = "Insert", DialogResult = DialogResult.OK, Size = new Size(90, 28), Location = new Point(222, 8), Anchor = AnchorStyles.Top | AnchorStyles.Right };
            var cancelButton = new SimpleButton { Text = "Cancel", DialogResult = DialogResult.Cancel, Size = new Size(90, 28), Location = new Point(320, 8), Anchor = AnchorStyles.Top | AnchorStyles.Right };
            var buttons = new PanelControl { Dock = DockStyle.Bottom, Height = 44, BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder, Width = 420 };
            buttons.Controls.Add(okButton);
            buttons.Controls.Add(cancelButton);
            Controls.Add(tree);
            Controls.Add(buttons);
            AcceptButton = okButton;
            CancelButton = cancelButton;

            tree.FocusedNodeChanged += (s, e) => UpdateOkButton();
            tree.MouseDoubleClick += Tree_MouseDoubleClick;
            UpdateOkButton();
        }

        // Dotted member path of the focused node, e.g. "Debtor.Address.City"; empty for the root.
        public string SelectedPath => GetPath(tree.FocusedNode);

        // Only plain fields can be inserted: a reference node itself merges as the object's default text.
        private static bool IsField(TreeListNode node) =>
            node is ObjectTreeListNode { Object: IMemberInfo member } && node.ParentNode != null && !node.HasChildren
            && !member.MemberTypeInfo.IsDomainComponent; // a reference whose class shows no members is still not a field

        private static string GetPath(TreeListNode node)
        {
            var names = new Stack<string>();
            for (; node is ObjectTreeListNode { Object: IMemberInfo member }; node = node.ParentNode)
            {
                names.Push(member.Name);
            }
            return string.Join(".", names);
        }

        private void UpdateOkButton() => okButton.Enabled = IsField(tree.FocusedNode);

        private void Tree_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            TreeListHitInfo hit = tree.CalcHitInfo(e.Location);
            if (e.Button == MouseButtons.Left && hit.Node != null && IsField(hit.Node))
            {
                tree.FocusedNode = (ObjectTreeListNode)hit.Node;
                DialogResult = DialogResult.OK;
            }
        }
    }
}
