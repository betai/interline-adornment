using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using System.Windows;
using System.Diagnostics;

namespace SanityTest
{
    public class SuggestionTag : InterLineAdornmentTag
    {
        internal readonly SuggestionInterLineAdornmentTaggerProvider Factory;
        internal readonly SuggestionSession Session;
        public readonly ITrackingPoint Location;

        //private SuggestionControl _adornment;
        private UIElement _adornment;
        //private SuggestionSessionViewModel _sessionViewModel;
        private int _promoting;

        internal SuggestionTag(SuggestionInterLineAdornmentTaggerProvider factory,
                         SuggestionSession session,
                         InterLineAdornmentFactory adornmentFactory,
                         AdornmentRemovedCallback removalCallback)
            : base(adornmentFactory: adornmentFactory,
                   isAboveLine: false,
                   initialHeight: 1.0,
                   horizontalPositioningMode: HorizontalPositioningMode.ViewRelative,
                   horizontalOffset: 0.0,
                   removalCallback: removalCallback)
        {
            if (factory == null)
                throw new ArgumentNullException("factory");
            //if (session == null)
            //    throw new ArgumentNullException("session");

            this.Factory = factory;
            this.Session = session;
            this.Location = session.GetTriggerPoint(session.TextView.TextBuffer);
        }

        public UIElement GetOrCreateAdornment(IWpfTextView textView)
        {
            //Debug.Assert(textView == this.Session.TextView);

            if (_adornment == null)
            {
                //_sessionViewModel = new SuggestionSessionViewModel(this);
                //_adornment = new SuggestionControl(this, textView);//, _sessionViewModel);
                //_adornment = new TestButton();
                _adornment = new SuggestionControl();
            }

            return _adornment;
        }

        //public SuggestionControl SuggestionControl { get { return _adornment; } }
        public UIElement SuggestionControl { get { return _adornment; } }

        internal void SetPromoting(bool promoting)
        {
            _promoting += promoting ? 1 : -1;
            Debug.Assert(_promoting >= 0, "Unbalanced calls to PeekTag.SetPromoting");
        }

        internal bool IsPromoting
        {
            get
            {
                return _promoting > 0;
            }
        }

        //internal SuggestionSessionViewModel SuggestionSessionViewModel
        //{
        //    get
        //    {
        //        return _sessionViewModel;
        //    }
        //}

        public void Dismiss()
        {
            if (_adornment != null)
            {
                //Debug.Assert(_sessionViewModel != null);

                // Return focus to the containing view if dismissed adornment had focus.
                // VS bug 103080: It's not safe to check focus during promoting because the new
                // window frame might not have taken focus yet (if its contents haven't been loaded)
                if (!this.IsPromoting && _adornment.IsKeyboardFocusWithin)
                {
                    var containingView = Session.TextView as IWpfTextView;
                    if (containingView != null && !containingView.IsClosed)
                    {
                        containingView.VisualElement.Focus();
                    }
                }

                //_adornment.Dismiss();
                _adornment = null;

                //_sessionViewModel.Dispose();
                //_sessionViewModel = null;
            }
        }
    }
}