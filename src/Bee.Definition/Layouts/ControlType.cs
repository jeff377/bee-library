namespace Bee.Definition.Layouts
{
    /// <summary>
    /// Control type.
    /// </summary>
    public enum ControlType
    {
        /// <summary>
        /// Automatically determined.
        /// </summary>
        Auto,
        /// <summary>
        /// Text edit box.
        /// </summary>
        TextEdit,
        /// <summary>
        /// Button edit box.
        /// </summary>
        ButtonEdit,
        /// <summary>
        /// Date input box.
        /// </summary>
        DateEdit,
        /// <summary>
        /// Year-month input box.
        /// </summary>
        YearMonthEdit,
        /// <summary>
        /// Drop-down list.
        /// </summary>
        DropDownEdit,
        /// <summary>
        /// Memo (multi-line text) input box.
        /// </summary>
        MemoEdit,
        /// <summary>
        /// Check box.
        /// </summary>
        CheckEdit,
        /// <summary>
        /// Numeric input box: culture-aware parsing, right-aligned, and formatted on blur
        /// per the field's <c>NumberFormat</c> while editing at full precision on focus.
        /// </summary>
        NumericEdit
    }
}
