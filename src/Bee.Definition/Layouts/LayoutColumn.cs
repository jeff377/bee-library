using System.ComponentModel;
using System.Xml.Serialization;
using Bee.Base.Attributes;

namespace Bee.Definition.Layouts
{
    /// <summary>
    /// A grid layout column.
    /// </summary>
    [Description("Grid layout column.")]
    [TreeNode]
    public class LayoutColumn : LayoutFieldBase
    {
        /// <summary>
        /// Initializes a new instance of <see cref="LayoutColumn"/>.
        /// </summary>
        public LayoutColumn()
        { }

        /// <summary>
        /// Initializes a new instance of <see cref="LayoutColumn"/>.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <param name="caption">The caption text.</param>
        /// <param name="controlType">The control type.</param>
        public LayoutColumn(string fieldName, string caption, ControlType controlType)
        {
            FieldName = fieldName;
            Caption = caption;
            ControlType = controlType;
        }

        /// <summary>
        /// Gets or sets the column width in pixels. Zero means auto/unset (UI framework decides default).
        /// </summary>
        [Category(PropertyCategories.Layout)]
        [XmlAttribute]
        [Description("Column width in pixels. Zero means auto/unset.")]
        [DefaultValue(0)]
        public int Width { get; set; } = 0;

        /// <summary>
        /// Creates a fully independent copy of this column.
        /// </summary>
        /// <returns>A new <see cref="LayoutColumn"/> sharing no mutable state with this one.</returns>
        public LayoutColumn Clone()
        {
            var copy = new LayoutColumn { Width = Width };
            CopyBaseTo(copy);
            return copy;
        }
    }
}
