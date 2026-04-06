using System;

namespace Bee.Base.Attributes
{
    /// <summary>
    /// Custom attribute applied to a class to describe how the object is presented as a tree node.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class TreeNodeAttribute : Attribute
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of <see cref="TreeNodeAttribute"/>.
        /// </summary>
        public TreeNodeAttribute()
        { }

        /// <summary>
        /// Initializes a new instance of <see cref="TreeNodeAttribute"/> with a display format string.
        /// </summary>
        /// <param name="displayFormat">The display name or format string.</param>
        public TreeNodeAttribute(string displayFormat)
        {
            DisplayFormat = displayFormat;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="TreeNodeAttribute"/> with a format string and a property name.
        /// </summary>
        /// <param name="displayFormat">The display format string.</param>
        /// <param name="propertyName">The property name used to replace <c>{0}</c> in the format string.</param>
        public TreeNodeAttribute(string displayFormat, string propertyName)
        {
            DisplayFormat = displayFormat;
            PropertyName = propertyName;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="TreeNodeAttribute"/> with a display format and a collection folder flag.
        /// </summary>
        /// <param name="displayFormat">The display name or format string.</param>
        /// <param name="collectionFolder">Whether to show a folder node for collection properties.</param>
        public TreeNodeAttribute(string displayFormat, bool collectionFolder)
        {
            DisplayFormat = displayFormat;
            CollectionFolder = collectionFolder;
        }

        #endregion

        /// <summary>
        /// Gets the display format string.
        /// </summary>
        public string DisplayFormat { get; private set; } = string.Empty;

        /// <summary>
        /// Gets the property name used to replace <c>{0}</c> in the format string.
        /// </summary>
        public string PropertyName { get; private set; } = string.Empty ;

        /// <summary>
        /// Gets whether collection properties should display a folder node.
        /// </summary>
        public bool CollectionFolder { get; private set; } = false;

        /// <summary>
        /// Gets the display text for the object that has <see cref="TreeNodeAttribute"/> applied.
        /// </summary>
        /// <param name="value">The object instance.</param>
        public static string GetDisplayText(object value)
        {
            // Get the TreeNodeAttribute from the object
            var attribute = (TreeNodeAttribute)BaseFunc.GetAttribute(value, typeof(TreeNodeAttribute));
            // If no attribute is found, return the object's string representation
            if (attribute == null) { return value.ToString(); }

            string displayText;
            if (StrFunc.IsNotEmpty(attribute.PropertyName))
            {
                // DisplayFormat is a composite format string
                var names = StrFunc.Split(attribute.PropertyName, ",");
                var args = new object[names.Length];
                for (int N1 = 0; N1 < names.Length; N1++)
                    args[N1] = BaseFunc.GetPropertyValue(value, names[N1]);
                displayText = StrFunc.Format(attribute.DisplayFormat, args);
            }
            else
            {
                // DisplayFormat is a literal string
                displayText = attribute.DisplayFormat;
            }

            if (StrFunc.IsEmpty(displayText))
            {
                if (value is IDisplayName)
                    displayText = (value as IDisplayName).DisplayName;
                else
                    displayText = value.ToString();
            }

            return displayText;
        }

    }
}
