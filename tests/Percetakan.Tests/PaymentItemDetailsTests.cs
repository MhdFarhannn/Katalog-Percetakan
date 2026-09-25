using Katalog.Services;
using Xunit;

namespace Percetakan.Tests
{
    // =========================================================
    // PAYMENT ITEM_DETAILS TESTS
    //
    // Midtrans menolak permintaan Snap dengan HTTP 400
    // ("transaction_details.gross_amount is not equal to the sum
    // of item_details") bila jumlah item_details tidak sama dengan
    // gross_amount.
    //
    // Sebelumnya item_details memakai harga_satuan x qty, sehingga
    // pesanan dengan mode PerArea / PerLength / PerUnit selalu
    // gagal 400 karena subtotal != harga_satuan x qty. Test di
    // bawah menjaga invariant tersebut.
    // =========================================================
    public class PaymentItemDetailsTests
    {
        private static PaymentServices.PaymentLineItem Item(
            int idProduct,
            string nama,
            int qty,
            decimal subtotal,
            decimal? widthMeters = null,
            decimal? heightMeters = null,
            decimal? lengthMeters = null)
        {
            return new PaymentServices.PaymentLineItem
            {
                IdProduct = idProduct,
                NamaProduct = nama,
                Qty = qty,
                Subtotal = subtotal,
                WidthMeters = widthMeters,
                HeightMeters = heightMeters,
                LengthMeters = lengthMeters
            };
        }

        // ---------------------------------------------------------
        // 1. FIXED: subtotal = harga_satuan x qty
        // ---------------------------------------------------------
        [Fact]
        public void Fixed_LineItem_SumsToGrossAmount()
        {
            var items = PaymentServices.BuildItemDetails(
                new[]
                {
                    Item(2, "Banner", qty: 2, subtotal: 300_000m)
                },
                grossAmount: 300_000m);

            Assert.NotNull(items);

            var item = Assert.Single(items!);

            Assert.Equal(300_000m, item.Price);
            Assert.Equal(1, item.Quantity);
            Assert.Equal("2", item.Id);
        }

        // ---------------------------------------------------------
        // 2. REGRESI PERAREA
        //
        // Test (id 15) PerArea: tarif 1.000/m2, ukuran 5x5 m,
        // qty 1 -> subtotal 25.000. Item harus memakai 25.000,
        // bukan tarif 1.000.
        // ---------------------------------------------------------
        [Fact]
        public void PerArea_LineItem_UsesSubtotal_NotUnitRate()
        {
            var items = PaymentServices.BuildItemDetails(
                new[]
                {
                    Item(
                        15,
                        "Test",
                        qty: 1,
                        subtotal: 25_000m,
                        widthMeters: 5m,
                        heightMeters: 5m)
                },
                grossAmount: 25_000m);

            var item = Assert.Single(items!);

            Assert.Equal(25_000m, item.Price);
            Assert.NotEqual(1_000m, item.Price);
            Assert.Equal("Test (5x5 m)", item.Name);
        }

        // ---------------------------------------------------------
        // 3. PERLENGTH: dimensi panjang ikut ke nama item
        // ---------------------------------------------------------
        [Fact]
        public void PerLength_LineItem_ShowsLength()
        {
            var items = PaymentServices.BuildItemDetails(
                new[]
                {
                    Item(
                        14,
                        "Spanduk",
                        qty: 1,
                        subtotal: 7_500m,
                        lengthMeters: 1.5m)
                },
                grossAmount: 7_500m);

            var item = Assert.Single(items!);

            Assert.Equal(7_500m, item.Price);
            Assert.Equal("Spanduk (1.5 m)", item.Name);
        }

        // ---------------------------------------------------------
        // 4. QTY > 1 ikut dicatat di nama item karena quantity
        //    item_details selalu 1
        // ---------------------------------------------------------
        [Fact]
        public void QuantityAboveOne_IsRecordedInItemName()
        {
            var items = PaymentServices.BuildItemDetails(
                new[]
                {
                    Item(13, "Cup", qty: 3, subtotal: 15_000m)
                },
                grossAmount: 15_000m);

            var item = Assert.Single(items!);

            Assert.Equal(1, item.Quantity);
            Assert.Equal("Cup (3 pcs)", item.Name);
        }

        // ---------------------------------------------------------
        // 5. Beberapa baris tetap dijumlahkan
        // ---------------------------------------------------------
        [Fact]
        public void MultipleLines_SumToGrossAmount()
        {
            var items = PaymentServices.BuildItemDetails(
                new[]
                {
                    Item(15, "Test", qty: 1, subtotal: 25_000m, widthMeters: 5m, heightMeters: 5m),
                    Item(13, "Cup", qty: 2, subtotal: 10_000m)
                },
                grossAmount: 35_000m);

            Assert.NotNull(items);
            Assert.Equal(2, items!.Count);
            Assert.Equal(35_000m, items.Sum(i => i.Price * i.Quantity));
        }

        // ---------------------------------------------------------
        // 6. Total tidak cocok -> item_details dilewati (null)
        //    supaya Midtrans tidak membalas 400
        // ---------------------------------------------------------
        [Fact]
        public void MismatchedTotal_ReturnsNull()
        {
            var items = PaymentServices.BuildItemDetails(
                new[]
                {
                    Item(15, "Test", qty: 1, subtotal: 25_000m, widthMeters: 5m, heightMeters: 5m)
                },
                grossAmount: 1_000m);

            Assert.Null(items);
        }

        // ---------------------------------------------------------
        // 7. Tanpa baris detail -> item_details tidak dikirim
        // ---------------------------------------------------------
        [Fact]
        public void EmptyDetails_ReturnsNull()
        {
            var items = PaymentServices.BuildItemDetails(
                Array.Empty<PaymentServices.PaymentLineItem>(),
                grossAmount: 0m);

            Assert.Null(items);
        }

        // ---------------------------------------------------------
        // 8. Nama item dipotong 50 karakter (batas Midtrans)
        // ---------------------------------------------------------
        [Fact]
        public void LongName_IsTruncatedToFiftyCharacters()
        {
            var nama = new string('A', 80);

            var items = PaymentServices.BuildItemDetails(
                new[]
                {
                    Item(1, nama, qty: 1, subtotal: 5_000m)
                },
                grossAmount: 5_000m);

            var item = Assert.Single(items!);

            Assert.Equal(50, item.Name.Length);
        }

        // ---------------------------------------------------------
        // 9. Nama tetap utuh bila muat
        // ---------------------------------------------------------
        [Fact]
        public void ShortName_WithoutDimensions_IsUnchanged()
        {
            var items = PaymentServices.BuildItemDetails(
                new[]
                {
                    Item(2, "Banner", qty: 1, subtotal: 5_000m)
                },
                grossAmount: 5_000m);

            var item = Assert.Single(items!);

            Assert.Equal("Banner", item.Name);
        }
    }
}
