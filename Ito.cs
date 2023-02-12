namespace itobot {
    public class Ito : IDisposable {
        readonly Dictionary<ulong, byte> table;
        readonly List<ulong> perm;

        public Ito(List<ulong> players) {
            table = new Dictionary<ulong, byte>(Enumerable.Range(1, 100).OrderBy(i => Guid.NewGuid()).Take(players.Count).Select((s, i) => KeyValuePair.Create(players[i], (byte)s)));
            perm = new List<ulong>();
        }

        public byte Check(ulong uid) => table[uid];

        public void Leave(ulong uid)  {
            perm.Remove(uid);
            table.Remove(uid);
        }

        public List<ulong> Submit(ulong uid) {
            perm.Add(uid);
            return perm.Reverse<ulong>().ToList();
        }

        public List<(bool, byte, ulong)> Result() {
            List <(bool, byte, ulong)> ret = new();
            byte min = 0;
            foreach (ulong i in perm) {
                if (table[i] < min)
                    ret.Add((false, table[i], i));
                else {
                    min = table[i];
                    ret.Add((true, table[i], i));
                }
            }
            return ret.Reverse<(bool, byte, ulong)>().ToList();
        }

        public void Dispose() {
            table.Clear();
            perm.Clear();
            GC.SuppressFinalize(this);
        }
    }
}
