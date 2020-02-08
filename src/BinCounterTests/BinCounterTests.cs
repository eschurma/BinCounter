using NUnit.Framework;
using System;

namespace Tests {
    public class BinCounterTests {
        // Note: shared test objects can go here. Like I could make a shared BinCounter
        //       but in this case I didn't want that. 

        [Test]
        public void BasicTest() {
            BinCounter b = new BinCounter(10, 0f, 1f);
            Assert.AreEqual(0.1f, b.BinSize, nameof(b.BinSize));
            Assert.AreEqual(0f, b.RangeMin, nameof(b.RangeMin));
            Assert.AreEqual(1f, b.RangeMax, nameof(b.RangeMax));
            Assert.AreEqual(0, b.TotalObservations, nameof(b.TotalObservations));
        }

        [Test]
        public void LogTest() {
            BinCounter b = new BinCounter(10, 0f, 1f);
            float[] observations = new float[] { 0f, .01f, .51f };
            float runningSum = 0f;
            foreach(float o in observations) {
                b.Log(o);
                runningSum += o;
            }
            float mean = runningSum / observations.Length;

            Assert.AreEqual(observations.Length, b.TotalObservations, nameof(b.TotalObservations));
            long bin0Cnt = b.Bins[0];
            Assert.AreEqual(2, bin0Cnt, nameof(bin0Cnt));
            long bin5Cnt = b.Bins[5];
            Assert.AreEqual(1, bin5Cnt, nameof(bin5Cnt));
            Assert.AreEqual(.51f, b.MaxObservation, nameof(b.MaxObservation));
            Assert.AreEqual(0f, b.MinObservation, nameof(b.MinObservation));
            Assert.AreEqual(mean, b.Mean, nameof(b.Mean));

            b.Log((double)1);
            long bin9Cnt = b.Bins[9];
            Assert.AreEqual(1, bin9Cnt);
        }

        [Test]
        public void LogAtBoundariesTest() {
            BinCounter b = new BinCounter(10, -1f, 1f);
            b.Log(-1);
            b.Log(1);
            Assert.IsTrue(b.Bins[0] == 1, "Expected the first bin to have an entry. It didn't.");
            Assert.IsTrue(b.Bins[b.NumBins - 1] == 1, "Expected the last bin to have an entry. It didn't.");
            b.Log(.999999999);
            Assert.IsTrue(b.Bins[b.NumBins - 1] == 2, "Failed adding value extremely close to RangeMax.");
        }

        public void LogBeyondBoundariesTest() {
            BinCounter b = new BinCounter(10, -1, 1);
            b.Log(-2);
            b.Log(2);
            Assert.IsTrue(b.Bins[0] == 1);
            Assert.IsTrue(b.Bins[b.NumBins - 1] == 1);
            b.Log(3);
            Assert.AreEqual(2, b.CountAboveRangeMax, nameof(b.CountAboveRangeMax));
            Assert.AreEqual(1, b.CountBelowRangeMin, nameof(b.CountBelowRangeMin));
        }

        public void LogNaNtest() {
            BinCounter b = new BinCounter(10, -1, 1);
            b.Log(-2);
            b.Log(float.NaN); // This should get discarded
            Assert.AreEqual(1, b.TotalObservations, nameof(b.TotalObservations));
        }

        [Test]
        public void ResetTest() {
            BinCounter b = new BinCounter(10, 0, 1);
            System.Random r = new System.Random();

            for(int i = 0; i < 500; i++) {
                b.Log((float)r.NextDouble());
            }

            // Log some outliers
            b.Log(-5);
            b.Log(10);
            b.Log(1);

            // All bins should have something in them.
            foreach(int binVal in b.Bins) {
                Assert.IsTrue(binVal > 0, "Failure populating bins");
            }

            b.Reset();

            // Make sure the entries counter is empty.
            Assert.IsTrue(b.TotalObservations == 0, "Expected 0 TotalEntries. Found " + b.TotalObservations);

            // Make sure all bins are empty.
            bool foundSomething = false;
            foreach(int binVal in b.Bins) {
                if(binVal > 0) {
                    foundSomething = true;
                    break;
                }
            }
            Assert.IsFalse(foundSomething, "Expected all bins to be empty. They weren't.");
            Assert.AreEqual(0, b.CountAboveRangeMax, nameof(b.CountAboveRangeMax));
            Assert.AreEqual(0, b.CountBelowRangeMin, nameof(b.CountBelowRangeMin));
            Assert.AreEqual(float.NaN, b.Mean, nameof(b.Mean));

            b.Log(1f);
            b.Log(2f);
            Assert.AreEqual(1f, b.MinObservation, nameof(b.MinObservation));
            Assert.AreEqual(2f, b.MaxObservation, nameof(b.MaxObservation));
            Assert.AreEqual(1.5f, b.Mean, nameof(b.Mean));
        }

        [Test]
        public void TryToOverflowBinsTest() {
            BinCounter b = new BinCounter(3, -1, 1);
            b.Log(0f, 5);
            b.Log(0f, long.MaxValue);
            Assert.IsTrue(b.IsFull && b.TotalObservations == long.MaxValue && b.Bins[1] == long.MaxValue, "Individual bin overflow fail.");
            b.Reset();
            b.Log(0f, 5);
            b.Log(1f, long.MaxValue - 4);
            Assert.IsTrue(b.IsFull && b.TotalObservations == long.MaxValue, "TotalEntries overflow fail.");
        }

        [Test]
        public void GetHistogramAllEqualBinsTest() {
            BinCounter b = new BinCounter(10, -1, 1);
            for(int i = 0; i < b.NumBins; i++) {
                // log one in each bin
                b.Log(b.RangeMin + (b.BinSize / 2) + i * b.BinSize);
            }
            string s = b.GetHistogram();

            Assert.IsTrue(s.IndexOf("TotalEntries: 10") > -1);
            Assert.IsTrue(s.IndexOf("CountBelowRangeMin: 0") > -1);
            Assert.IsTrue(s.IndexOf("CountAboveRangeMax: 0") > -1);
            Assert.IsTrue(s.IndexOf("MedianBinIdx: 4") > -1);

            // All of the bins should be the same max size of 100
            string hundredStars = new string('*', 100);
            string[] splitOn = new string[1];
            splitOn[0] = hundredStars;
            int count = s.Split(splitOn, System.StringSplitOptions.None).Length - 1;
            Assert.IsTrue(count == b.NumBins, "Not all bins were the expected size." + count + "\nOutput was:\n" + s);
        }
    }
}
