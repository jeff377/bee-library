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
        /// <param name="models">The owning permission model registry.</param>
        public PermissionModelCollection(PermissionModels models) : base(models)
        { }

        /// <summary>
        /// Adds a permission model to the collection.
        /// </summary>
        /// <param name="modelId">The model id.</param>
        /// <param name="displayName">The display name.</param>
        public PermissionModel Add(string modelId, string displayName)
        {
            var model = new PermissionModel(modelId, displayName);
            base.Add(model);
            return model;
        }
    }
}
