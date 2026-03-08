
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
    }
}


