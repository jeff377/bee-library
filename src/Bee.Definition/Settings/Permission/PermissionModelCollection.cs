using System.ComponentModel;
using Bee.Base.Attributes;
using Bee.Base.Collections;

namespace Bee.Definition.Settings
{
    /// <summary>
    /// A collection of permission models, keyed by model id.
    /// </summary>
    [Description("Permission model collection.")]
    [TreeNode("Models", false)]
    public class PermissionModelCollection : KeyCollectionBase<PermissionModel>
    {
        /// <summary>
        /// Initializes a new instance of <see cref="PermissionModelCollection"/>.
        /// </summary>
        /// <remarks>
        /// Required by XmlSerializer's reflection-only deserialization path (AOT targets such as iOS
        /// create the collection via the public parameterless constructor).
        /// </remarks>
        public PermissionModelCollection() : base()
        { }

        /// <summary>
        /// Initializes a new instance of <see cref="PermissionModelCollection"/>.
        /// </summary>
        /// <param name="models">The owning permission model registry.</param>
        public PermissionModelCollection(PermissionModels models) : base(models)
        { }
    }

    /// <summary>
    /// Extension methods for <see cref="PermissionModelCollection"/>.
    /// </summary>
    public static class PermissionModelCollectionExtensions
    {
        /// <summary>
        /// Adds a permission model to the collection.
        /// </summary>
        /// <param name="collection">The collection to add to.</param>
        /// <param name="modelId">The model id.</param>
        /// <param name="displayName">The display name.</param>
        public static PermissionModel Add(this PermissionModelCollection? collection, string modelId, string displayName)
        {
            ArgumentNullException.ThrowIfNull(collection);
            var model = new PermissionModel(modelId, displayName);
            collection.Add(model);
            return model;
        }
    }
}
