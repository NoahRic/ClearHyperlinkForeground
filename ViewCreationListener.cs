using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Diagnostics;

namespace ItalicComments
{
    [Export(typeof(IWpfTextViewCreationListener))]
    [ContentType("code")]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    internal sealed class ViewCreationListener : IWpfTextViewCreationListener
    {
        [Import]
        IClassificationFormatMapService formatMapService = null;

        [Import]
        IClassificationTypeRegistryService typeRegistry = null;

        public void TextViewCreated(IWpfTextView textView)
        {
            textView.Properties.GetOrCreateSingletonProperty(() =>
                     new FormatMapWatcher(textView, formatMapService.GetClassificationFormatMap(textView), typeRegistry));
        }
    }

    internal sealed class FormatMapWatcher
    {
        bool inUpdate = false;
        IClassificationFormatMap formatMap;
        IClassificationTypeRegistryService typeRegistry;
        IClassificationType text;

        public FormatMapWatcher(ITextView view, IClassificationFormatMap formatMap, IClassificationTypeRegistryService typeRegistry)
        {
            this.formatMap = formatMap;
            this.text = typeRegistry.GetClassificationType("text");
            this.typeRegistry = typeRegistry;
            this.ClearUrlForegroundColor();

            this.formatMap.ClassificationFormatMappingChanged += FormatMapChanged;

            view.GotAggregateFocus += FirstGotFocus;
        }
 
        void FirstGotFocus(object sender, EventArgs e)
        {
            ((ITextView)sender).GotAggregateFocus -= FirstGotFocus;

            Debug.Assert(!inUpdate, "How can we be updating *while* the view is getting focus?");

            this.ClearUrlForegroundColor();
        }

        void FormatMapChanged(object sender, System.EventArgs e)
        {
            if (!inUpdate)
                this.ClearUrlForegroundColor();
        }

        internal void ClearUrlForegroundColor()
        {
            try
            {
                inUpdate = true;

                var urlClassificationType = typeRegistry.GetClassificationType("url");
                if (urlClassificationType != null)
                    ClearForeground(urlClassificationType);
            }
            finally
            {
                inUpdate = false;
            } 
        }

        void ClearForeground(IClassificationType classification)
        {
            var properties = formatMap.GetTextProperties(classification);

            // If this is already cleared out, skip it
            if (properties.ForegroundBrushEmpty)
                return;

            formatMap.SetTextProperties(classification, properties.ClearForegroundBrush());
        }
    }
}