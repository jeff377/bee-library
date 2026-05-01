namespace Bee.Definition
{
    /// <summary>
    /// Extension methods for <see cref="DefineType"/>.
    /// </summary>
    public static class DefineTypeExtensions
    {
        private static readonly Dictionary<DefineType, string> DefineTypeNames = new()
        {
            { DefineType.SystemSettings,   "Bee.Definition.Settings.SystemSettings" },
            { DefineType.DatabaseSettings, "Bee.Definition.Settings.DatabaseSettings" },
            { DefineType.DbSchemaSettings, "Bee.Definition.Settings.DbSchemaSettings" },
            { DefineType.ProgramSettings,  "Bee.Definition.Settings.ProgramSettings" },
            { DefineType.TableSchema,      "Bee.Definition.Database.TableSchema" },
            { DefineType.FormSchema,       "Bee.Definition.Forms.FormSchema" },
            { DefineType.FormLayout,       "Bee.Definition.Layouts.FormLayout" },
        };

        /// <summary>
        /// Gets the CLR type for the specified define type.
        /// </summary>
        /// <param name="defineType">The define data type.</param>
        /// <exception cref="NotSupportedException">Thrown when the define type is not registered.</exception>
        public static Type ToClrType(this DefineType defineType)
        {
            if (!DefineTypeNames.TryGetValue(defineType, out string? typeName))
                throw new NotSupportedException($"Type not found: {defineType}");
            var assembly = typeof(DefineTypeExtensions).Assembly;
            var type = assembly.GetType(typeName);
            if (type == null)
                throw new NotSupportedException($"Type not found: {typeName}");
            return type;
        }
    }
}
