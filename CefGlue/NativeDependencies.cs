using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Xilium.CefGlue
{
    static class NativeDependencies
    {
        static IEnumerable<KeyValuePair<Tuple<string, int>, string>> NativeDependencyPaths =
                new List<KeyValuePair<Tuple<string, int>, string>>()
                {
                    new KeyValuePair<Tuple<string,int>, string>(Tuple.Create("linux",32),   "http://opensource.spotify.com/cefbuilds/cef_binary_3.2883.1539.gd7f087e_linux32_minimal.tar.bz2"),
                    new KeyValuePair<Tuple<string,int>, string>(Tuple.Create("linux",64),   "http://opensource.spotify.com/cefbuilds/cef_binary_3.2883.1539.gd7f087e_linux64_minimal.tar.bz2"),
                    new KeyValuePair<Tuple<string,int>, string>(Tuple.Create("mac",  64),   "http://opensource.spotify.com/cefbuilds/cef_binary_3.2883.1539.gd7f087e_macosx64_minimal.tar.bz2"),
                    new KeyValuePair<Tuple<string,int>, string>(Tuple.Create("windows",32), "http://opensource.spotify.com/cefbuilds/cef_binary_3.2883.1539.gd7f087e_windows32_minimal.tar.bz2"),
                    new KeyValuePair<Tuple<string,int>, string>(Tuple.Create("windows",64), "http://opensource.spotify.com/cefbuilds/cef_binary_3.2883.1539.gd7f087e_windows64_minimal.tar.bz2"),
                };
    }
}
