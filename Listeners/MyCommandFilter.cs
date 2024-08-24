using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;
using System.Windows.Media;

using EnvDTE;

using EnvDTE80;

using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Text.Editor;

using TabOut.Utilities;

namespace TabOut.Listeners
{
    internal class MyCommandFilter : IOleCommandTarget
    {
        readonly static Guid EditGuid = new Guid("1496A755-94DE-11D0-8C3F-00C04FC2AAE2");
        readonly static MatchedBrackets[] Brackets = [
            new('[',']'),
            new('{','}'),
            new('(',')'),
            new('\'','\''),
            new('"','"'),
            new(':',':'),
            new('=','='),
            new('<', '>'),
            new('>', '<'),
            new('.','.'),
            new('`','`'),
            new(';',';'),
            new(',',','),
            ];
        readonly DTE2 Dte;
        readonly IWpfTextView View;
        readonly ICompletionBroker CompletionBroker;
        public MyCommandFilter(DTE2 dte, IWpfTextView view, ICompletionBroker completionBroker)
        {
            Dte = dte;
            View = view;
            CompletionBroker = completionBroker;
        }

        public IOleCommandTarget NextCommandTarget { get; set; }

        public int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
        {
            // 在这里处理命令状态
            return NextCommandTarget.QueryStatus(ref pguidCmdGroup, cCmds, prgCmds, pCmdText);
        }

        public int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            int moveCount = default;
            if (pguidCmdGroup == EditGuid && nCmdID == 4) //这个命令是Edit.InsertTab命令
            {
                if (Dte.ActiveDocument is Document document) //对所有文件生效
                {
                    if (!CompletionBroker.IsCompletionActive(View) && !isIntelliCodeActive(View.VisualElement)) //代码自动完成不能在活动状态
                    {
                        var line = View.Caret.Position.BufferPosition.GetContainingLine();
                        var linePosition = View.Caret.Position.BufferPosition.Position - line.Start.Position;
                        var lineContent = line.GetText();
                        if (linePosition < lineContent.Length) //光标位置在行内
                        {
                            if (linePosition > 0 && lineContent.Substring(0, linePosition).Trim().Length > 0) //要求光标位于有效数据中
                            {
                                var nextChar = lineContent[linePosition];
                                var b = Array.Find(Brackets, b => b.Open == nextChar || b.Close == nextChar);
                                if (b.Open != default) //字符普通匹配模式
                                {
                                    moveCount = 1;
                                }
                                else //判断其他模式
                                {
                                    var previousChar = lineContent[linePosition - 1];
                                    //判断开头匹配模式
                                    b = Array.Find(Brackets, b => b.Open == previousChar);
                                    if (b.Open != default) //开头匹配模式
                                    {
                                        //要从开始位置后找结尾位置
                                        var count = 0;
                                        bool find = false;
                                        while (linePosition + count < lineContent.Length)
                                        {
                                            var curChar = lineContent[linePosition + count];
                                            if (curChar == b.Close)
                                            {
                                                find = true;
                                                break;
                                            }

                                            count++;
                                        }

                                        if (find)
                                            moveCount = count;
                                    }

                                    if (moveCount == 0) //空格匹配结尾模式
                                    {
                                        var caretChar = nextChar;
                                        var count = 0;
                                        while (caretChar == ' ' && linePosition + count < lineContent.Length) //光标位置是空格，我们要往后继续找
                                        {
                                            caretChar = lineContent[linePosition + count];
                                            count++;
                                        }

                                        if (Array.FindIndex(Brackets, b => b.Open == caretChar || b.Close == caretChar) >= 0) //现在光标位置是目标字符，可以做标记了
                                            moveCount = count;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            if (moveCount > 0)
            {
                View.Caret.MoveTo(View.Caret.Position.BufferPosition + moveCount);
                return 0;
            }

            // 在这里处理命令执行
            return NextCommandTarget.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
        }

        record struct MatchedBrackets(char Open, char Close);

        static bool isIntelliCodeActive(DependencyObject reference)
        {
            return (isIntelliCodeActive1(reference) || isIntelliCodeActive2());
        }

        /// <summary>
        /// 第一部分，通过可视树判断ai智能补全及 代码片段扩展 是否正在激活中
        /// </summary>
        private static bool isIntelliCodeActive1(DependencyObject reference)
        {
            int count = VisualTreeHelper.GetChildrenCount(reference);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(reference, i);
                if (child is TextBlock textBlock)
                {
                    if (!string.IsNullOrEmpty(textBlock.Text) && textBlock.IsEnabled && textBlock.Opacity == .5 && textBlock.GetType().Name == nameof(TextBlock) && textBlock.Parent?.GetType().FullName is "Microsoft.VisualStudio.Text.Editor.Implementation.AdornmentLayer" or "Microsoft.VisualStudio.Text.Editor.Implementation.TextAndAdornmentSequencing.IntraTextAdornment+AdornmentWrapper")
                        return true;
                }
                else if (child.GetType().FullName.Contains("SuggestionSession+GeometryAdornment"))
                    return true;
                else if (child.GetType().FullName == "Microsoft.VisualStudio.Editor.Implementation.TextMarkerImage")
                    return true;
                else
                    if (isIntelliCodeActive1(child)) return true;
            }
            return false;
        }

        /// <summary>
        /// 第二部分，通过Popup元素判断ai智能补全是否正在激活中
        /// </summary>
        private static bool isIntelliCodeActive2()
        {
            var windows = WindowEnumerator.EnumerateWindows(System.Diagnostics.Process.GetCurrentProcess().Id);
            foreach (var item in windows)
            {
                var source = HwndSource.FromHwnd(item.Hwnd);
                if (source?.RootVisual is FrameworkElement element && element.Parent is Popup)
                {
                    if (isIntelliCodeActive2(element))
                        return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 在里面查找Tab按钮
        /// </summary>
        static bool isIntelliCodeActive2(DependencyObject reference)
        {
            int count = VisualTreeHelper.GetChildrenCount(reference);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(reference, i);
                if (child is Button button)
                {
                    if (button.Parent is StackPanel && button.Name == "PrimaryKeyButton" && button.Content is TextBlock && (
                        i + 2 < count && VisualTreeHelper.GetChild(reference, i + 2) is TextBlock textBlock && textBlock.Name == "CommandDescription"
                        || i + 1 < count && VisualTreeHelper.GetChild(reference, i + 1) is TextBlock textBlock2 && textBlock2.Name == "CommandDescription"))
                        return true;
                }
                else
                    if (isIntelliCodeActive2(child)) return true;
            }
            return false;
        }
    }
}
