using Katalog.Models;
using Katalog.Services;
using Xunit;

namespace Percetakan.Tests
{
    // =========================================================
    // PRICING CALCULATOR TESTS
    //
    // Menguji PricingCalculator secara murni (tanpa database):
    // rumus tiap mode, validasi, konversi satuan, pembulatan,
    // kompatibilitas product lama, dan jaminan bahwa harga TIDAK
    // diambil dari input client.
    // =========================================================
    public class PricingCalculatorTests
    {
        private static PricingConfiguration Config(
            PricingMode mode,
            decimal basePrice,
            decimal? variantPrice = null,
            decimal additional = 0,
            DimensionUnit unit = DimensionUnit.Meter)
        {
            return new PricingConfiguration
            {
                Mode = mode,
                BasePrice = basePrice,
                VariantPrice = variantPrice,
                AdditionalPrice = additional,
                Unit = unit
            };
        }

        // ---------------------------------------------------------
        // 1. FIXED
        // Standing Banner 60x160, Rp150.000, qty 2 -> Rp300.000
        // ---------------------------------------------------------
        [Fact]
        public void Fixed_UsesVariantPrice_TimesQuantity()
        {
            var config = Config(
                PricingMode.Fixed,
                basePrice: 0m,
                variantPrice: 150_000m);

            var result = PricingCalculator.Calculate(
                config,
                new PricingInput { Quantity = 2 });

            Assert.True(result.Success);
            Assert.Equal(150_000m, result.UnitPrice);
            Assert.Equal(300_000m, result.Subtotal);
        }

        // Product lama (tanpa varian): harga = base + harga_tambahan.
        [Fact]
        public void Fixed_LegacyProduct_UsesBasePlusAdditional()
        {
            var config = Config(
                PricingMode.Fixed,
                basePrice: 100_000m,
                additional: 50_000m);

            var result = PricingCalculator.Calculate(
                config,
                new PricingInput { Quantity = 2 });

            Assert.True(result.Success);
            Assert.Equal(150_000m, result.UnitPrice);
            Assert.Equal(300_000m, result.Subtotal);
        }

        [Fact]
        public void Fixed_RejectsCustomDimensions()
        {
            var config = Config(PricingMode.Fixed, 150_000m);

            var result = PricingCalculator.Calculate(
                config,
                new PricingInput { Quantity = 1, Width = 3m, Height = 1m });

            Assert.False(result.Success);
        }

        // ---------------------------------------------------------
        // 2. PER AREA
        // Banner Rp25.000/m2, 3m x 1m, qty 2 -> Rp150.000
        // ---------------------------------------------------------
        [Fact]
        public void PerArea_MultipliesAreaRateQuantity()
        {
            var config = Config(PricingMode.PerArea, 25_000m);

            var result = PricingCalculator.Calculate(
                config,
                new PricingInput
                {
                    Quantity = 2,
                    Width = 3m,
                    Height = 1m
                });

            Assert.True(result.Success);
            Assert.Equal(3m, result.AreaM2);
            Assert.Equal(150_000m, result.Subtotal);
        }

        // Input dalam centimeter harus dikonversi ke meter.
        [Fact]
        public void PerArea_ConvertsCentimeterInputToMeters()
        {
            var config = Config(
                PricingMode.PerArea,
                25_000m,
                unit: DimensionUnit.Centimeter);

            var result = PricingCalculator.Calculate(
                config,
                new PricingInput
                {
                    Quantity = 2,
                    Width = 300m,
                    Height = 100m
                });

            Assert.True(result.Success);
            Assert.Equal(3m, result.WidthMeters);
            Assert.Equal(1m, result.HeightMeters);
            Assert.Equal(3m, result.AreaM2);
            Assert.Equal(150_000m, result.Subtotal);
        }

        [Fact]
        public void PerArea_RejectsZeroWidth()
        {
            var config = Config(PricingMode.PerArea, 25_000m);

            var result = PricingCalculator.Calculate(
                config,
                new PricingInput
                {
                    Quantity = 1,
                    Width = 0m,
                    Height = 1m
                });

            Assert.False(result.Success);
            Assert.NotNull(result.Error);
        }

