using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;

namespace SanityTest
{
    //[Export(typeof(ILineTransformSourceProvider))]
    [ContentType("text")]
    [TextViewRole(PredefinedTextViewRoles.Interactive)]
    internal sealed class InterLineAdornmentManagerFactory : ILineTransformSourceProvider
    {
        //[Export]
        [Name(PredefinedAdornmentLayers.InterLine)]
        [Order(After = PredefinedAdornmentLayers.Selection, Before = PredefinedAdornmentLayers.Squiggle)]
        internal AdornmentLayerDefinition adornmentLayer;

        //[Import]
        internal IViewTagAggregatorFactoryService TagAggregatorFactoryService { get; set; }

        public ILineTransformSource Create(IWpfTextView textView)
        {
            return InterLineAdornmentManager.Create(textView, this);
        }
    }
}
