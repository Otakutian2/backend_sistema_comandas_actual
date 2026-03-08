
using proyecto_backend.Enums;

namespace proyecto_backend.Utils
{
    public static class GlobalUtils
    {
        public static int GenerateRandomNumber(int min, int max)
        {
            Random random = new();
            return random.Next(min, max);
        }

        public static decimal CalculateDiscountedPrice(decimal originalPrice, decimal discountValue, string discountType)
        {
            if (discountType == Constants.DiscountTypes.Percentage)
            {
                return originalPrice - (originalPrice * (discountValue / 100));
            }
            else if (discountType == Constants.DiscountTypes.FixedAmount)
            {
                return originalPrice - discountValue;
            }
            else
            {
                return originalPrice;
            }
        }

        public static decimal GetDiscountAmount(decimal originalPrice, decimal discountValue, string discountType)
        {
            if (discountType == Constants.DiscountTypes.Percentage)
            {
                return originalPrice * (discountValue / 100);
            }
            else if (discountType == Constants.DiscountTypes.FixedAmount)
            {
                return discountValue;
            }
            else
            {
                return 0;
            }
        }

        public static DateTime GetPeruTime()
        {
            try
            {
                // Identificador para Windows
                TimeZoneInfo peruZone = TimeZoneInfo.FindSystemTimeZoneById("SA Pacific Standard Time");
                return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, peruZone);
            }
            catch (TimeZoneNotFoundException)
            {
                // Identificador para Linux/macOS o sistemas modernos de .NET Core con tzdata
                try 
                {
                    TimeZoneInfo peruZone = TimeZoneInfo.FindSystemTimeZoneById("America/Lima");
                    return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, peruZone);
                }
                catch 
                {
                    // Alternativa directa de UTC-5 en caso extremo
                    return DateTime.UtcNow.AddHours(-5);
                }
            }
        }
    }
}


