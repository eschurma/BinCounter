using System;
using System.Collections.Generic;
using System.Diagnostics;

/// <summary>
/// Counts observed data into bins for reporting histograms and other statistics about that data.
/// </summary>
public class BinCounter {
    private long[] _bins;
    public IList<long> Bins;

    private float _rangeMin;
    /// <summary>
    /// The lower bound of the first bin. User defined at construction time.
    /// Observations below this value will still be included in the first bin.
    /// </summary>
    public float RangeMin { get => _rangeMin; }

    private float _rangeMax;
    /// <summary>
    /// The upper bound of the last bin. User defined at construction time.
    /// Observations above this value will still be included in the last bin.
    /// </summary>
    public float RangeMax { get => _rangeMax; }

    /// <summary>
    /// The number of observations below RangeMin
    /// </summary>
    private long _countBelowRangeMin;
    public long CountBelowRangeMin { get => _countBelowRangeMin; }

    private long _countAboveRangeMax;
    /// <summary>
    /// The number of observations higher then RangeMin
    /// </summary>
    public long CountAboveRangeMax { get => _countAboveRangeMax; }

    private float _minObservation = float.NaN;
    public float MinObservation { get => _minObservation; }

    private float _maxObservation = float.NaN;
    public float MaxObservation { get => _maxObservation; }

    private int _numBins;
    public int NumBins { get => _numBins; }

    private float _binSize;
    public float BinSize { get => _binSize; }

    private long _totalObservations = 0;
    public long TotalObservations { get => _totalObservations; }

    /// <summary>
    /// Returs whether the BinLogger is full and no longer accepting observations.
    /// </summary>
    public bool IsFull { get => (_totalObservations == long.MaxValue); }

    // Used during mean calculation
    private double _runningSum = 0;

    /// <summary>
    /// Creates a new bin logger. Divides the total range (rangeMax-rangeMin) into numBins number of bins.
    /// </summary>
    /// <param name="numBins">The number of distinct bins</param>
    /// <param name="rangeMin">The min value of the lowest bin to report on. Any items less than this value will also increment the first bin.</param>
    /// <param name="rangeMax">The max value of the highest bin to report on. Any items greater than this value will also increment the last bin.</param>
    public BinCounter(int numBins, float rangeMin, float rangeMax) {
        Debug.Assert(numBins > 0);
        Debug.Assert(rangeMin < rangeMax);

        _bins = new long[numBins];
        Bins = Array.AsReadOnly(_bins);
        _numBins = numBins;
        _rangeMin = rangeMin;
        _rangeMax = rangeMax;
        _binSize = (_rangeMax - rangeMin) / _numBins;
    }

    /// <summary>
    /// Logs a new observation into the appropriate bin. 
    /// If the BinCounter is full, then don't log.
    /// </summary>
    /// <param name="observation">The observation data to record.</param>
    /// <param name="count">The count to increment the bin by. Defaults to 1.</param>
    public void Log(double observation, long count = 1) {
        Log((float)observation, count);
    }

    /// <summary>
    /// Logs a new observation into the appropriate bin. 
    /// If the BinCounter is full, then don't log.
    /// </summary>
    /// <param name="observation">The observation data to record.</param>
    /// <param name="count">The count to increment the bin by. Defaults to 1.</param>
    public void Log(float observation, long count = 1) {
        if(count <= 0) {
            throw new ArgumentOutOfRangeException(nameof(count), "Count must be positive.");
        }
        if(float.IsNaN(observation) || IsFull) {
            return;
        }
        if(long.MaxValue - TotalObservations < count) {
            // Reduce count so it won't exceed long capacity
            count = long.MaxValue - TotalObservations;
        }

        if(float.IsNaN(_minObservation) || observation < _minObservation) _minObservation = observation;
        if(float.IsNaN(_maxObservation) || observation > _maxObservation) _maxObservation = observation;
        _runningSum += (double)(count * observation);

        int idx;
        if(observation < _rangeMin) {
            idx = 0;
            _countBelowRangeMin += count;
        }
        else if(observation > _rangeMax) {
            idx = _numBins - 1;
            _countAboveRangeMax += count;
        }
        else {
            idx = (int)((observation - _rangeMin) / _binSize);
            // In case floating point precision results in too high a bin idx.
            idx = Math.Min(idx, NumBins - 1);
        }

        _bins[idx] += count;
        _totalObservations += count;
    }

    /// <summary>
    /// Gets a text based histogram representation of the data in the bins.
    /// </summary>
    /// <returns></returns>
    public String GetHistogram(bool includeStats = true) {
        if(TotalObservations == 0) { return ""; }

        System.Text.StringBuilder s = new System.Text.StringBuilder();

        // First run through and find max. This is later used to set graph sizes.
        long maxVal = 0;
        long runningCount = 0;
        long medianBin = 0;
        long medianObservationIdx = TotalObservations / 2;
        for(int i = 0; i < NumBins; i++) {
            if(Bins[i] > maxVal) {
                maxVal = Bins[i];
            }

            if(runningCount < medianObservationIdx && (runningCount + Bins[i]) >= medianObservationIdx) {
                medianBin = i;
            }
            runningCount += Bins[i];
        }

        if(includeStats) {
            s.AppendLine($"TotalEntries: {TotalObservations}");
            s.AppendLine($"CountBelowRangeMin: {CountBelowRangeMin}");
            s.AppendLine($"CountAboveRangeMax: {CountAboveRangeMax}");
            float binLow = RangeMin + (medianBin * BinSize);
            float binHigh = binLow + BinSize;
            s.AppendLine($"MedianBinIdx: {medianBin}");
            s.AppendLine($"MedianBinRange: {binLow,4:F3}<->{binHigh,4:F3}");
            s.AppendLine($"MinObservation: {MinObservation,4:F3}");
            s.AppendLine($"MaxObservation: {MaxObservation,4:F3}");
            s.AppendLine($"Mean: {Mean,4:F3}");
        }

        for(int i = 0; i < NumBins; i++) {
            float binLow = RangeMin + (i * BinSize);
            float binHigh = binLow + BinSize;

            if(i == 0) { // first bin
                s.Append($"<--{binLow,9:F2}--<={binHigh,-12:F2}");
            }
            else if(i == NumBins - 1) { // last bin
                s.Append($"{binLow,12:F2}--<={binHigh,-12:F2}-->");
            }
            else { // the rest
                s.Append($"{binLow,12:F2}--<={binHigh,-12:F2}");
            }

            s.Append($"\t:\t{Bins[i],-10} ");

            // Find what percentage of maxVal the current bin contains, then 
            // display that times 100 *'s. 
            s.Append('*', (int)(100 * ((float)Bins[i] / maxVal)));
            s.Append('\n');
        }
        return s.ToString();
    }

    public float Mean { get => (float)_runningSum / TotalObservations; }

    /// <summary>
    /// Resets the array to all zero values.
    /// </summary>
    public void Reset() {
        System.Array.Clear(_bins, 0, _bins.Length);
        _countAboveRangeMax = 0;
        _countBelowRangeMin = 0;
        _minObservation = float.NaN;
        _maxObservation = float.NaN;
        _runningSum = 0;
        _totalObservations = 0;
    }
}
