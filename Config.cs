using System.Runtime.InteropServices;
using System.Text;

namespace itobot {
    static class Config {
        [DllImport("KERNEL32.DLL", CharSet = CharSet.Unicode)]
        private static extern uint GetPrivateProfileString(string lpAppName, string lpKeyName, string? lpDefault, StringBuilder lpReturnedString, uint nSize, string lpFileName);
        
        private const string CONFIG_FILE_NAME = "config.ini";
        private static readonly string IniFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, CONFIG_FILE_NAME);

        private static string GetConfigString(string section, string key) {
            StringBuilder sb = new(1024);
            _ = GetPrivateProfileString(section, key, null, sb, (uint)sb.Capacity, IniFilePath);
            return sb.ToString();
        }

        public static class Bot {
            private const string SECTION = "bot";
            public static string BotToken {
                get {
                    string ret = GetConfigString(SECTION, "Token");
                    if (string.IsNullOrEmpty(ret))
                        throw new Exception("BOTトークンの取得に失敗しました。");
                    return ret;
                }
            }
        }

        public static class ID {
            private const string SECTION = "Id";
            public static ulong ServerID {
                get {
                    string ret = GetConfigString(SECTION, "ServerID");
                    if (ret == string.Empty)
                        throw new Exception("認証サーバーIDの取得に失敗しました。");
                    return ulong.Parse(ret);
                }
            }
        }

        public static class Master {
            private const string SECTION = "Master";
            public static string Owner {
                get {
                    string ret = GetConfigString(SECTION, "Owner");
                    if (string.IsNullOrEmpty(ret))
                        throw new Exception("所有者の取得に失敗しました。");
                    return ret;
                }
            }
            public static string Log {
                get {
                    string ret = GetConfigString(SECTION, "Log");
                    if (string.IsNullOrEmpty(ret))
                        throw new Exception("Logチャンネルの取得に失敗しました。");
                    return ret;
                }
            }
        }
    }
}