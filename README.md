# BinCounter
BinCounter provides a memory efficient way to gather very large numbers of numeric observations, count them into bins, and produce histograms and other statistics describing those bins of observations.

At instantiation time, describe the range of observations to report on and the number of buckets to divide that range into.  Then log up to long.MaxValue observations and at any point access the entry counts in different bins or produce histograms and statistics about the data.

## Sample usage
```csharp
    BinCounter b = new BinCounter(30, 0, 2);

    // Log a bunch of distributed data
    Random r = new Random();
    for(int i = 0; i < 10000; i++) {
        float dir = r.NextDouble() < .5 ? -1 : 1;
        float dist = (float) Math.Pow(r.NextDouble(), 2);
        b.Log(1 + dir * dist);
    }

    // Log a few outliers
    b.Log(5f);
    b.Log(-3f);

    Console.WriteLine("TotalEntries: " + b.TotalEntries);
    Console.WriteLine("Mean: " + b.Mean);
    Console.WriteLine("A particular bin count: " + b.Bins[4]);
    Console.WriteLine("\nFull histogram plus info:\n------" + b.GetHistogram());
```

## Sample output
    TotalEntries: 10002
    Mean: 0.9918377
    A particular bin count: 195

    Full histogram plus info:
    ------TotalEntries: 10002
    CountBelowRangeMin: 1
    CountAboveRangeMax: 1
    MedianBinIdx: 14
    MedianBinRange: 0.933<->1.000
    MinObservation: -3.000
    MaxObservation: 5.000
    Mean: 0.992
    <--     0.00--<=0.07            :       176        ************
            0.07--<=0.13            :       169        ************
            0.13--<=0.20            :       183        *************
            0.20--<=0.27            :       204        **************
            0.27--<=0.33            :       195        **************
            0.33--<=0.40            :       201        **************
            0.40--<=0.47            :       243        *****************
            0.47--<=0.53            :       231        ****************
            0.53--<=0.60            :       271        *******************
            0.60--<=0.67            :       269        *******************
            0.67--<=0.73            :       293        *********************
            0.73--<=0.80            :       343        ************************
            0.80--<=0.87            :       440        *******************************
            0.87--<=0.93            :       484        ***********************************
            0.93--<=1.00            :       1382       ****************************************************************************************************
            1.00--<=1.07            :       1321       ***********************************************************************************************
            1.07--<=1.13            :       540        ***************************************
            1.13--<=1.20            :       401        *****************************
            1.20--<=1.27            :       354        *************************
            1.27--<=1.33            :       304        *********************
            1.33--<=1.40            :       221        ***************
            1.40--<=1.47            :       250        ******************
            1.47--<=1.53            :       247        *****************
            1.53--<=1.60            :       211        ***************
            1.60--<=1.67            :       182        *************
            1.67--<=1.73            :       208        ***************
            1.73--<=1.80            :       182        *************
            1.80--<=1.87            :       167        ************
            1.87--<=1.93            :       163        ***********
            1.93--<=2.00        --> :       167        ************

