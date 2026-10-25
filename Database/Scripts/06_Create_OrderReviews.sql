
-- =====================================================
-- 06. CREATE ORDER REVIEWS
-- Bổ sung bảng đánh giá tổng thể đơn hàng
-- Database: PostgreSQL
-- =====================================================

CREATE TABLE IF NOT EXISTS public."OrderReviews"
(
    "OrderReviewID" SERIAL PRIMARY KEY,

    "OrderID" INT NOT NULL,
    "UserID" VARCHAR(128) NOT NULL,

    "Rating" INT NOT NULL
        CHECK ("Rating" >= 1 AND "Rating" <= 5),

    "Comment" TEXT,

    "CreatedAt" TIMESTAMP NOT NULL
        DEFAULT CURRENT_TIMESTAMP,

    "IsApproved" BOOLEAN NOT NULL
        DEFAULT TRUE,

    CONSTRAINT "FK_OrderReviews_Orders"
        FOREIGN KEY ("OrderID")
        REFERENCES public."Orders" ("OrderID")
        ON DELETE CASCADE,

    CONSTRAINT "FK_OrderReviews_AspNetUsers"
        FOREIGN KEY ("UserID")
        REFERENCES public."AspNetUsers" ("Id")
        ON DELETE CASCADE,

    CONSTRAINT "UQ_OrderReviews_OrderID"
        UNIQUE ("OrderID")
);

-- Kiểm tra bảng sau khi tạo
SELECT column_name, data_type, is_nullable
FROM information_schema.columns
WHERE table_schema = 'public'
  AND table_name = 'OrderReviews'
ORDER BY ordinal_position;
