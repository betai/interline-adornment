using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;

namespace SanityTest
{

    internal class SuggestionInterLineAdornmentTaggerProvider : IViewTaggerProvider
    {
        public ITagger<T> CreateTagger<T>(ITextView textView, ITextBuffer buffer) where T : ITag
        {
            if (textView.TextBuffer == buffer)
            {
                //return SuggestionInterLineAdornmentTagger.GetorCreate(this, textView) as ITagger<T>;
                return null; // TODO: eventually replace with this ^
            }

            return null;
        }
    }
}

