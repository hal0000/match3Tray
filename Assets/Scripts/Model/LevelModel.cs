using System;
using System.Collections.Generic;

namespace Match3Tray.Model
{
    [Serializable]
    public class LevelModel
    {
        public int level; // 1..N (görsel/diagnostic)
        public int[] tasks; // her eleman = (type<<12) | count
        public int rewardGold; // tamamlanınca verilecek altın
    }

    [Serializable]
    public class LevelPack
    {
        public List<LevelModel> levels;
    }
}