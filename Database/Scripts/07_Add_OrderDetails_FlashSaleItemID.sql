-- =====================================================
-- 07. ADD OrderDetails."FlashSaleItemID"
-- Ghi lại suất Flash Sale mà dòng đơn hàng đã dùng, để khi hủy đơn
-- hoàn trả SoldQuantity đúng đợt sale (không phải đợt đang chạy lúc hủy).
-- Đơn cũ để NULL -> khi hủy không hoàn suất.
-- Database: PostgreSQL
-- =====================================================

ALTER TABLE public."OrderDetails"
    ADD COLUMN IF NOT EXISTS "FlashSaleItemID" INT NULL;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint WHERE conname = 'FK_OrderDetails_FlashSaleItems'
    ) THEN
        ALTER TABLE public."OrderDetails"
            ADD CONSTRAINT "FK_OrderDetails_FlashSaleItems"
            FOREIGN KEY ("FlashSaleItemID")
            REFERENCES public."FlashSaleItems" ("FlashSaleItemID")
            ON DELETE SET NULL;
    END IF;
END $$;

-- Kiểm tra cột sau khi thêm
SELECT column_name, data_type, is_nullable
FROM information_schema.columns
WHERE table_schema = 'public'
  AND table_name = 'OrderDetails'
  AND column_name = 'FlashSaleItemID';
