using Bee.Api.Contracts.System;
using Bee.Definition.Language;

namespace Bee.Business.System
{
    /// <summary>
    /// Output result for retrieving a language resource as a typed object.
    /// </summary>
    public class GetLanguageResult : BusinessResult, IGetLanguageResponse
    {
        /// <summary>
        /// Gets or sets the language resource as a typed object (serialised as
        /// a JSON tree on the Plain wire format).
        /// </summary>
        public LanguageResource? Resource { get; set; }
    }
}
