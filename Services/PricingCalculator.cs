using Katalog.Models;

namespace Katalog.Services
{
    // =========================================================
    // PRICING CALCULATOR
    //
    // Satu-satunya tempat rumus harga & validasi dimensi.
    // Fungsi murni (tidak menyentuh database) sehingga mudah
    // diuji dan dipakai ulang oleh service mana pun.
    //
    // Rumus:
    //   Fixed     : subtotal = unitPrice x quantity
    //   PerArea   : area = width x height
    //               subtotal = area x pricePerArea x quantity
    //   PerLength : subtotal = length x pricePerLength x quantity
    //   PerUnit   : subtotal = unitPrice x quantity
    //   Custom    : diperlakukan seperti Fixed (qty x unit price)
    // =========================================================
    public static class PricingCalculator
    {
        private const int MoneyDecimals = 2;

        public static PricingResult Calculate(
            PricingConfiguration config,
            PricingInput input)
        {
            if (input.Quantity <= 0)
            {
                return Fail(config, "Qty minimal 1");
            }

            return config.Mode switch
            {
                PricingMode.PerArea => CalculatePerArea(config, input),
                PricingMode.PerLength => CalculatePerLength(config, input),
                PricingMode.PerUnit => CalculatePerUnit(config, input),
                _ => CalculateFixed(config, input)
            };
        }

        // ---------------------------------------------------------
        // FIXED / CUSTOM
        // Dimensi custom TIDAK diterima (ukuran sudah ditentukan
        // product/varian). Harga = unit price x qty.
        // ---------------------------------------------------------
        private static PricingResult CalculateFixed(
            PricingConfiguration config,
            PricingInput input)
        {
            if (HasAnyDimension(input))
            {
                return Fail(
                    config,
                    "Product dengan pricing Fixed tidak menerima width/height/length");
            }

            var rate = config.EffectiveRate;

            return new PricingResult
            {
                Success = true,
                Mode = config.Mode,
                UnitPrice = rate,
                Quantity = input.Quantity,
                Unit = config.Unit,
                Subtotal = Round(rate * input.Quantity)
            };
        }

        // ---------------------------------------------------------
        // PER AREA
        // area (m2) = width x height (dikonversi ke meter)
        // subtotal  = area x rate x qty
        // ---------------------------------------------------------
        private static PricingResult CalculatePerArea(
            PricingConfiguration config,
            PricingInput input)
        {
            if (input.Length.HasValue)
            {
                return Fail(
                    config,
                    "Product dengan pricing PerArea tidak menerima field length");
            }

            if (input.Width is null || input.Width <= 0)
            {
                return Fail(config, "Width wajib lebih dari 0 untuk pricing PerArea");
            }

            if (input.Height is null || input.Height <= 0)
            {
                return Fail(config, "Height wajib lebih dari 0 untuk pricing PerArea");
            }

            var widthM = PricingUnits.ToMeters(input.Width.Value, config.Unit);
            var heightM = PricingUnits.ToMeters(input.Height.Value, config.Unit);

            if (widthM <= 0 || heightM <= 0)
            {
                return Fail(config, "Width dan Height harus lebih dari 0");
            }

            var area = widthM * heightM;
            var rate = config.EffectiveRate;

            return new PricingResult
            {
                Success = true,
                Mode = config.Mode,
                UnitPrice = rate,
                Quantity = input.Quantity,
                WidthMeters = widthM,
                HeightMeters = heightM,
                Unit = config.Unit,
                AreaM2 = area,
                Subtotal = Round(area * rate * input.Quantity)
            };
        }

        // ---------------------------------------------------------
        // PER LENGTH
        // subtotal = length (m) x rate x qty
        // ---------------------------------------------------------
        private static PricingResult CalculatePerLength(
            PricingConfiguration config,
            PricingInput input)
        {
            if (input.Width.HasValue || input.Height.HasValue)
            {
                return Fail(
                    config,
                    "Product dengan pricing PerLength tidak menerima field width/height");
            }

            if (input.Length is null || input.Length <= 0)
            {
                return Fail(config, "Length wajib lebih dari 0 untuk pricing PerLength");
            }

            var lengthM = PricingUnits.ToMeters(input.Length.Value, config.Unit);

            if (lengthM <= 0)
            {
                return Fail(config, "Length harus lebih dari 0");
            }

            var rate = config.EffectiveRate;

            return new PricingResult
            {
                Success = true,
                Mode = config.Mode,
                UnitPrice = rate,
                Quantity = input.Quantity,
                LengthMeters = lengthM,
                Unit = config.Unit,
                Subtotal = Round(lengthM * rate * input.Quantity)
            };
        }

        // ---------------------------------------------------------
        // PER UNIT
        // subtotal = unit price x qty, tanpa dimensi.
        // ---------------------------------------------------------
        private static PricingResult CalculatePerUnit(
            PricingConfiguration config,
            PricingInput input)
        {
            if (HasAnyDimension(input))
            {
                return Fail(
                    config,
                    "Product dengan pricing PerUnit tidak menerima width/height/length");
            }

            var rate = config.EffectiveRate;

            return new PricingResult
            {
                Success = true,
                Mode = config.Mode,
                UnitPrice = rate,
                Quantity = input.Quantity,
                Unit = config.Unit,
                Subtotal = Round(rate * input.Quantity)
            };
        }

        // ---------------------------------------------------------
        // HELPER
        // ---------------------------------------------------------
        private static bool HasAnyDimension(PricingInput input)
        {
            return input.Width.HasValue
                || input.Height.HasValue
                || input.Length.HasValue;
        }

        // Uang dibulatkan 2 desimal (AwayFromZero) agar konsisten
        // dengan kolom DECIMAL(15,2) di database.
        private static decimal Round(decimal value)
        {
            return Math.Round(value, MoneyDecimals, MidpointRounding.AwayFromZero);
        }

        private static PricingResult Fail(
            PricingConfiguration config,
            string message)
        {
            return new PricingResult
            {
                Success = false,
                Error = message,
                Mode = config.Mode,
                Unit = config.Unit
            };
        }
    }
}
