-- ============================================================================
-- SCRIPT CẬP NHẬT: Bổ sung các cột hủy đơn hàng cho bảng Orders
-- Áp dụng khi gặp lỗi: "column CancellationReason of relation Orders does not exist"
-- ============================================================================

ALTER TABLE "Orders" ADD COLUMN IF NOT EXISTS "CancellationReason" VARCHAR(1000);
ALTER TABLE "Orders" ADD COLUMN IF NOT EXISTS "CancelledBy" VARCHAR(50);
ALTER TABLE "Orders" ADD COLUMN IF NOT EXISTS "CancelledAt" TIMESTAMP;
