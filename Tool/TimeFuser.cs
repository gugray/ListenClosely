using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tool
{
    class TimeFuser
    {
        readonly Material mTrans;
        readonly Material mOrig;

        public TimeFuser(Material mTrans, Material mOrig)
        {
            this.mTrans = mTrans;
            this.mOrig = mOrig;
        }

        private List<DiffItem> buildDiffItems(Material mat)
        {
            var res = new List<DiffItem>();
            for (int segIx = 0; segIx < mat.Segments.Count; ++segIx)
            {
                for (int wdIx = 0; wdIx < mat.Segments[segIx].Words.Count; ++wdIx)
                {
                    var wd = mat.Segments[segIx].Words[wdIx];
                    foreach (char c in wd.Text)
                    {
                        res.Add(new DiffItem { Val = char.ToLower(c), SegIx = segIx, WordIx = wdIx });
                    }
                }
            }
            return res;
        }


        public void Fuse()
        {
            var trDiff = buildDiffItems(mTrans);
            var orDiff = buildDiffItems(mOrig);
            var differ = new Differ(trDiff, orDiff);
            differ.DoDiff();
            // Here we index by orig word index (seg/wd)
            // For each word, we count how many characters it shares with various aligned transcription words
            // We'll be mapping orig word onto the transcript word with the most aligned letters
            // Composite key is 1000 * segmentIx + wordIX
            var counter = new Dictionary<int, Dictionary<int, int>>();
            int trIx = 0, orIx = 0;
            while (orIx < orDiff.Count && trIx < trDiff.Count)
            {
                if (orDiff[orIx].Change && trDiff[trIx].Change) { ++orIx; ++trIx; continue; }
                if (orDiff[orIx].Change) { ++orIx; continue; }
                if (trDiff[trIx].Change) { ++trIx; continue; }
                int orKey = orDiff[orIx].SegIx * 1000 + orDiff[orIx].WordIx;
                int trKey = trDiff[trIx].SegIx * 1000 + trDiff[trIx].WordIx;
                Dictionary<int, int> itm = null;
                if (counter.ContainsKey(orKey)) itm = counter[orKey];
                else { itm = new Dictionary<int, int>(); counter[orKey] = itm; }
                if (itm.ContainsKey(trKey)) ++itm[trKey];
                else itm[trKey] = 1;
                ++orIx; ++trIx;
            }
            // Set timestamps in successfully mapped original words
            // Also collect refs to words in a flat list, for next step
            var flatWords = new List<Word>();
            var sorter = new List<int>();
            for (int segIx = 0; segIx < mOrig.Segments.Count; ++segIx)
            {
                for (int wdIx = 0; wdIx < mOrig.Segments[segIx].Words.Count; ++wdIx)
                {
                    flatWords.Add(mOrig.Segments[segIx].Words[wdIx]);
                    int orKey = segIx * 1000 + wdIx;
                    if (!counter.ContainsKey(orKey)) continue;
                    sorter.Clear();
                    foreach (var x in counter[orKey]) sorter.Add(x.Key);
                    sorter.Sort((a, b) => counter[orKey][b].CompareTo(counter[orKey][a]));
                    int trSegIx = sorter[0] / 1000;
                    int trWdIx = sorter[0] % 1000;
                    mOrig.Segments[segIx].Words[wdIx].StartSec = mTrans.Segments[trSegIx].Words[trWdIx].StartSec;
                    mOrig.Segments[segIx].Words[wdIx].LengthSec = mTrans.Segments[trSegIx].Words[trWdIx].LengthSec;
                }
            }
            // Sequences of unmapped words: equally distribute gap time
            // This maps from start of gaps to length of gaps
            var gaps = new Dictionary<int, int>();
            int gapStart = -1;
            for (int i = 0; i < flatWords.Count; ++i)
            {
                if (flatWords[i].StartSec != 0)
                {
                    gapStart = -1;
                    continue;
                }
                if (gapStart == -1)
                {
                    gapStart = i;
                    gaps[i] = 1;
                }
                else ++gaps[gapStart];
            }
            foreach (var x in gaps)
            {
                if (x.Key == 0)
                {
                    flatWords[0].LengthSec = flatWords[1].StartSec;
                    continue;
                }
                // Start time of first word: end of previous word
                decimal start = flatWords[x.Key - 1].StartSec + flatWords[x.Key - 1].LengthSec;
                if (x.Key == flatWords.Count) continue;
                decimal end = flatWords[x.Key + 1].StartSec;
                decimal len = (end - start) / ((decimal)x.Value);
                for (int i = 0; i < x.Value; ++i)
                {
                    flatWords[x.Key + i].StartSec = start + ((decimal)i) * len;
                    flatWords[x.Key + i].LengthSec = len;
                }
            }
            // Timestamp each segment
            for (int segIx = 0; segIx < mOrig.Segments.Count; ++segIx)
            {
                mOrig.Segments[segIx].StartSec = mOrig.Segments[segIx].Words[0].StartSec;
                int wdx = mOrig.Segments[segIx].Words.Count - 1;
                decimal endSec = mOrig.Segments[segIx].Words[wdx].StartSec + mOrig.Segments[segIx].Words[wdx].LengthSec;
                mOrig.Segments[segIx].LengthSec = endSec - mOrig.Segments[segIx].StartSec;
            }
        }
    }
}
