using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Xilium.CefGlue
{
    public static class NativeDependencies
    {
        public static IEnumerable<KeyValuePair<Tuple<string, int>, string>> NativeDependencyPaths =
                new List<KeyValuePair<Tuple<string, int>, string>>()
                {
                    new KeyValuePair<Tuple<string,int>, string>(Tuple.Create("linux",32),   "https://cef-builds.spotifycdn.com/cef_binary_87.1.1%2Bg9a70877%2Bchromium-87.0.4280.27_linux32_beta_minimal.tar.bz2"),
                    new KeyValuePair<Tuple<string,int>, string>(Tuple.Create("linux",64),   "https://cef-builds.spotifycdn.com/cef_binary_87.1.1%2Bg9a70877%2Bchromium-87.0.4280.27_linux64_beta_minimal.tar.bz2"),
                    new KeyValuePair<Tuple<string,int>, string>(Tuple.Create("mac",  64),   "https://cef-builds.spotifycdn.com/cef_binary_87.1.1%2Bg9a70877%2Bchromium-87.0.4280.27_macosx64_beta_minimal.tar.bz2"),
                    new KeyValuePair<Tuple<string,int>, string>(Tuple.Create("windows",32), "https://cef-builds.spotifycdn.com/cef_binary_87.1.1%2Bg9a70877%2Bchromium-87.0.4280.27_windows32_beta_minimal.tar.bz2"),
                    new KeyValuePair<Tuple<string,int>, string>(Tuple.Create("windows",64), "https://cef-builds.spotifycdn.com/cef_binary_87.1.1%2Bg9a70877%2Bchromium-87.0.4280.27_windows64_beta_minimal.tar.bz2"),
                };
    }
}
