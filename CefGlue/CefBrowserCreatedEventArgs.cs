using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Xilium.CefGlue
{

    public class CefBrowserCreatedEventArgs : EventArgs
    {
        public readonly CefBrowser Browser;

        public CefBrowserCreatedEventArgs(CefBrowser browser)
        {
            Browser = browser;
        }
    }
}
