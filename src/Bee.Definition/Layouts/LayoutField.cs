using System.ComponentModel;
using System.Xml.Serialization;
using Bee.Base.Attributes;

namespace Bee.Definition.Layouts
{
    /// <summary>
    /// A field placed in a master <see cref="LayoutSection"/>.
    /// </summary>
    [Description("Layout field.")]
    [TreeNode]
    public class LayoutField : LayoutFieldBase
    {
        private int _rowSpan = 1;
        private int _columnSpan = 1;

        /// <summary>
        /// Gets or sets the number of rows to span.
        /// </summary>
        [Category(PropertyCategories.Layout)]
        [XmlAttribute]
        [Description("Number of rows to span.")]
        [DefaultValue(1)]
        public int RowSpan
        {
            get { return _rowSpan; }
            set
            {
                if (value < 1) { value = 1; }
                _rowSpan = value;
            }
        }

        /// <summary>
        /// Gets or sets the number of columns to span.
        /// </summary>
        [Category(PropertyCategories.Layout)]
        [XmlAttribute]
        [Description("Number of columns to span.")]
        [DefaultValue(1)]
        public int ColumnSpan
        {
            get { return _columnSpan; }
            set
            {
                if (value < 1) { value = 1; }
                _columnSpan = value;
            }
        }

        /// <summary>
        /// Gets or sets the form modes in which this field is editable. Combined
        /// with <see cref="LayoutFieldBase.ReadOnly"/>, which forces read-only in
        /// every mode regardless of these flags.
        /// </summary>
        [Category(PropertyCategories.Appearance)]
        [XmlAttribute]
        [Description("Form modes in which this field is editable.")]
        [DefaultValue(FormEditModes.All)]
        public FormEditModes AllowEditModes { get; set; } = FormEditModes.All;
    }
}
