using System.ComponentModel;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;

namespace MailMergeTree.Module.BusinessObjects
{
    // Demo model for the merge-field tree: Invoice -> Debtor -> Address gives two levels of nesting.
    // ponytail: no explicit FK properties, they would only show up as noise in the field tree; add when the demo grows real edits.
    [DefaultClassOptions]
    [DefaultProperty(nameof(Number))]
    public class Invoice : BaseObject
    {
        public virtual string Number { get; set; }
        public virtual DateTime Date { get; set; }
        public virtual decimal Amount { get; set; }
        public virtual Debtor Debtor { get; set; }
        public virtual Supplier Supplier { get; set; }
    }

    [DefaultClassOptions]
    [DefaultProperty(nameof(Name))]
    public class Debtor : BaseObject
    {
        public virtual string Name { get; set; }
        public virtual string Attention { get; set; }
        public virtual string Email { get; set; }
        public virtual Address Address { get; set; }
    }

    [DefaultClassOptions]
    [DefaultProperty(nameof(Name))]
    public class Supplier : BaseObject
    {
        public virtual string Name { get; set; }
        public virtual string ContactPerson { get; set; }
    }

    [DefaultProperty(nameof(City))]
    public class Address : BaseObject
    {
        public virtual string Street { get; set; }
        public virtual string City { get; set; }
    }
}
