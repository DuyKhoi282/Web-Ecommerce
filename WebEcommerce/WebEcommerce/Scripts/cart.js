(function () {
    "use strict";

    function getAntiForgeryToken() {
        try {
            var token = $('input[name="__RequestVerificationToken"]').first().val();

            if (!token || typeof token !== "string") {
                return null;
            }

            return token;
        }
        catch (error) {
            console.error("[Cart] Cannot get AntiForgeryToken:", error);
            return null;
        }
    }

    function updateCartCount(count) {
        try {
            var cartCount = parseInt(count, 10);

            if (isNaN(cartCount) || cartCount < 0) {
                cartCount = 0;
            }

            var cartCountElement = $("#cart-count");

            if (cartCountElement.length > 0) {
                cartCountElement.text(cartCount);
            }
        }
        catch (error) {
            console.error("[Cart] Cannot update cart count:", error);
        }
    }

    function resetButton(button, originalHtml) {
        try {
            if (!button || button.length === 0) {
                return;
            }

            button.html(originalHtml);
            button.prop("disabled", false);
        }
        catch (error) {
            console.error("[Cart] Cannot reset add-to-cart button:", error);
        }
    }

    window.addToCart = function (productId) {

        try {
            // ==============================
            // 1. Validate productId
            // ==============================
            productId = parseInt(productId, 10);

            if (isNaN(productId) || productId <= 0) {
                alert("Sản phẩm không hợp lệ.");
                return;
            }

            // ==============================
            // 2. Get AntiForgeryToken
            // ==============================
            var token = getAntiForgeryToken();

            if (!token) {
                alert(
                    "Không tìm thấy mã xác thực bảo mật. " +
                    "Vui lòng tải lại trang và thử lại."
                );
                return;
            }

            // ==============================
            // 3. Find button
            // ==============================
            var button = $(
                '.btn-add-to-cart[data-product-id="' +
                productId +
                '"]'
            );

            if (button.length === 0) {
                console.error(
                    "[Cart] Add-to-cart button not found. ProductID:",
                    productId
                );

                alert(
                    "Không tìm thấy nút thêm sản phẩm. " +
                    "Vui lòng tải lại trang."
                );

                return;
            }

            // ==============================
            // 4. Prevent duplicate click
            // ==============================
            if (button.prop("disabled")) {
                return;
            }

            var originalHtml = button.html();

            button.prop("disabled", true);

            button.html(
                '<span class="material-symbols-outlined text-base animate-spin">' +
                'progress_activity' +
                '</span>' +
                '<span>Đang thêm...</span>'
            );

            // ==============================
            // 5. AJAX request
            // ==============================
            $.ajax({
                url: "/Cart/AddToCart",
                type: "POST",
                dataType: "json",
                data: {
                    productId: productId,
                    quantity: 1,
                    __RequestVerificationToken: token
                },

                success: function (response) {

                    try {

                        // ------------------------------
                        // Validate response
                        // ------------------------------
                        if (!response ||
                            typeof response !== "object") {

                            resetButton(button, originalHtml);

                            alert(
                                "Phản hồi từ máy chủ không hợp lệ."
                            );

                            return;
                        }

                        // ------------------------------
                        // Business error from Controller
                        // ------------------------------
                        if (response.success !== true) {

                            resetButton(button, originalHtml);

                            alert(
                                response.message ||
                                "Không thể thêm sản phẩm vào giỏ hàng."
                            );

                            return;
                        }

                        // ------------------------------
                        // Success
                        // ------------------------------
                        updateCartCount(response.cartItemCount);

                        button.html(
                            '<span class="material-symbols-outlined text-base">' +
                            'check' +
                            '</span>' +
                            '<span>Đã thêm</span>'
                        );

                        alert(
                            response.message ||
                            "Đã thêm sản phẩm vào giỏ hàng."
                        );

                        // Restore button
                        setTimeout(function () {
                            resetButton(button, originalHtml);
                        }, 1200);

                    }
                    catch (error) {

                        console.error(
                            "[Cart] Error while processing AJAX response:",
                            error
                        );

                        resetButton(button, originalHtml);

                        alert(
                            "Đã xảy ra lỗi khi xử lý kết quả. " +
                            "Vui lòng thử lại."
                        );
                    }
                },

                error: function (xhr, status, error) {

                    try {

                        console.error(
                            "[Cart] AJAX request failed:",
                            {
                                status: status,
                                error: error,
                                httpStatus: xhr ? xhr.status : null
                            }
                        );

                        resetButton(button, originalHtml);

                        // ------------------------------
                        // Unauthorized / session expired
                        // ------------------------------
                        if (xhr && xhr.status === 401) {

                            alert(
                                "Phiên đăng nhập đã hết hạn. " +
                                "Vui lòng đăng nhập lại."
                            );

                            return;
                        }

                        // ------------------------------
                        // Forbidden / AntiForgery
                        // ------------------------------
                        if (xhr && xhr.status === 403) {

                            alert(
                                "Yêu cầu không hợp lệ hoặc phiên bảo mật " +
                                "đã hết hạn. Vui lòng tải lại trang."
                            );

                            return;
                        }

                        // ------------------------------
                        // Not Found
                        // ------------------------------
                        if (xhr && xhr.status === 404) {

                            alert(
                                "Không tìm thấy chức năng giỏ hàng."
                            );

                            return;
                        }

                        // ------------------------------
                        // Server Error
                        // ------------------------------
                        if (xhr && xhr.status >= 500) {

                            alert(
                                "Máy chủ đang gặp sự cố. " +
                                "Vui lòng thử lại sau."
                            );

                            return;
                        }

                        // ------------------------------
                        // Network / unknown error
                        // ------------------------------
                        alert(
                            "Không thể kết nối đến máy chủ. " +
                            "Vui lòng kiểm tra kết nối và thử lại."
                        );

                    }
                    catch (catchError) {

                        console.error(
                            "[Cart] Error while handling AJAX failure:",
                            catchError
                        );

                        resetButton(button, originalHtml);

                        alert(
                            "Đã xảy ra lỗi khi thêm sản phẩm. " +
                            "Vui lòng thử lại."
                        );
                    }
                }
            });

        }
        catch (error) {

            console.error(
                "[Cart] Unexpected addToCart error:",
                error
            );

            alert(
                "Đã xảy ra lỗi không mong muốn. " +
                "Vui lòng thử lại."
            );
        }
    };

})();