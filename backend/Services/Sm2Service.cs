namespace FlashcardsApi.Services;

public static class Sm2Service
{
    // quality: 0=Again, 1=Easy, 2=Normal, 3=Hard
    // ef: easiness factor
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

        // Calculate the new easiness factor using the caculations from Piotr Wozniak's original SM-2 paper
        float newEf = currentEf + (0.1f - (5 - sm2Quality) * (0.08f + (5 - sm2Quality) * 0.02f));

        // Floor EF so that intervals continue to grow at a reasonable pace
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
                // First time seeing the card (repetitions = 0)
                0 => 1,
                // Second time seeing the card (review in 6 days)
                1 => 6,
                // 2+ times seeing the card (multiple current interval by the easiness factor so that
                // intervals grow exponentially the more you know a card.)
                _ => (int)Math.Round(currentInterval * currentEf)
            };
        }

        return (newInterval, newEf, newRepetitions, DateTime.UtcNow.AddDays(newInterval));
    }
}
