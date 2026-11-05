namespace Axis.App.RFID.Services;

/// <summary>
/// Emite beep sonoro ao capturar tags RFID.
/// Usa ToneGenerator do Android para som imediato.
/// </summary>
public static class BeepService
{
    private static Android.Media.ToneGenerator? _tone;

    private static Android.Media.ToneGenerator GetTone()
    {
        return _tone ??= new Android.Media.ToneGenerator(
            (Android.Media.Stream)Android.Media.Stream.Music, 80);
    }

    /// <summary>Beep curto de confirmacao (tag nova lida).</summary>
    public static void Beep()
    {
        try { GetTone().StartTone(Android.Media.Tone.PropBeep, 80); }
        catch { }
    }

    /// <summary>Beep duplo (tag encontrada no localizador).</summary>
    public static void BeepDuplo()
    {
        try { GetTone().StartTone(Android.Media.Tone.PropBeep2, 120); }
        catch { }
    }

    /// <summary>Beep de erro (tag desconhecida).</summary>
    public static void BeepErro()
    {
        try { GetTone().StartTone(Android.Media.Tone.PropNack, 100); }
        catch { }
    }
}
