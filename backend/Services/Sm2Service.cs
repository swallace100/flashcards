namespace FlashcardsApi.Services;

public static class Sm2Service
{
    // quality: 0=Again, 1=Easy, 2=Normal, 3=Hard
    public static (int interval, float ef, int repetitions, DateTime dueDate) Calculate(
        int quality, int currentInterval, float currentEf, int currentRepetitions)
    {
        int sm2Quality = quality switch
        {
            0 => 1, // Again  → fail (resets)
            1 => 5, // Easy   → perfect
            2 => 4, // Normal → correct
            3 => 3, // Hard   → correct but difficult (advances, does not reset)
            _ => 3
        };

        float newEf = currentEf + (0.1f - (5 - sm2Quality) * (0.08f + (5 - sm2Quality) * 0.02f));
        newEf = Math.Max(1.3f, newEf);

        int newRepetitions;
        int newInterval;

        newRepetitions = currentRepetitions + 1;

        if (sm2Quality < 3)
        {
            // Failed — reset interval but keep repetition count so card stays in due queue
            newInterval = 1;
        }
        else
        {
            newInterval = currentRepetitions switch
            {
                0 => 1,
                1 => 6,
                _ => (int)Math.Round(currentInterval * currentEf)
            };
        }

        return (newInterval, newEf, newRepetitions, DateTime.UtcNow.AddDays(newInterval));
    }
}
