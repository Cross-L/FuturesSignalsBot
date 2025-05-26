using FuturesSignalsBot.Models;
using FuturesSignalsBot.Models.IndicatorResults;

namespace FuturesSignalsBot.Indicators;

public static class FramaIndicator
{
    public static FramaResult CalculateFramaSignal(List<CryptocurrencyDataItem> data)
    {
        int gaussLen = 2;
        double gaussSigma = 2.0;
        int fmLen = 12;
        int fmUpperLimit = 90;
        int fmLowerLimit = 60;
        int atrLen = 72;
        double atrMult = 3.0;

        var gauss = new double[data.Count];
        
        for (int i = 0; i < data.Count; i++)
        {
            if (i < gaussLen - 1)
            {
                gauss[i] = (double)data[i].Open;
            }
            else
            {
                double sumG = 0.0;
                double weightedSumG = 0.0;
                
                for (int j = 0; j < gaussLen; j++)
                {
                    int idx = i - j;
                    double w = Math.Exp(
                        -0.5 * Math.Pow(
                            (j - (gaussLen - 1.0)/2.0) / gaussSigma, 
                            2.0
                        )
                    );
                    sumG += w;
                    weightedSumG += (double)data[idx].Open * w;
                }
                gauss[i] = weightedSumG / sumG;
            }
        }

        var frama = new double[data.Count];
        frama[0] = gauss[0];

        for (int i = 1; i < data.Count; i++)
        {
            if (i < fmLen)
            {
                frama[i] = frama[i - 1];
                continue;
            }

            int half = fmLen / 2;

            decimal highestAll = decimal.MinValue;
            decimal lowestAll = decimal.MaxValue;
            for (int b = i - fmLen + 1; b <= i; b++)
            {
                if (data[b].High > highestAll) highestAll = data[b].High;
                if (data[b].Low < lowestAll) lowestAll = data[b].Low;
            }
            double HL = (double)(highestAll - lowestAll) / fmLen;

            decimal highestHalf1 = decimal.MinValue;
            decimal lowestHalf1 = decimal.MaxValue;
            for (int b = i - half + 1; b <= i; b++)
            {
                if (data[b].High > highestHalf1) highestHalf1 = data[b].High;
                if (data[b].Low < lowestHalf1) lowestHalf1 = data[b].Low;
            }
            double HL1 = (double)(highestHalf1 - lowestHalf1) / half;

            decimal highestHalf2 = decimal.MinValue;
            decimal lowestHalf2 = decimal.MaxValue;
            int halfStart = i - fmLen + 1;
            int halfEnd = i - half;
            for (int b = halfStart; b <= halfEnd; b++)
            {
                if (data[b].High > highestHalf2) highestHalf2 = data[b].High;
                if (data[b].Low < lowestHalf2) lowestHalf2 = data[b].Low;
            }
            double HL2 = (double)(highestHalf2 - lowestHalf2) / half;

            bool valid = (HL > 0 && HL1 > 0 && HL2 > 0);
            double D;
            if (valid)
            {
                D = (Math.Log(HL1 + HL2) - Math.Log(HL)) / Math.Log(2.0);
            }
            else
            {
                D = 1.0;
            }

            double w = Math.Log(2.0 / (fmLowerLimit + 1.0));
            double alpha = Math.Exp(w * (D - 1.0));
            double alpha1 = Clamp(alpha, 0.01, 1.0);

            double oldN = (2.0 - alpha1) / alpha1;
            double newN = (fmLowerLimit - fmUpperLimit) * (oldN - 1.0) / (fmLowerLimit - 1.0) + fmUpperLimit;
            double newAlpha = 2.0 / (newN + 1.0);
            double newAlpha1 = Clamp(newAlpha, 2.0 / (fmLowerLimit + 1.0), 1.0);

            double prevFrama = frama[i - 1];
            double currentSrc = gauss[i];

            frama[i] = (1.0 - newAlpha1) * prevFrama + newAlpha1 * currentSrc;
        }

        var atr = new double[data.Count];
        decimal firstTR = data[0].High - data[0].Low;
        atr[0] = (double)firstTR;
        double alphaAtr = 1.0 / atrLen;

        for (int i = 1; i < data.Count; i++)
        {
            decimal highLow = data[i].High - data[i].Low;
            decimal highClose = Math.Abs(data[i].High - data[i - 1].Close);
            decimal lowClose = Math.Abs(data[i].Low - data[i - 1].Close);
            decimal trueRange = Math.Max(highLow, Math.Max(highClose, lowClose));
            
            double tr = (double)trueRange;
            atr[i] = atr[i - 1] + alphaAtr * (tr - atr[i - 1]);
        }

        var QB = new int[data.Count];
        QB[0] = 0;

        for (int i = 1; i < data.Count; i++)
        {
            double fr = frama[i];
            double a = atr[i] * atrMult;
            decimal c = data[i].Close;

            double longV = fr + a;
            double shortV = frama[i] - atr[i];

            bool longC = c > (decimal)longV;
            bool shortC = c < (decimal)shortV;

            int prevQB = QB[i - 1];
            int curQB = prevQB;

            if (longC && !shortC)
                curQB = 1;
            if (shortC)
                curQB = -1;

            QB[i] = curQB;
        }

        int last = data.Count - 1;
        bool isBlue = (QB[last] == 1);
        bool isRed = (QB[last] == -1);

        return new FramaResult(isRed, isBlue);
    }

    private static double Clamp(double value, double min, double max)
    {
        if (value < min) return min;
        if (value > max) return max;
        return value;
    }
}