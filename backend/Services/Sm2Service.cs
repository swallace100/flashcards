namespace FlashcardsApi.Services;

public static class Sm2Service
{
    // quality: 0=Again, 1=Easy, 2=Normal, 3=Hard
    public static (int interval, float ef, int repetitions, DateTime dueDate) Calculate(
        int quality, int currentInterval, float currentEf, int currentRepetitions)
    {
        int sm2Quality = quality switch
        {
            0 => 1, // Again  → fail
            1 => 5, // Easy   → perfect
            2 => 4, // Normal → correct
            3 => 2, // Hard   → correct with difficulty
            _ => 3
        };

        float newEf = currentEf + (0.1f - (5 - sm2Quality) * (0.08f + (5 - sm2Quality) * 0.02f));
        newEf = Math.Max(1.3f, newEf);

        int newRepetitions;
        int newInterval;

        if (sm2Quality < 3)
        {
            // Failed — reset
            newRepetitions = 0;
            newInterval = 1;
        }
        else
        {
            newRepetitions = currentRepetitions + 1;
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
