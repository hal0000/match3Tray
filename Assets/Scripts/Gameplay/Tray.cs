namespace Match3Tray.Gameplay
{
    public class Tray
    {
        private readonly int _cap;
        private readonly int[] _slots;

        public Tray(int cap)
        {
            _cap = cap;
            _slots = new int[cap];
            for (var i = 0; i < cap; i++) _slots[i] = -1;
        }

        public TrayAddResult TryAdd(int typeId)
        {
            var free = -1;
            for (var i = 0; i < _cap; i++)
                if (_slots[i] == -1)
                {
                    free = i;
                    break;
                }

            if (free < 0) return new TrayAddResult { Accepted = false };

            _slots[free] = typeId;


            var count = 0;
            for (var i = 0; i < _cap; i++)
                if (_slots[i] == typeId)
                    count++;
            if (count < 3) return new TrayAddResult { Accepted = true, Cleared = false };


            var idx = new int[3];
            var k = 0;
            for (var i = 0; i < _cap && k < 3; i++)
                if (_slots[i] == typeId)
                    idx[k++] = i;


            for (var i = 0; i < 3; i++) _slots[idx[i]] = -1;


            var w = 0;
            for (var r = 0; r < _cap; r++)
                if (_slots[r] != -1)
                    _slots[w++] = _slots[r];
            while (w < _cap) _slots[w++] = -1;

            return new TrayAddResult { Accepted = true, Cleared = true, ClearedIndices = idx };
        }

        public bool IsFull()
        {
            for (var i = 0; i < _cap; i++)
                if (_slots[i] == -1)
                    return false;
            return true;
        }
    }
}