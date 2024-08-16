using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;

using System.IO;
using System.Threading.Tasks;

namespace TabOut
{
    [Command(PackageIds.Tab)]
    internal sealed class Tab : BaseCommand<Tab>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            var dte = await Package.GetServiceAsync<DTE, DTE2>();
            int moveCount = default;
            IWpfTextView view = default;
            if (dte.ActiveDocument is Document document && Path.GetExtension(document.FullName) == ".cs") //仅对c#生效
            {
                view = await getTextViewAsync(Package);
                var model = await Package.GetServiceAsync<SComponentModel, IComponentModel>();
                var broker = model.GetService<ICompletionBroker>();
                if (!broker.IsCompletionActive(view)) //自动补全不能在活动状态
                {

                    var line = view.Caret.Position.BufferPosition.GetContainingLine();
                    var linePosition = view.Caret.Position.BufferPosition.Position - line.Start;
                    var lineContent = line.GetText();
                    if (linePosition < lineContent.Length) //光标位置在行内
                    {
                        if (lineContent.Substring(0, linePosition).Trim() != string.Empty) //光标不在有效数据的前面
                        {
                            var caretChar = lineContent[linePosition];
                            int mc = 1;

                            while (caretChar == ' ') //光标位置是空格，我们要往后继续找
                            {
                                mc++;
                                linePosition++;
                                if (linePosition >= lineContent.Length)
                                    break;
                                caretChar = lineContent[linePosition];
                            }

                            if (caretChar is '{' or '[' or '(' or ')' or ']' or '}' or '"' or '\'' or '.' or ';' or ',') //现在光标位置是目标字符，可以做标记了
                                moveCount = mc;
                        }
                    }
                }
            }

            if (moveCount > 0)
            {
                view.Caret.MoveTo(view.Caret.Position.BufferPosition + moveCount);
            }
            else
            {
                dte.ExecuteCommand("Edit.InsertTab");
            }
        }

        private async ValueTask<IWpfTextView> getTextViewAsync(AsyncPackage asyncPackage)
        {
            IVsTextManager textManager = await asyncPackage.GetServiceAsync<SVsTextManager, IVsTextManager>();
            textManager.GetActiveView(1, null, ppView: out IVsTextView textView);
            IVsUserData userData = textView as IVsUserData;
            if (userData == null) return null;
            Guid guidViewHost = Microsoft.VisualStudio.Editor.DefGuidList.guidIWpfTextViewHost;
            userData.GetData(ref guidViewHost, out object holder);
            IWpfTextViewHost viewHost = (IWpfTextViewHost)holder;
            return viewHost?.TextView;
        }
    }
}
