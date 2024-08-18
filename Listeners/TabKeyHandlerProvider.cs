using System.ComponentModel.Composition;

using EnvDTE;

using EnvDTE80;

using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;

namespace TabOut.Listeners
{
    [Export(typeof(IVsTextViewCreationListener))]
    [ContentType("text")]
    [TextViewRole(PredefinedTextViewRoles.Editable)]
    internal class TabKeyHandlerProvider : IVsTextViewCreationListener
    {
        [Import]
        internal IVsEditorAdaptersFactoryService AdapterService = null;

        [Import]
        internal SVsServiceProvider ServiceProvider = null;

        [Import]
        internal ICompletionBroker CompletionBroker = null;

        public void VsTextViewCreated(IVsTextView textViewAdapter)
        {
            IWpfTextView view = AdapterService.GetWpfTextView(textViewAdapter);
            if (view == null)
                return;

            // 添加命令过滤器
            var commandFilter = new MyCommandFilter(ServiceProvider.GetService<DTE, DTE2>(), view, CompletionBroker);
            textViewAdapter.AddCommandFilter(commandFilter, out var nextCommandTarget);
            commandFilter.NextCommandTarget = nextCommandTarget;
        }
    }
}
