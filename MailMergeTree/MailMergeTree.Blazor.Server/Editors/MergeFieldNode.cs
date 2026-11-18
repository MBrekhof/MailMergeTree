using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Utils;

namespace MailMergeTree.Blazor.Server.Editors;

// One member in the merge-field tree. Same rules as XAF's WinForms "Object Model" tree (ModelBrowserAdapter with
// ShowCollection = false), which is Win-only and cannot be reused here.
public class MergeFieldNode {
    private readonly IMemberInfo member;
    private List<MergeFieldNode> children;

    private MergeFieldNode(IMemberInfo member, string parentPath) {
        this.member = member;
        Path = parentPath.Length == 0 ? member.Name : parentPath + "." + member.Name;
    }

    // Caption from the Application Model.
    public string Caption => CaptionHelper.GetLastMemberPartCaption(member.Owner.Type, member.Name);
    // Dotted member path, e.g. "Debtor.Address.City".
    public string Path { get; }
    // Built on first access, so only one level ahead of what the user expanded ever exists (cyclic models are safe).
    public List<MergeFieldNode> Children => children ??= member.IsList ? new() : For(member.MemberTypeInfo, Path);
    public bool HasChildren => Children.Count > 0;
    // Only plain fields can be inserted: a reference node itself merges as the object's default text.
    public bool IsField => !HasChildren && !member.MemberTypeInfo.IsDomainComponent;

    public static List<MergeFieldNode> For(ITypeInfo type, string parentPath = "") =>
        type.Members.Where(IsDisplayed).Select(m => new MergeFieldNode(m, parentPath)).ToList();

    private static bool IsDisplayed(IMemberInfo m) =>
        m.Owner.KeyMember == m
        || (m.IsPublic && m.IsVisible
            && (!m.IsList || ModelNodesGeneratorBase.IsBinaryImage(m) || ModelNodesGeneratorBase.IsRichEditPropertyEditorAlias(m))
            && (m.MemberTypeInfo.IsDomainComponent || m.Owner.IsDomainComponent));
}