        [Fact]
        public void PerArea_RejectsMissingHeight()
        {
            var config = Config(PricingMode.PerArea, 25_000m);

            var result = PricingCalculator.Calculate(
                config,
                new PricingInput { Quantity = 1, Width = 3m });

            Assert.False(result.Success);
        }

        [Fact]
        public void PerArea_RejectsLengthField()
        {
            var config = Config(PricingMode.PerArea, 25_000m);

            var result = PricingCalculator.Calculate(
                config,
                new PricingInput
                {
                    Quantity = 1,
                    Width = 3m,
                    Height = 1m,
                    Length = 5m
                });

            Assert.False(result.Success);
        }

        // ---------------------------------------------------------
        // 3. PER LENGTH
        // Fabric Rp20.000/m, 5m, qty 2 -> Rp200.000
        // ---------------------------------------------------------
        [Fact]
        public void PerLength_MultipliesLengthRateQuantity()
        {
            var config = Config(PricingMode.PerLength, 20_000m);

            var result = PricingCalculator.Calculate(
                config,
                new PricingInput { Quantity = 2, Length = 5m });

            Assert.True(result.Success);
            Assert.Equal(5m, result.LengthMeters);
            Assert.Equal(200_000m, result.Subtotal);
        }

        [Fact]
        public void PerLength_DoesNotComputeArea()
        {
            var config = Config(PricingMode.PerLength, 20_000m);

            var result = PricingCalculator.Calculate(
                config,
                new PricingInput { Quantity = 1, Length = 5m });

            Assert.True(result.Success);
            Assert.Equal(0m, result.AreaM2);
            Assert.Null(result.WidthMeters);
        }

        [Fact]
        public void PerLength_RejectsWidthOrHeight()
        {
            var config = Config(PricingMode.PerLength, 20_000m);

            var result = PricingCalculator.Calculate(
                config,
                new PricingInput
                {
                    Quantity = 1,
                    Length = 5m,
                    Width = 1m
                });

            Assert.False(result.Success);
        }

        // ---------------------------------------------------------
        // 4. PER UNIT
        // Business Cards Rp50.000, qty 3 -> Rp150.000
        // ---------------------------------------------------------
        [Fact]
        public void PerUnit_MultipliesUnitPriceQuantity()
        {
            var config = Config(PricingMode.PerUnit, 50_000m);

            var result = PricingCalculator.Calculate(
                config,
                new PricingInput { Quantity = 3 });

            Assert.True(result.Success);
            Assert.Equal(50_000m, result.UnitPrice);
            Assert.Equal(150_000m, result.Subtotal);
        }

        // ---------------------------------------------------------
        // 5. CUSTOM (cadangan) - diperlakukan seperti Fixed
        // ---------------------------------------------------------
        [Fact]
        public void Custom_BehavesLikeFixed()
        {
            var config = Config(PricingMode.Custom, 10_000m);

            var result = PricingCalculator.Calculate(
                config,
                new PricingInput { Quantity = 4 });

            Assert.True(result.Success);
            Assert.Equal(40_000m, result.Subtotal);
        }

        // ---------------------------------------------------------
        // 6. INVALID QUANTITY
        // ---------------------------------------------------------
        [Theory]
        [InlineData(PricingMode.Fixed)]
        [InlineData(PricingMode.PerArea)]
        [InlineData(PricingMode.PerLength)]
        [InlineData(PricingMode.PerUnit)]
        [InlineData(PricingMode.Custom)]
        public void Quantity_Zero_FailsForEveryMode(PricingMode mode)
        {
            var config = Config(mode, 25_000m);

            var result = PricingCalculator.Calculate(
                config,
                new PricingInput
                {
                    Quantity = 0,
                    Width = 3m,
                    Height = 1m,
                    Length = 5m
                });

            Assert.False(result.Success);
        }

        // ---------------------------------------------------------
        // 7. PEMBULATAN
        // area = 0.333 x 0.333 = 0.110889 m2
        // 0.110889 x 25.000 = 2.772.225 -> dibulatkan 2.772,23
        // ---------------------------------------------------------
        [Fact]
        public void PerArea_RoundsMoneyToTwoDecimals()
        {
            var config = Config(PricingMode.PerArea, 25_000m);

            var result = PricingCalculator.Calculate(
                config,
                new PricingInput
                {
                    Quantity = 1,
                    Width = 0.333m,
                    Height = 0.333m
                });

            Assert.True(result.Success);
            Assert.Equal(2_772.23m, result.Subtotal);
        }

