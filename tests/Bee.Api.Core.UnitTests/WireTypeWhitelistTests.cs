using System.ComponentModel;
using Bee.Api.Core.MessagePack;

namespace Bee.Api.Core.UnitTests
{
    /// <summary>
    /// Covers the assembly-qualified name screen that guards every wire path resolving a type
    /// from a caller-supplied name.
    /// </summary>
    /// <remarks>
    /// The shape under test is generic-argument smuggling: an allowed outer type carrying a
    /// disallowed argument. A screen that splits on the first comma accepts it, because the
    /// argument's comma sits inside <c>[[...]]</c> and therefore comes first.
    /// </remarks>
    public class WireTypeWhitelistTests
    {
        [Theory]
        [InlineData("System.String")]
        [InlineData("System.Int32")]
        [InlineData("System.Byte[]")]
        [InlineData("System.Object[]")]
        [InlineData("System.Data.DataTable")]
        [InlineData("Bee.Definition.Collections.ParameterCollection, Bee.Definition")]
        [InlineData("Bee.Api.Core.Messages.System.LoginRequest, Bee.Api.Core")]
        [InlineData("Bee.Base.Something, Bee.Base, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null")]
        [DisplayName("IsAssemblyQualifiedNameAllowed 應接受白名單內的名稱")]
        public void IsAssemblyQualifiedNameAllowed_AllowedName_ReturnsTrue(string name)
        {
            Assert.True(WireTypeWhitelist.IsAssemblyQualifiedNameAllowed(name));
        }

        [Theory]
        [InlineData("Evil.Namespace.Exploit, Evil.Assembly")]
        [InlineData("System.Diagnostics.Process, System.Diagnostics.Process")]
        [DisplayName("IsAssemblyQualifiedNameAllowed 應拒絕白名單外的名稱")]
        public void IsAssemblyQualifiedNameAllowed_DisallowedName_ReturnsFalse(string name)
        {
            Assert.False(WireTypeWhitelist.IsAssemblyQualifiedNameAllowed(name));
        }

        [Theory]
        // The regression this screen exists for: allowed outer type, disallowed generic argument.
        [InlineData("Bee.Base.Collections.Dictionary`1[[System.Diagnostics.Process, System.Diagnostics.Process]], Bee.Base")]
        [InlineData("Bee.Definition.Wrapper`1[[Evil.Namespace.Exploit, Evil.Assembly]], Bee.Definition")]
        // Two allowed arguments plus one disallowed.
        [InlineData("Bee.Base.Pair`2[[System.String],[Evil.Namespace.Exploit, Evil.Assembly]], Bee.Base")]
        // Disallowed type buried one level deeper.
        [InlineData("Bee.Base.A`1[[Bee.Base.B`1[[Evil.Namespace.Exploit, Evil.Assembly]], Bee.Base]], Bee.Base")]
        // Array forms.
        [InlineData("Evil.Namespace.Exploit[], Evil.Assembly")]
        [InlineData("Bee.Base.Holder`1[[Evil.Namespace.Exploit[], Evil.Assembly]], Bee.Base")]
        [DisplayName("IsAssemblyQualifiedNameAllowed 應拒絕夾帶在泛型參數或陣列元素中的型別")]
        public void IsAssemblyQualifiedNameAllowed_SmuggledArgument_ReturnsFalse(string name)
        {
            Assert.False(WireTypeWhitelist.IsAssemblyQualifiedNameAllowed(name));
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        // Unbalanced brackets, empty segments and stray separators must fail closed rather than
        // be handed to `Type.GetType`.
        [InlineData("Bee.Base.Broken`1[[Bee.Base.Ok, Bee.Base], Bee.Base")]
        [InlineData("Bee.Base.Broken`1[[]], Bee.Base")]
        [InlineData("Bee.Base.Broken`1[, Bee.Base")]
        [InlineData("[[Bee.Base.Ok, Bee.Base]]")]
        [InlineData(", Bee.Base")]
        // Pointer and by-ref forms never appear on this wire.
        [InlineData("Bee.Base.Ok*, Bee.Base")]
        [InlineData("Bee.Base.Ok&, Bee.Base")]
        [DisplayName("IsAssemblyQualifiedNameAllowed 對空值與畸形名稱應 fail-closed")]
        public void IsAssemblyQualifiedNameAllowed_MalformedName_ReturnsFalse(string? name)
        {
            Assert.False(WireTypeWhitelist.IsAssemblyQualifiedNameAllowed(name));
        }

        [Fact]
        [DisplayName("IsAssemblyQualifiedNameAllowed 應拒絕過長的名稱")]
        public void IsAssemblyQualifiedNameAllowed_OverlongName_ReturnsFalse()
        {
            var name = "Bee.Base." + new string('a', 2000);

            Assert.False(WireTypeWhitelist.IsAssemblyQualifiedNameAllowed(name));
        }

        [Fact]
        [DisplayName("IsAssemblyQualifiedNameAllowed 應拒絕巢狀過深的名稱")]
        public void IsAssemblyQualifiedNameAllowed_ExcessiveNesting_ReturnsFalse()
        {
            // Ten levels of nesting, every named type allowed. Depth alone must reject it, so a
            // crafted payload cannot drive unbounded recursion.
            var name = "Bee.Base.Ok, Bee.Base";
            for (var i = 0; i < 10; i++)
            {
                name = $"Bee.Base.Wrap`1[[{name}]], Bee.Base";
            }

            Assert.False(WireTypeWhitelist.IsAssemblyQualifiedNameAllowed(name));
        }

        [Fact]
        [DisplayName("IsRuntimeTypeAllowed 應拒絕帶有不允許泛型參數的具現型別")]
        public void IsRuntimeTypeAllowed_ConstructedGenericWithDisallowedArgument_ReturnsFalse()
        {
            // `Bee.Base.Collections.Dictionary<T>` is an allowed outer type; the argument is not.
            var type = typeof(Bee.Base.Collections.Dictionary<>)
                .MakeGenericType(typeof(global::System.Text.StringBuilder));

            Assert.False(WireTypeWhitelist.IsRuntimeTypeAllowed(type));
        }

        [Fact]
        [DisplayName("IsRuntimeTypeAllowed 應接受泛型參數也在白名單內的具現型別")]
        public void IsRuntimeTypeAllowed_ConstructedGenericWithAllowedArgument_ReturnsTrue()
        {
            var type = typeof(Bee.Base.Collections.Dictionary<>).MakeGenericType(typeof(string));

            Assert.True(WireTypeWhitelist.IsRuntimeTypeAllowed(type));
        }

        [Theory]
        [InlineData(typeof(byte[]))]
        [InlineData(typeof(object[]))]
        [InlineData(typeof(string))]
        [InlineData(typeof(global::System.Data.DataTable))]
        [DisplayName("IsRuntimeTypeAllowed 應接受白名單內的型別")]
        public void IsRuntimeTypeAllowed_AllowedType_ReturnsTrue(Type type)
        {
            Assert.True(WireTypeWhitelist.IsRuntimeTypeAllowed(type));
        }

        [Theory]
        [InlineData(typeof(global::System.Text.StringBuilder))]
        [InlineData(typeof(global::System.Text.StringBuilder[]))]
        [DisplayName("IsRuntimeTypeAllowed 應拒絕白名單外的型別與其陣列")]
        public void IsRuntimeTypeAllowed_DisallowedType_ReturnsFalse(Type type)
        {
            Assert.False(WireTypeWhitelist.IsRuntimeTypeAllowed(type));
        }
    }
}
