using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text;

namespace Tool
{
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    class DiffItem
    {
        public int SegIx;
        public int WordIx;
        public char Val;
        public bool Change = false;

        public string DebuggerDisplay
        {
            get
            {
                string res = Val + " / " + Change;
                return res;
            }
        }
    }

    class Differ
    {
        public readonly List<DiffItem> A;
        public readonly List<DiffItem> B;
        List<bool> modifA = new List<bool>();
        List<bool> modifB = new List<bool>();
        List<int> downVector = new List<int>();
        List<int> upVector = new List<int>();

        public Differ(List<DiffItem> a, List<DiffItem> b)
        {
            A = a;
            B = b;
        }

        public void DoDiff()
        {
            int lenMax = A.Count + B.Count + 1;
            for (int i = 0; i < A.Count + 2; ++i) modifA.Add(false);
            for (int i = 0; i < B.Count + 2; ++i) modifB.Add(false);
            for (int i = 0; i < 2 * lenMax + 2; ++i) downVector.Add(0);
            for (int i = 0; i < 2 * lenMax + 2; ++i) upVector.Add(0);

            lcs(0, A.Count, 0, B.Count);

            for (int aPos = 0; aPos < A.Count; ++aPos)
                A[aPos].Change = modifA[aPos];
            for (int bPos = 0; bPos < B.Count; ++bPos)
                B[bPos].Change = modifB[bPos];
        }

        void lcs(int lowerA, int upperA, int lowerB, int upperB)
        {
            // Fast walkthrough equal lines at the start
            while (lowerA < upperA && lowerB < upperB && A[lowerA].Val == B[lowerB].Val)
            {
                ++lowerA; ++lowerB;
            }

            // Fast walkthrough equal lines at the end
            while (lowerA < upperA && lowerB < upperB && A[upperA - 1].Val == B[upperB - 1].Val)
            {
                --upperA; --upperB;
            }

            if (lowerA == upperA)
            {
                // mark as inserted.
                while (lowerB < upperB) modifB[lowerB++] = true;
            }
            else if (lowerB == upperB)
            {
                // mark as deleted.
                while (lowerA < upperA) modifA[lowerA++] = true;
            }
            else
            {
                // Find the middle snake and length of an optimal path for A and B
                int x = 0, y = 0;
                sms(lowerA, upperA, lowerB, upperB, ref x, ref y);
                // The path is from lowerX to (x,y) and (x,y) to upperX
                lcs(lowerA, x, lowerB, y);
                lcs(x, upperA, y, upperB);
            }
        }

        void sms(int lowerA, int upperA, int lowerB, int upperB, ref int xRes, ref int yRes)
        {
            xRes = yRes = 0;
            int lenMax = A.Count + B.Count + 1;

            int downK = lowerA - lowerB; // the k-line to start the forward search
            int upK = upperA - upperB; // the k-line to start the reverse search
            int delta = (upperA - lowerA) - (upperB - lowerB);
            bool oddDelta = (delta & 1) != 0;

            // The vectors in the publication accept negative indexes. The vectors here are 0-based
            // and are accessed using a specific offset: upOffset for upVector and downOffset for downVector
            int downOffset = lenMax - downK;
            int upOffset = lenMax - upK;
            int maxD = ((upperA - lowerA + upperB - lowerB) / 2) + 1;

            // Init vectors
            downVector[downOffset + downK + 1] = lowerA;
            upVector[upOffset + upK - 1] = upperA;

            for (int d = 0; d <= maxD; d++)
            {
                // Extend the forward path
                for (int k = downK - d; k <= downK + d; k += 2)
                {
                    // Find the only or better starting point
                    int x, y;
                    if (k == downK - d)
                    {
                        x = downVector[downOffset + k + 1]; // down
                    }
                    else
                    {
                        x = downVector[downOffset + k - 1] + 1; // a step to the right
                        if ((k < downK + d) && (downVector[downOffset + k + 1] >= x))
                            x = downVector[downOffset + k + 1]; // down
                    }
                    y = x - k;
                    // Find the end of the furthest reaching forward d-path in diagonal k.
                    while ((x < upperA) && (y < upperB) && A[x].Val == B[y].Val)
                    {
                        x++; y++;
                    }
                    downVector[downOffset + k] = x;

                    // Overlap?
                    if (oddDelta && (upK - d < k) && (k < upK + d))
                    {
                        if (upVector[upOffset + k] <= downVector[downOffset + k])
                        {
                            xRes = downVector[downOffset + k];
                            yRes = downVector[downOffset + k] - k;
                            return;
                        }
                    }
                } // for k

                // Extend the reverse path
                for (int k = upK - d; k <= upK + d; k += 2)
                {
                    // Find the only or better starting point
                    int x, y;
                    if (k == upK + d)
                    {
                        x = upVector[upOffset + k - 1]; // up
                    }
                    else
                    {
                        x = upVector[upOffset + k + 1] - 1; // left
                        if ((k > upK - d) && (upVector[upOffset + k - 1] < x))
                            x = upVector[upOffset + k - 1]; // up
                    }
                    y = x - k;

                    while ((x > lowerA) && (y > lowerB) && A[x - 1].Val == B[y - 1].Val)
                    {
                        x--; y--; // diagonal
                    }
                    upVector[upOffset + k] = x;

                    // Overlap?
                    if (!oddDelta && (downK - d <= k) && (k <= downK + d))
                    {
                        if (upVector[upOffset + k] <= downVector[downOffset + k])
                        {
                            xRes = downVector[downOffset + k];
                            yRes = downVector[downOffset + k] - k;
                            return;
                        }
                    }
                } // for k
            } // for d
        }
    }
}
