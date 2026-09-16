namespace GrafikPlanerCore;

/// <summary>
/// Stan urlopu pracownika dla danego roku, liczony z danych grafiku.
/// </summary>
public class VacationState
{
    /// <summary>Niewykorzystane urlopy z poprzedniego roku (0, gdy wygasłe).</summary>
    public int Carryover { get; init; }

    /// <summary>Łączna pula urlopowa w roku: wymiar + zaległe.</summary>
    public int Pool { get; init; }

    /// <summary>Pozostałe dni = pula − wykorzystane w bieżącym roku.</summary>
    public int Remaining { get; init; }

    /// <summary>Proporcjonalny wymiar urlopu w danym roku (pełny od drugiego roku pracy).</summary>
    public int Quota { get; init; }

    /// <summary>Stan krytyczny: wykorzystane przekroczyły pulę.</summary>
    public bool IsCritical { get; init; }
}

/// <summary>
/// Czysta kalkulacja stanu urlopu. Nie dotyka bazy — otrzymuje gotowe liczby
/// (wymiar, wykorzystane w roku, wykorzystane w poprzednim roku, miesiąc i rok
/// referencyjny oraz datę dołączenia pracownika) i zwraca stan zgodnie z regułami:
/// zaległe(Y) = max(0, wymiar proporcjonalny(Y−1) − wykorzystane(Y−1)),
/// aktywne tylko dla miesiąca referencyjnego ≤ 9 (wygasają 30.09).
/// Wymiar w pierwszym roku pracy jest proporcjonalny: quota × (13 − miesiąc dołączenia) / 12.
/// </summary>
public static class VacationStateCalculator
{
    public static VacationState Compute(int quota, int usedInYear, int usedPrevYear,
        int referenceMonth, int referenceYear, string? joinDate)
    {
        int effectiveQuota = GetProportionalQuota(quota, referenceYear, joinDate);
        int prevYearQuota = GetProportionalQuota(quota, referenceYear - 1, joinDate);

        int carryover = referenceMonth <= 9 ? Math.Max(0, prevYearQuota - usedPrevYear) : 0;
        int pool = effectiveQuota + carryover;

        return new VacationState
        {
            Carryover = carryover,
            Pool = pool,
            Quota = effectiveQuota,
            Remaining = pool - usedInYear,
            IsCritical = usedInYear > pool
        };
    }

    /// <summary>
    /// Proporcjonalny wymiar urlopu w danym roku. W roku dołączenia wymiar to
    /// quota × (13 − miesiąc dołączenia) / 12 (zaokrąglone w górę do pełnego dnia —
    /// art. 155² Kodeksu Pracy); w latach przed dołączeniem 0; w kolejnych latach
    /// pełny wymiar.
    /// </summary>
    public static int GetProportionalQuota(int quota, int year, string? joinDate)
    {
        if (!DateOnly.TryParseExact(joinDate ?? "", "yyyy-MM-dd", out var joinDateValue))
            return quota;

        if (year < joinDateValue.Year) return 0;
        if (year == joinDateValue.Year)
            return (int)Math.Ceiling(quota * (13 - joinDateValue.Month) / 12.0);
        return quota;
    }
}