namespace FuturesSignalsBot.Enums;

public enum CurrencyDeactivationReason
{
    /// <summary>
    /// Currency is active and fully operational
    /// </summary>
    None = 0,

    /// <summary>
    /// Deactivated due to a technical error (API failure, parsing issues, etc.)
    /// </summary>
    Error = 1,

    /// <summary>
    /// Deactivated because the currency has been delisted or is no longer available 
    /// on supported exchanges (insufficient data)
    /// </summary>
    Delisted = 2,

    /// <summary>
    /// Deactivated because it fell out of the top list (no longer meets popularity/volume criteria)
    /// </summary>
    NotInTop = 3
}