        // ---------------------------------------------------------
        // 8. HARGA TIDAK BOLEH DATANG DARI CLIENT
        //
        // PricingInput hanya berisi id + qty + dimensi. Tidak ada
        // field harga, sehingga client tidak bisa menentukan harga.
        // ---------------------------------------------------------
        [Fact]
        public void PricingInput_ExposesNoClientPriceField()
        {
            var names = typeof(PricingInput)
                .GetProperties()
                .Select(p => p.Name.ToLowerInvariant())
                .ToList();

            Assert.DoesNotContain(
                names,
                n => n.Contains("price")
                    || n.Contains("harga")
                    || n.Contains("subtotal"));

            Assert.Equal(
                new[] { "height", "idproduct", "idukuranproduk", "length", "quantity", "width" },
                names.OrderBy(n => n));
        }

        [Fact]
        public void ClientProvidedValues_AreNotUsedForPricing()
        {
            // Harga murni berasal dari PricingConfiguration di server,
            // bukan dari input. Dua input identik menghasilkan harga
            // yang sama walau "niat" client berbeda.
            var config = Config(PricingMode.PerUnit, 50_000m);

            var a = PricingCalculator.Calculate(
                config,
                new PricingInput { Quantity = 3 });

            var b = PricingCalculator.Calculate(
                config,
                new PricingInput { Quantity = 3 });

            Assert.Equal(a.Subtotal, b.Subtotal);
            Assert.Equal(150_000m, a.Subtotal);
        }

        // ---------------------------------------------------------
        // 9. KOMPATIBILITAS PRODUCT LAMA
        // Product lama tidak punya pricing_mode -> dianggap Fixed.
        // ---------------------------------------------------------
        [Fact]
        public void LegacyProduct_DefaultsToFixedMode()
        {
            Assert.Equal(PricingMode.Fixed, PricingUnits.ParseMode(null));
            Assert.Equal(PricingMode.Fixed, PricingUnits.ParseMode(""));
            Assert.Equal(PricingMode.Fixed, PricingUnits.ParseMode("tidak-dikenal"));
        }
    }

    // =========================================================
    // UNIT CONVERSION TESTS
    // =========================================================
    public class PricingUnitsTests
    {
        [Theory]
        [InlineData("cm", DimensionUnit.Centimeter)]
        [InlineData("centimeter", DimensionUnit.Centimeter)]
        [InlineData("m", DimensionUnit.Meter)]
        [InlineData("meter", DimensionUnit.Meter)]
        [InlineData(null, DimensionUnit.Meter)]
        [InlineData("diagonal", DimensionUnit.Meter)]
        public void ParseUnit_Normalizes(string? input, DimensionUnit expected)
        {
            Assert.Equal(expected, PricingUnits.ParseUnit(input));
        }

        [Theory]
        [InlineData("Fixed", PricingMode.Fixed)]
        [InlineData("fixed", PricingMode.Fixed)]
        [InlineData("PerArea", PricingMode.PerArea)]
        [InlineData("perlength", PricingMode.PerLength)]
        public void ParseMode_IsCaseInsensitive(string input, PricingMode expected)
        {
            Assert.Equal(expected, PricingUnits.ParseMode(input));
        }

        [Fact]
        public void ToMeters_ConvertsCentimeterAndMeter()
        {
            Assert.Equal(1m, PricingUnits.ToMeters(100m, DimensionUnit.Centimeter));
            Assert.Equal(3m, PricingUnits.ToMeters(3m, DimensionUnit.Meter));
        }

        [Fact]
        public void ToCode_ProducesCanonicalStrings()
        {
            Assert.Equal("centimeter", PricingUnits.ToCode(DimensionUnit.Centimeter));
            Assert.Equal("meter", PricingUnits.ToCode(DimensionUnit.Meter));
            Assert.Equal("PerArea", PricingUnits.ToCode(PricingMode.PerArea));
        }
    }
}
