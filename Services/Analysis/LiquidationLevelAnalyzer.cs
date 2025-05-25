using System;
using System.Globalization;
using System.Linq;
using FuturesSignalsBot.Models;
using FuturesSignalsBot.Models.IndicatorResults;

namespace FuturesSignalsBot.Services.Analysis;

public class LiquidationLevelAnalyzer(Cryptocurrency currency)
{
    private static readonly VolumeConfiguration RawVolumeConfiguration = new(36,24);
    
    private int _precision;
    public PreliminaryImpulse? PreliminaryImpulse30M { get; private set; }
    public PreliminaryImpulse? PreliminaryImpulse5M { get; private set; }
    public PreliminaryImpulse? SpecifiedPreliminaryImpulse5M { get; private set; }

    public void InitializeAsync()
    {
        InitializeVolumeManagersAsync();
        _precision = CalculatePrecision(currency.TradingDataContainer.ThirtyMinuteData.Last().Open);
        ProcessPreliminaryImpulses();
    }

    public void Update(bool newThirtyMinuteReceived)
    {
        PreliminaryImpulse30M = null;
        PreliminaryImpulse5M = null;
        SpecifiedPreliminaryImpulse5M = null;

        if (newThirtyMinuteReceived)
        {
            UpdateVolumeManagers();
        }
        
        _precision = CalculatePrecision(currency.TradingDataContainer.ThirtyMinuteData.Last().Open);
        ProcessPreliminaryImpulses();
    }

    private void InitializeVolumeManagersAsync()
    {
        VolumeProfileManager.CalculateRawVolumeProfiles(currency.TradingDataContainer, RawVolumeConfiguration);
        VolumeProfileManager.InitializeVolumeLevels(currency.TradingDataContainer);
    }

    private void ProcessPreliminaryImpulses()
    {
        var lastItem30M = currency.TradingDataContainer.ThirtyMinuteData.Last();
        var lastItem5M = currency.TradingDataContainer.FiveMinuteData.Last();
        var specifiedIntersectionResult = VolumeProfileManager.CheckSpecifiedLiquidationIntersection5M(lastItem5M, lastItem30M);
        var intersectionResult30M = VolumeProfileManager.CheckLiquidationIntersection30M(lastItem30M);
        var intersectionResult5M = VolumeProfileManager.CheckLiquidationIntersection5M(lastItem5M, lastItem30M);

        var lastPoc = lastItem30M.VolumeProfileData[0].SmoothedPoc;
        var isLong30M = lastPoc < lastItem30M.SmoothedClose;
        var isLong5M = lastPoc < lastItem5M.SmoothedClose;

        var pocPercentageChange = CryptoAnalysisTools.CalculatePercentageChange
            (lastPoc, lastItem5M.Close);
        
        var lastItems = currency.TradingDataContainer.FiveMinuteData.TakeLast(360).ToList();
        var lastClose = lastItem5M.Close;
        var maxLoop = lastItems.MaxBy(item => item.High)!.High;
        var minLoop = lastItems.MinBy(item => item.Low)!.Low;
        var rangeLoop = maxLoop - minLoop;
        var difLoop = lastClose - minLoop;
        var changeOv = difLoop / rangeLoop * 100;

        if (intersectionResult30M is not null)
        {
            PreliminaryImpulse30M = new PreliminaryImpulse(
                currency.Name,
                true,
                lastItem30M,
                isLong30M,
                intersectionResult30M.ToString(),
                intersectionResult30M.Price,
                _precision,
                intersectionResult30M.LiquidationNumber, lastItem30M.Tmo30
            );
        }
        else
        {
            PreliminaryImpulse30M = new PreliminaryImpulse(
                currency.Name,
                false,
                null,
                isLong30M,
                null,
                0,
                _precision,
                0, lastItem30M.Tmo30
            );
        }

        if (intersectionResult5M is not null)
        {
            PreliminaryImpulse5M = new PreliminaryImpulse(
                currency.Name,
                true,
                lastItem5M,
                isLong5M,
                intersectionResult5M.ToString(),
                intersectionResult5M.Price,
                _precision,
                intersectionResult5M.LiquidationNumber, lastItem30M.Tmo30
            );
        }
        else
        {
            PreliminaryImpulse5M = new PreliminaryImpulse(
                currency.Name,
                false,
                null,
                isLong5M,
                null,
                0,
                _precision,
                0, lastItem30M.Tmo30
            );
        }
        
        PreliminaryImpulse30M.PocPercentageChange = pocPercentageChange;
        PreliminaryImpulse5M.PocPercentageChange = pocPercentageChange;
        PreliminaryImpulse5M.ChangeOv = changeOv;

        if (specifiedIntersectionResult is not null)
        {
            SpecifiedPreliminaryImpulse5M = new PreliminaryImpulse(
                currency.Name,
                true,
                lastItem5M,
                isLong5M,
                specifiedIntersectionResult.ToString(),
                specifiedIntersectionResult.Price,
                _precision,
                specifiedIntersectionResult.LiquidationNumber, lastItem30M.Tmo30)
            {
                PocPercentageChange = pocPercentageChange,
            };
        }
    }

    private void UpdateVolumeManagers()
    {
        VolumeProfileManager.CalculateRawVolumeProfilesForLastItem
            (currency.TradingDataContainer, RawVolumeConfiguration);
        VolumeProfileManager.Update(currency.TradingDataContainer.ThirtyMinuteData);
    }

    private static int CalculatePrecision(decimal value)
    {
        var str = value.ToString("G29", CultureInfo.InvariantCulture);
        var split = str.Split('.');
        return split.Length == 2 ? Math.Min(split[1].TrimEnd('0').Length, 7) : 0;
    }
}